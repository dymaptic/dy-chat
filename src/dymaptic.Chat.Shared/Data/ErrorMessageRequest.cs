namespace dymaptic.Chat.Shared.Data
{
    public record ErrorMessageRequest(Guid ErrorToken, string? ExceptionMessage, string? ExceptionStackTrack, string? ExceptionInnerException)
    {
    }

}
