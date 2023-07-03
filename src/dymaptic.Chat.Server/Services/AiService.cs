using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using dymaptic.Chat.Shared.Data;
using Microsoft.Extensions.Options;

namespace dymaptic.Chat.Server;

public class AiService
{
    public AiService(HttpClient httpClient, IOptions<AiServiceSettings> aiServiceSettingsOptions)
    {
        _httpClient = httpClient;
        _aiServiceSettings = aiServiceSettingsOptions.Value;
    }

    public async Task<Stream> Query(DyRequest request)
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

    private readonly HttpClient _httpClient;
    private readonly AiServiceSettings _aiServiceSettings;
}