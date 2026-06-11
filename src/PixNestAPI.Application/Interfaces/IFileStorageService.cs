namespace PixNestAPI.Application.Interfaces;

public record SavedFile(string ServerPath, string FileHash);

public interface IFileStorageService
{
    Task<SavedFile> SaveAsync(Stream content, string username, string extension, CancellationToken ct = default);
    Task DeleteAsync(string serverPath);
    long GetAvailableStorage();
}
