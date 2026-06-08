namespace PixNestAPI.Domain.Exceptions;

public class MediaItemNotFoundException : Exception
{
    public MediaItemNotFoundException(string mediaId)
        : base($"Media item with ID '{mediaId}' was not found.") { }
}
