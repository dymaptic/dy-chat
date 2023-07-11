using dymaptic.ArcGIS.Authentication.AppSupport;

namespace dymaptic.Chat.Server.Authentication;

public class DyChatConfigService : IAppConfigService
{
    public DyChatConfigService(IConfiguration configuration, ILogger<DyChatConfigService> logger)
    {
        this._configuration = configuration;
        this._logger = logger;
    }
    public string GetPortalRootUrl()
    {
        try
        {
            return _configuration["ArcGIS:PortalUrl"] ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error Getting Portal Root Url, please recheck configuration settings");
            throw;
        }


    }

    private readonly IConfiguration _configuration;
    public readonly ILogger<DyChatConfigService> _logger;
}