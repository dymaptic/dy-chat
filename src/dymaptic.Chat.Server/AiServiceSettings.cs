public class AiServiceSettings
{
    public required string Url { get; set; }
    public required string Token { get; set; }
}

public class ArcGIS
{
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
    public string PortalUrl { get; set; }
    public string[] ValidOrgIds { get; set; }
}
