namespace dymaptic.Chat.Shared.Data;

public class ChatMessage
{
    public ChatMessage()
    {

    }

    public ChatMessage(string username, string body, bool isMine)
    {
        Username = username;
        Body = body;
        IsMine = isMine;
    }

    public string Username { get; set; }
    public string Body { get; set; }
    public bool IsMine { get; set; }

    public bool IsNotice => Body?.StartsWith("[Notice]") ?? false;
    // ask about the CSS down here, assume that it is user specified
    public string CSS => IsMine ? "sent" : "received";
}
