using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using tickets_management.Services.Interfaces;

namespace tickets_management.Services;

public class N8nService : IN8nService
{
    private readonly HttpClient _http;
    private readonly string _webhookUrl;
    private readonly string _secret;
    private readonly ILogger<N8nService> _logger;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public N8nService(HttpClient http, IConfiguration config, ILogger<N8nService> logger)
    {
        _http = http;
        _webhookUrl = config["N8n:WebhookUrl"] ?? throw new InvalidOperationException("N8n:WebhookUrl not configured");
        _secret = config["N8n:Secret"] ?? throw new InvalidOperationException("N8n:Secret not configured");
        _logger = logger;
    }

    public async Task SendTicketPurchaseNotificationAsync(TicketPurchasePayload payload)
    {
        try
        {
            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var request = new HttpRequestMessage(HttpMethod.Post, _webhookUrl)
            {
                Content = content
            };
            request.Headers.Add("x-ticketzone-secret", _secret);

            var response = await _http.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("n8n webhook returned {StatusCode}: {Body}", (int)response.StatusCode, body);
            }
            else
            {
                _logger.LogInformation("n8n ticket purchase notification sent successfully for order {OrderId}", payload.Order.Id);
            }
        }
        catch (Exception ex)
        {
            // No propagamos la excepción para que un fallo del webhook no rompa el checkout
            _logger.LogError(ex, "Failed to send n8n ticket purchase notification");
        }
    }
}
