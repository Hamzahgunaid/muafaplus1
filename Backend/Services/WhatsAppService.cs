using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace MuafaPlus.Services;

/// <summary>
/// Phase 2: Sends WhatsApp messages via Meta WhatsApp Cloud API (v21.0).
/// All methods return (bool Success, string? ErrorMessage) — never throw.
///
/// TestMode = true (default on Railway until production approval):
///   Sends the approved hello_world template instead of free-form text.
///   Free-form text requires an approved template or an active 24-hour
///   conversation window with the recipient.
/// </summary>
public class WhatsAppService
{
    private readonly HttpClient        _http;
    private readonly IConfiguration    _config;
    private readonly ILogger<WhatsAppService> _logger;

    private string PhoneNumberId      => _config["WhatsApp:PhoneNumberId"] ?? "1112172131979263";
    private string AccessToken        => _config["WhatsApp:AccessToken"]   ?? string.Empty;
    private bool   TestMode           => _config.GetValue<bool>("WhatsApp:TestMode", true);

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
    // Message 1 — Summary article
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Sends the full Stage 1 summary article to the patient's WhatsApp.
    /// In TestMode uses the approved hello_world template (test numbers only
    /// support approved templates to non-verified recipients).
    /// </summary>
    public async Task<(bool Success, string? ErrorMessage)> SendSummaryMessageAsync(
        string toPhone,
        string summaryText,
        string physicianName,
        string riskLevel)
    {
        if (TestMode)
        {
            _logger.LogInformation(
                "WHATSAPP TEST MODE: Would send summary to {Phone}: {Preview}",
                toPhone,
                summaryText.Length > 100 ? summaryText[..100] + "…" : summaryText);

            return await SendTemplateAsync(toPhone, "hello_world", "en_US");
        }

        var body = $"🏥 معافى بلس — تثقيفك الصحي\n\n" +
                   $"من: د. {physicianName}\n" +
                   $"مستوى الخطر: {riskLevel}\n\n" +
                   $"{summaryText}\n\n" +
                   $"📱 لقراءة المقالات التفصيلية، حمّل تطبيق معافى+\n" +
                   $"⚠️ هذا المحتوى للتثقيف الصحي فقط وليس استشارة طبية.";

        return await SendTextAsync(toPhone, body);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Message 2 — Access code
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Sends the 4-digit access code as a standalone WhatsApp message.
    /// In TestMode uses the approved hello_world template.
    /// </summary>
    public async Task<(bool Success, string? ErrorMessage)> SendAccessCodeAsync(
        string toPhone,
        string accessCode,
        string patientName)
    {
        if (TestMode)
        {
            _logger.LogInformation(
                "WHATSAPP TEST MODE: Would send access code {Code} to {Phone}",
                accessCode, toPhone);

            return await SendTemplateAsync(toPhone, "hello_world", "en_US");
        }

        var body = $"🔐 رمز الوصول الخاص بك في تطبيق معافى+:\n\n" +
                   $"*{accessCode}*\n\n" +
                   $"أدخل هذا الرمز مع رقم هاتفك للوصول إلى محتواك الصحي.";

        return await SendTextAsync(toPhone, body);
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

    private async Task<(bool Success, string? ErrorMessage)> SendTemplateAsync(
        string toPhone, string templateName, string languageCode)
    {
        var payload = new
        {
            messaging_product = "whatsapp",
            to   = toPhone,
            type = "template",
            template = new
            {
                name     = templateName,
                language = new { code = languageCode }
            }
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
                "WhatsApp message sent — to:{Phone} status:{Status}",
                payload.ToString(), (int)response.StatusCode);

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WhatsApp API exception: {Message}", ex.Message);
            return (false, ex.Message);
        }
    }
}
