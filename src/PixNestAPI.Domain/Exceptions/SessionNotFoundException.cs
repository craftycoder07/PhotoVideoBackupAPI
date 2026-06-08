namespace PixNestAPI.Domain.Exceptions;

public class SessionNotFoundException : Exception
{
    public SessionNotFoundException(string sessionId)
        : base($"Backup session with ID '{sessionId}' was not found.") { }
}
