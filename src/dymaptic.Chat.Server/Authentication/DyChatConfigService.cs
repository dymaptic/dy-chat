using dymaptic.ArcGIS.Authentication.AppSupport;

namespace dymaptic.Chat.Server.Authentication;

public class DyChatConfigService : IAppConfigService
{

    private readonly IConfiguration _configuration;

    public DyChatConfigService(IConfiguration configuration)
    {
        this._configuration = configuration;
    }
    public string GetPortalRootUrl()
    {
        return _configuration["ArcGIS:PortalUrl"] ?? string.Empty;
    }
}