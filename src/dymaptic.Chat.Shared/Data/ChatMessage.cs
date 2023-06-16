namespace dymaptic.Chat.Shared.Data;


public record DyChatMessage(string Content, DyChatSenderType SenderType, string? Username = null)
{
    public string? Content { get; set; } = Content;
}

public record SkyNetChatMessages(List<DyChatMessage> Messages);
public record DyField(string Name, string Alias, string DataType);
public record DyLayer(string Name, List<DyField> Fields);
public record DyChatContext(List<DyLayer> Layers, string CurrentLayer);

public record DyUserInfo(string? Username, string? OrganizationId, string? PortalUri, string? UserToken);

public record DyRequest(List<DyChatMessage> Messages, DyChatContext? Context, DyUserInfo UserInfo);
public record SkyNetRequest(SkyNetChatMessages Messages, DyChatContext Context);
public enum DyChatSenderType
{
    User,
    Bot
}
