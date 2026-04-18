using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MuafaPlus.Data;
using MuafaPlus.Models;
using MuafaPlus.Services;

namespace MuafaPlus.Controllers;

/// <summary>
/// Phase 1: Tenant management endpoints.
/// All routes require a valid JWT (Rule 6).
/// </summary>
[ApiController]
[Route("api/v1/tenants")]
[Authorize]
[Produces("application/json")]
public class TenantsController : ControllerBase
{
    private readonly TenantService              _tenants;
    private readonly MuafaDbContext             _db;
    private readonly ILogger<TenantsController> _logger;

    public TenantsController(
        TenantService              tenants,
        MuafaDbContext             db,
        ILogger<TenantsController> logger)
    {
        _tenants = tenants;
        _db      = db;
        _logger  = logger;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET /api/v1/tenants
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Returns all tenants ordered by creation date (newest first).</summary>
    [HttpGet]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<List<TenantResponse>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<TenantResponse>>>> GetAll()
    {
        var tenants = await _tenants.GetAllTenantsAsync();
        return Ok(new ApiResponse<List<TenantResponse>> { Success = true, Data = tenants });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST /api/v1/tenants
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new tenant with default settings, initial subscription,
    /// and an HA-XXXXXX invitation code for the first Hospital Admin.
    /// Returns 201 Created with the full TenantResponse.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<TenantResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>),         StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<TenantResponse>>> Create(
        [FromBody] CreateTenantRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Error   = "Validation failed.",
                Metadata = new Dictionary<string, object>
                {
                    ["errors"] = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList()
                }
            });

