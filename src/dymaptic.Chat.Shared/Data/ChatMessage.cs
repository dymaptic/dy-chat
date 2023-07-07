namespace dymaptic.Chat.Shared.Data;


public record DyChatMessage(string Content, DyChatSenderType SenderType, string? Username = null)
{
    public string? Content { get; set; } = Content;
}

public record SkyNetChatMessages(List<DyChatMessage> Messages);
public record DyField(string Name, string Alias, string DataType);
public record DyLayer(string Name, List<DyField> Fields);

public class DyChatContext
{
    public DyChatContext(List<DyLayer> layers, string? currentLayer)
    {
        Layers = layers;
        CurrentLayer = currentLayer;
    }
    public string? CurrentLayer { get; set; }

    public List<DyLayer> Layers { get; set; }
};

public record DyUserInfo(string? Username, string? OrganizationId, string? PortalUri, string? UserToken);

public record DyRequest(List<DyChatMessage> Messages, DyChatContext? Context, DyUserInfo UserInfo);
public record SkyNetRequest(SkyNetChatMessages Messages, DyChatContext Context);
public enum DyChatSenderType
{
    User,
    Bot
}

public static class SystemMessages
{
    public static string Forbidden = "Sorry, the chat app is in a closed beta, if you would like to join please email us at info@dymaptic.com!";

    public static string Error = "Sorry, something went wrong, please try again later!";

}

