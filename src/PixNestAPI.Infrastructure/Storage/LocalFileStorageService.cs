using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using PixNestAPI.Application.Interfaces;

namespace PixNestAPI.Infrastructure.Storage;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _basePath;

    public LocalFileStorageService(IConfiguration configuration)
    {
        _basePath = configuration["StorageSettings:BasePath"]
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "MediaBackup");

        Directory.CreateDirectory(_basePath);
        Directory.CreateDirectory(Path.Combine(_basePath, "Thumbnails"));
    }

    public async Task<SavedFile> SaveAsync(Stream content, string username, string extension, CancellationToken ct = default)
    {
        var userPath = Path.Combine(_basePath, username);
        Directory.CreateDirectory(userPath);

        var filePath = Path.Combine(userPath, $"{Guid.NewGuid()}{extension}");

        await using (var fs = new FileStream(filePath, FileMode.Create))
            await content.CopyToAsync(fs, ct);

        var hash = await ComputeHashAsync(filePath);
        return new SavedFile(filePath, hash);
    }

    public Task DeleteAsync(string serverPath)
    {
        if (File.Exists(serverPath))
            File.Delete(serverPath);
        return Task.CompletedTask;
    }

    public long GetAvailableStorage()
    {
        try
        {
            var root = Path.GetPathRoot(_basePath) ?? "/";
            return new DriveInfo(root).AvailableFreeSpace;
        }
        catch
        {
            return -1;
        }
    }

    private static async Task<string> ComputeHashAsync(string filePath)
    {
        using var sha256 = SHA256.Create();
        await using var stream = File.OpenRead(filePath);
        return Convert.ToBase64String(await sha256.ComputeHashAsync(stream));
    }
}
