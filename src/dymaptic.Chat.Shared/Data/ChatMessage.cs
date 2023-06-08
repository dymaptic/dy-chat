namespace dymaptic.Chat.Shared.Data;


public record DyChatMessage(string Content, DyChatSenderType SenderType, string? Username = null);
public record SkyNetChatMessages(List<DyChatMessage> Messages);
public record DyField(string Name, string Alias, string DataType);
public record DyLayer(string Name, List<DyField> Fields);
public record DyChatContext(List<DyLayer> Layers, string CurrentLayer);

public record DyRequest(List<DyChatMessage> Messages, DyChatContext Context);
public record SkyNetRequest(SkyNetChatMessages Messages, DyChatContext Context);
public enum DyChatSenderType
{
    User,
    Bot
}