        try
        {
            var tenant = await _tenants.CreateTenantAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = tenant.TenantId },
                new ApiResponse<TenantResponse> { Success = true, Data = tenant });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create tenant");
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
            {
                Success   = false,
                Error     = "Failed to create tenant.",
                ErrorType = ex.GetType().Name
            });
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET /api/v1/tenants/{id}
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Returns a single tenant by ID. Returns 404 if not found.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<TenantResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>),         StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TenantResponse>>> GetById(Guid id)
    {
        var tenant = await _tenants.GetTenantAsync(id);
        if (tenant == null)
            return NotFound(new ApiResponse<object>
            {
                Success   = false,
                Error     = $"Tenant {id} not found.",
                ErrorType = "NotFound"
            });

        return Ok(new ApiResponse<TenantResponse> { Success = true, Data = tenant });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET /api/v1/tenants/{id}/settings
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Returns settings for the specified tenant.</summary>
    [HttpGet("{id:guid}/settings")]
    [ProducesResponseType(typeof(ApiResponse<TenantSettingsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>),                 StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TenantSettingsResponse>>> GetSettings(Guid id)
    {
        var tenant = await _tenants.GetTenantAsync(id);
        if (tenant == null)
            return NotFound(new ApiResponse<object>
            {
                Success   = false,
                Error     = $"Tenant {id} not found.",
                ErrorType = "NotFound"
            });

        return Ok(new ApiResponse<TenantSettingsResponse> { Success = true, Data = tenant.Settings });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PUT /api/v1/tenants/{id}/settings
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Updates tenant settings. All fields are optional — only non-null values
    /// are applied.
    /// </summary>
    [HttpPut("{id:guid}/settings")]
    [ProducesResponseType(typeof(ApiResponse<TenantSettingsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>),                 StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>),                 StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TenantSettingsResponse>>> UpdateSettings(
        Guid id,
        [FromBody] UpdateTenantSettingsRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Error   = "Validation failed.",
                Metadata = new Dictionary<string, object>
                {
                    ["errors"] = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList()
                }
            });

        try
        {
            var settings = await _tenants.UpdateTenantSettingsAsync(id, request);
            return Ok(new ApiResponse<TenantSettingsResponse> { Success = true, Data = settings });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ApiResponse<object>
            {
                Success   = false,
                Error     = ex.Message,
                ErrorType = "NotFound"
            });
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET /api/v1/tenants/{id}/subscription
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the active subscription for the specified tenant.
    /// Returns 404 if there is no active subscription.
    /// </summary>
    [HttpGet("{id:guid}/subscription")]
    [ProducesResponseType(typeof(ApiResponse<TenantSubscriptionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>),                     StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TenantSubscriptionResponse>>> GetSubscription(Guid id)
    {
        var sub = await _tenants.GetTenantSubscriptionAsync(id);
        if (sub == null)
            return NotFound(new ApiResponse<object>
            {
                Success   = false,
                Error     = $"No active subscription found for tenant {id}.",
                ErrorType = "NotFound"
            });

        return Ok(new ApiResponse<TenantSubscriptionResponse> { Success = true, Data = sub });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST /api/v1/tenants/{id}/assistant-links
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Links an assistant to a physician within the specified tenant.
    /// Returns 409 Conflict if the link already exists and is active.
    /// </summary>
    [HttpPost("{id:guid}/assistant-links")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<object>>> LinkAssistant(
        Guid id,
        [FromBody] LinkAssistantRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Error   = "Validation failed.",
                Metadata = new Dictionary<string, object>
                {
                    ["errors"] = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList()
                }
            });

        var linked = await _tenants.LinkAssistantToPhysicianAsync(id, request);
        if (!linked)
            return Conflict(new ApiResponse<object>
            {
                Success   = false,
                Error     = $"Assistant {request.AssistantId} is already linked to physician {request.PhysicianId} in this tenant.",
                ErrorType = "Conflict"
            });

        return Ok(new ApiResponse<object>
        {
            Success  = true,
            Metadata = new Dictionary<string, object>
            {
                ["assistantId"] = request.AssistantId,
                ["physicianId"] = request.PhysicianId,
                ["tenantId"]    = id
            }
        });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET /api/v1/tenants/{id}/assistant-links
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Returns all active assistant-physician links for the specified tenant.</summary>
    [HttpGet("{id:guid}/assistant-links")]
    [ProducesResponseType(typeof(ApiResponse<List<AssistantLinkResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>),                      StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<List<AssistantLinkResponse>>>> GetAssistantLinks(Guid id)
    {
        var tenant = await _tenants.GetTenantAsync(id);
        if (tenant == null)
            return NotFound(new ApiResponse<object>
            {
                Success   = false,
                Error     = $"Tenant {id} not found.",
                ErrorType = "NotFound"
            });

        var links = await _db.AssistantPhysicianLinks
            .Where(l => l.TenantId == id && l.IsActive)
            .OrderByDescending(l => l.LinkedAt)
            .ToListAsync();

        if (!links.Any())
            return Ok(new ApiResponse<List<AssistantLinkResponse>> { Success = true, Data = new List<AssistantLinkResponse>() });

        // Collect all user IDs (stored as strings, parsed to Guid for the DB lookup)
        var guidSet = links
            .SelectMany(l => new[] { l.AssistantId, l.PhysicianId })
            .Distinct()
            .Where(s => Guid.TryParse(s, out _))
            .Select(Guid.Parse)
            .ToList();

        var userMap = await _db.Users
            .Where(u => guidSet.Contains(u.UserId))
            .Select(u => new { UserId = u.UserId.ToString(), u.FullName })
            .ToDictionaryAsync(u => u.UserId, u => u.FullName);

        var response = links.Select(l => new AssistantLinkResponse
        {
            LinkId        = l.LinkId,
            AssistantId   = l.AssistantId,
            AssistantName = userMap.GetValueOrDefault(l.AssistantId, l.AssistantId),
            PhysicianId   = l.PhysicianId,
            PhysicianName = userMap.GetValueOrDefault(l.PhysicianId, l.PhysicianId),
            TenantId      = l.TenantId,
            IsActive      = l.IsActive,
            LinkedAt      = l.LinkedAt
        }).ToList();

        return Ok(new ApiResponse<List<AssistantLinkResponse>> { Success = true, Data = response });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET /api/v1/tenants/{id}/users
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Returns all users belonging to the specified tenant.</summary>
    [HttpGet("{id:guid}/users")]
    [ProducesResponseType(typeof(ApiResponse<List<UserSummaryResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>),                    StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<List<UserSummaryResponse>>>> GetUsers(Guid id)
    {
        var tenant = await _tenants.GetTenantAsync(id);
        if (tenant == null)
            return NotFound(new ApiResponse<object>
            {
                Success   = false,
                Error     = $"Tenant {id} not found.",
                ErrorType = "NotFound"
            });

        var rawUsers = await _db.Users
            .Where(u => u.TenantId == id)
            .Join(_db.UserRoles,
                  u  => u.UserId,
                  ur => ur.UserId,
                  (u, ur) => new {
                      u.UserId,
                      u.Email,
                      u.FullName,
                      u.IsActive,
                      u.CreatedAt,
                      u.TenantId,
                      ur.Role
                  })
            .ToListAsync();

        var users = rawUsers.Select(x => new UserSummaryResponse
            {
                UserId    = x.UserId,
                Email     = x.Email,
                FullName  = x.FullName ?? string.Empty,
                Role      = x.Role.ToString(),
                TenantId  = x.TenantId,
                IsActive  = x.IsActive,
                CreatedAt = x.CreatedAt,
            })
            .OrderBy(u => u.FullName)
            .ToList();

        return Ok(new ApiResponse<List<UserSummaryResponse>> { Success = true, Data = users });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST /api/v1/tenants/{id}/users
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new user within the specified tenant.
    /// Returns 409 Conflict if a user with the same email already exists.
    /// </summary>
    [HttpPost("{id:guid}/users")]
    [ProducesResponseType(typeof(ApiResponse<UserSummaryResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>),              StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>),              StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>),              StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<UserSummaryResponse>>> CreateUser(
        Guid id,
        [FromBody] CreateTenantUserRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Error   = "Validation failed.",
                Metadata = new Dictionary<string, object>
                {
                    ["errors"] = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList()
                }
            });

        var tenant = await _tenants.GetTenantAsync(id);
        if (tenant == null)
            return NotFound(new ApiResponse<object>
            {
                Success   = false,
                Error     = $"Tenant {id} not found.",
                ErrorType = "NotFound"
            });

        var exists = await _db.Users.AnyAsync(u => u.Email == request.Email);
        if (exists)
            return Conflict(new ApiResponse<object>
            {
                Success   = false,
                Error     = $"A user with email {request.Email} already exists.",
                ErrorType = "Conflict"
            });

        var user = new AppUser
        {
            UserId               = Guid.NewGuid(),
            TenantId             = id,
            Email                = request.Email,
            FullName             = request.FullName,
            PasswordHash         = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 12),
            Role                 = request.Role,
            IsActive             = true,
            MustResetOnNextLogin = true,
            CreatedAt            = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var response = new UserSummaryResponse
        {
            UserId    = user.UserId,
            Email     = user.Email,
            FullName  = user.FullName,
            Role      = user.Role,
            TenantId  = user.TenantId,
            IsActive  = user.IsActive,
            CreatedAt = user.CreatedAt
        };

        return CreatedAtAction(nameof(GetById), new { id = user.UserId },
            new ApiResponse<UserSummaryResponse> { Success = true, Data = response });
    }
}
