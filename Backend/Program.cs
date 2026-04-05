using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MuafaPlus.Data;
using MuafaPlus.Infrastructure;
using MuafaPlus.Models;
using MuafaPlus.Services;
using QuestPDF.Infrastructure;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("DefaultConnection not configured.");

    builder.Host.UseSerilog((ctx, cfg) =>
        cfg.ReadFrom.Configuration(ctx.Configuration)
           .Enrich.FromLogContext()
           .WriteTo.Console()
           .WriteTo.File("logs/muafa-.txt", rollingInterval: RollingInterval.Day));

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "Muafa+ API",
            Version = "v1",
            Description = "Medical education content generation - Yemen"
        });
        c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Description = "Enter: Bearer {token}"
        });
        c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
        {{
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }});
    });

    builder.Services.AddDbContext<MuafaDbContext>(opts =>
        opts.UseNpgsql(connectionString));

    builder.Services.AddHealthChecks()
        .AddDbContextCheck<MuafaDbContext>("database");

    var jwtSecret = builder.Configuration["Jwt:Secret"]
        ?? throw new InvalidOperationException("Jwt:Secret must be configured.");

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(opts =>
        {
            opts.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "muafaplus-api",
                ValidAudience = builder.Configuration["Jwt:Audience"] ?? "muafaplus-ui",
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtSecret)),
                ClockSkew = TimeSpan.FromMinutes(1)
            };
        });

    builder.Services.AddAuthorization();

    builder.Services.AddSingleton<PromptBuilder>();
    builder.Services.AddSingleton<RiskCalculatorService>();
    builder.Services.AddScoped<JwtService>();
    builder.Services.AddScoped<MuafaApiClient>();
    builder.Services.AddScoped<WorkflowService>();
    builder.Services.AddScoped<GenerationJobService>();
    builder.Services.AddScoped<ExportService>();
    builder.Services.AddScoped<InvitationCodeService>();

    QuestPDF.Settings.License = LicenseType.Community;

    builder.Services.AddHangfire(cfg =>
        cfg.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
           .UseSimpleAssemblyNameTypeSerializer()
           .UseRecommendedSerializerSettings()
           .UsePostgreSqlStorage(c =>
               c.UseNpgsqlConnection(connectionString)));

    builder.Services.AddHangfireServer();

    builder.Services.AddHttpClient();

    var allowedOrigins = builder.Configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>()
        ?? new[] { "http://localhost:3000", "https://localhost:3000" };

    builder.Services.AddCors(opts =>
        opts.AddDefaultPolicy(policy =>
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials()));

    var generationsPerHour = builder.Configuration.GetValue<int>("RateLimit:GenerationsPerHour", 10);

    builder.Services.AddRateLimiter(options =>
    {
        options.AddPolicy("GenerationsPerHour", httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? httpContext.Connection.RemoteIpAddress?.ToString()
                              ?? "anonymous",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit          = generationsPerHour,
                    Window               = TimeSpan.FromHours(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit           = 0
                }));

        options.OnRejected = async (context, token) =>
        {
            context.HttpContext.Response.StatusCode  = StatusCodes.Status429TooManyRequests;
            context.HttpContext.Response.ContentType = "application/json";
            var body = JsonSerializer.Serialize(new ApiResponse<object>
            {
                Success   = false,
                Error     = $"لقد تجاوزت الحد المسموح به من الطلبات. الحد الأقصى هو {generationsPerHour} طلبات في الساعة الواحدة.",
                ErrorType = "RateLimitExceeded"
            });
            await context.HttpContext.Response.WriteAsync(body, token);
        };
    });

    var app = builder.Build();

    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<MuafaDbContext>();
        db.Database.Migrate();
    }

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Muafa+ API v1");
            c.RoutePrefix = string.Empty;
        });
    }

    if (app.Environment.IsDevelopment())
        app.UseHttpsRedirection();
    app.UseCors();
    app.UseSerilogRequestLogging();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseRateLimiter();
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = new[] { new HangfireDashboardAuthFilter(
            app.Services.GetRequiredService<IConfiguration>()) }
    });
    app.MapControllers();
    app.MapHealthChecks("/health");

    Log.Information("Muafa+ API starting - env:{Env}",
        app.Environment.EnvironmentName);

    var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
    app.Run($"http://+:{port}");
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application startup failed");
}
finally
{
    Log.CloseAndFlush();
}
