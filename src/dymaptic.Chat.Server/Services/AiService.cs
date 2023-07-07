using dymaptic.Chat.Shared.Data;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace dymaptic.Chat.Server;

public class AiService
{
    public AiService(HttpClient httpClient, IOptions<AiServiceSettings> aiServiceSettingsOptions, ILogger<AiService> logger)
    {
        _httpClient = httpClient;
        _aiServiceSettings = aiServiceSettingsOptions.Value;
        _logger = logger;
    }

    public async Task<Stream> Query(DyRequest request)
    {
        try
        {
            SkyNetRequest snRequest = new SkyNetRequest(new SkyNetChatMessages(request.Messages), request.Context!);
            HttpRequestMessage requestBody = new HttpRequestMessage(HttpMethod.Post, _aiServiceSettings.Url);
            requestBody.Content = new StringContent(JsonSerializer.Serialize(snRequest),
                Encoding.UTF8, "application/json");
            requestBody.Headers.Add("Authorization", $"Bearer {_aiServiceSettings.Token}");
            var response = await _httpClient.SendAsync(requestBody,
                HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStreamAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error With AI Service");
            throw;
        }
    }

    private readonly HttpClient _httpClient;
    private readonly AiServiceSettings _aiServiceSettings;
    private readonly ILogger<AiService> _logger;
}