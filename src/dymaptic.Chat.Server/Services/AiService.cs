using Microsoft.Extensions.Options;

namespace dymaptic.Chat.Server;

public class AiService
{
    public AiService(HttpClient httpClient, IOptions<AiServiceSettings> aiServiceSettingsOptions)
    {
        _httpClient = httpClient;
        _aiServiceSettings = aiServiceSettingsOptions.Value;
    }

    private readonly HttpClient _httpClient;
    private readonly AiServiceSettings _aiServiceSettings;
}