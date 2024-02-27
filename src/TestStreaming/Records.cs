public enum DyPromptType
{
    ArcadePopups
}

public enum DyChatMessageType
{
    User,
    Bot
}
// Record that contains a chat message type of content
public record DyChatMessage(string Content, DyChatMessageType Sender);

public record DyChatMessages(List<DyChatMessage> Messages);

public record DyField(string Name, string Alias, string DataType);
public record DyLayer(string Name, List<DyField> Fields);
public record DyChatContext(List<DyLayer> Layers, string CurrentLayer);


public record DyRequest(DyChatMessages Messages, DyChatContext Context);