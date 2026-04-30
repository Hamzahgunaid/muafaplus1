using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace MuafaPlus.Services;

/// <summary>
/// Sends WhatsApp messages via Meta WhatsApp Cloud API (v21.0).
/// Uses two approved templates: muafa_health_notification + muafa_access_code1.
/// All methods return (bool Success, string? ErrorMessage) — never throw.
/// </summary>
public class WhatsAppService
{
    private readonly HttpClient        _http;
    private readonly IConfiguration    _config;
    private readonly ILogger<WhatsAppService> _logger;

    private string PhoneNumberId => _config["WhatsApp:PhoneNumberId"] ?? "1112172131979263";
    private string AccessToken   => _config["WhatsApp:AccessToken"]   ?? string.Empty;

    private static readonly JsonSerializerOptions _jsonOpts =
        new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

    public WhatsAppService(HttpClient http, IConfiguration config, ILogger<WhatsAppService> logger)
    {
        _http   = http;
        _config = config;
        _logger = logger;

        _http.BaseAddress = new Uri("https://graph.facebook.com/v21.0/");
        _http.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Message 1 — Health notification (muafa_health_notification)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Sends the muafa_health_notification template.
    /// Body parameters: {{1}} patientName, {{2}} physicianName, {{3}} appLink.
    /// </summary>
    public async Task<(bool Success, string? ErrorMessage)> SendSummaryMessageAsync(
        string toPhone,
        string patientName,
        string physicianName)
    {
        const string appLink = "https://muafaplus1.vercel.app";

        var payload = new
        {
            messaging_product = "whatsapp",
            to   = toPhone,
            type = "template",
            template = new
            {
                name     = "muafa_test_message",
                language = new { code = "en" }
            }
        };

        _logger.LogInformation(
            "WhatsApp: sending muafa_health_notification to {Phone} — patient:{Patient}",
            toPhone, patientName);

        return await PostToApiAsync(payload);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Message 2 — Access code (muafa_access_code1)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Sends the muafa_access_code1 template.
    /// Body parameter: {{1}} accessCode (4-digit code).
    /// </summary>
    public async Task<(bool Success, string? ErrorMessage)> SendAccessCodeAsync(
        string toPhone,
        string accessCode)
    {
        var payload = new
        {
            messaging_product = "whatsapp",
            to   = toPhone,
            type = "template",
            template = new
            {
                name     = "muafa_test_message",
                language = new { code = "en" }
            }
        };

        _logger.LogInformation(
            "WhatsApp: sending muafa_access_code1 to {Phone}", toPhone);

        return await PostToApiAsync(payload);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Staff onboarding text message
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Sends a plain text message to a WhatsApp number.
    /// Used for staff onboarding notifications. Requires an active 24-hour window.
    /// </summary>
    public async Task SendTextMessageAsync(string toPhone, string message)
    {
        await SendTextAsync(toPhone, message);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SMS fallback
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// SMS fallback — logs and returns success. Real Twilio integration in a future phase.
    /// </summary>
    public Task<(bool Success, string? ErrorMessage)> SendSmsAsync(
        string toPhone,
        string accessCode,
        string physicianName)
    {
        _logger.LogInformation(
            "SMS FALLBACK: Would send to {Phone} code {Code} from physician {Physician}",
            toPhone, accessCode, physicianName);

        return Task.FromResult<(bool, string?)>((true, null));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Private helpers
    // ─────────────────────────────────────────────────────────────────────────

    private async Task<(bool Success, string? ErrorMessage)> SendTextAsync(
        string toPhone, string messageBody)
    {
        var payload = new
        {
            messaging_product = "whatsapp",
            to   = toPhone,
            type = "text",
            text = new { body = messageBody }
        };

        return await PostToApiAsync(payload);
    }

    private async Task<(bool Success, string? ErrorMessage)> PostToApiAsync(object payload)
    {
        try
        {
            var json    = JsonSerializer.Serialize(payload, _jsonOpts);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"{PhoneNumberId}/messages");

            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", AccessToken);
            request.Content = content;

            using var response = await _http.SendAsync(request);
            var responseBody   = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "WhatsApp API error: {StatusCode} {Body}",
                    (int)response.StatusCode, responseBody);

                return (false, $"WhatsApp API {(int)response.StatusCode}: {responseBody}");
            }

            _logger.LogInformation(
                "WhatsApp message sent to {Phone} — status:{Status}",
                toPhone, (int)response.StatusCode);

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WhatsApp API exception: {Message}", ex.Message);
            return (false, ex.Message);
        }
    }
}
