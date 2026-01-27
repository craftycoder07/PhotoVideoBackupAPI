using PhotoVideoBackupAPI.Models;
using PhotoVideoBackupAPI.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using SkiaSharp;

namespace PhotoVideoBackupAPI.Services
{
    public class MediaBackupService : IMediaBackupService
    {
        private readonly MediaBackupDbContext _context;
        private readonly ILogger<MediaBackupService> _logger;
        private readonly string _baseStoragePath;
        private readonly string _thumbnailsPath;

        private static readonly string[] PhotoExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".webp", ".heic", ".heif" };
        private static readonly string[] VideoExtensions = { ".mp4", ".avi", ".mov", ".wmv", ".flv", ".webm", ".mkv", ".m4v", ".3gp", ".mpg", ".mpeg" };

        public MediaBackupService(MediaBackupDbContext context, ILogger<MediaBackupService> logger, IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _baseStoragePath = configuration["StorageSettings:BasePath"] ?? 
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "MediaBackup");
            _thumbnailsPath = Path.Combine(_baseStoragePath, "Thumbnails");
            
            // Ensure directories exist
            Directory.CreateDirectory(_baseStoragePath);
            Directory.CreateDirectory(_thumbnailsPath);
        }

        // User Management
        public async Task<User?> GetUserAsync(string userId)
        {
            return await _context.Users.FindAsync(userId);
        }

        public async Task<User> UpdateUserSettingsAsync(string userId, DeviceSettings settings)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                throw new ArgumentException($"User with ID {userId} not found");
            }

            user.Settings = settings;
            user.LastSeen = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("User settings updated for: {Username}", user.Username);
            
            return user;
        }

        // Backup Sessions
        public async Task<BackupSession> StartBackupSessionAsync(string userId, BackupSessionInfo sessionInfo)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                throw new ArgumentException($"User with ID {userId} not found");
            }

            var session = new BackupSession
            {
                UserId = userId,
                SessionInfo = sessionInfo
            };

            _context.BackupSessions.Add(session);
            user.LastSeen = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Backup session started for user: {Username} (Session: {SessionId})", 
                user.Username, session.Id);
            
            return session;
        }

        public async Task<BackupSession> GetBackupSessionAsync(string sessionId)
        {
            var session = await _context.BackupSessions
                .Include(s => s.Items)
                .FirstOrDefaultAsync(s => s.Id == sessionId);
            
            if (session == null)
            {
                throw new ArgumentException($"Session with ID {sessionId} not found");
            }
            
            return session;
        }

        public async Task<BackupSession> UpdateBackupSessionAsync(string sessionId, BackupSessionUpdateRequest request)
        {
            var session = await _context.BackupSessions.FindAsync(sessionId);
            if (session == null)
            {
                throw new ArgumentException($"Session with ID {sessionId} not found");
            }

            if (request.ProcessedItems.HasValue) session.ProcessedItems = request.ProcessedItems.Value;
            if (request.SuccessfulBackups.HasValue) session.SuccessfulBackups = request.SuccessfulBackups.Value;
            if (request.FailedBackups.HasValue) session.FailedBackups = request.FailedBackups.Value;
            if (request.SkippedItems.HasValue) session.SkippedItems = request.SkippedItems.Value;
            if (request.TotalSize.HasValue) session.TotalSize = request.TotalSize.Value;
            if (request.Status.HasValue) session.Status = request.Status.Value;
            if (!string.IsNullOrEmpty(request.ErrorMessage)) session.ErrorMessage = request.ErrorMessage;

            if (session.Status == SessionStatus.Completed || session.Status == SessionStatus.Failed)
            {
                session.EndTime = DateTime.UtcNow;
            }
            
            await _context.SaveChangesAsync();
            
            return session;
        }

        public async Task<List<BackupSession>> GetUserBackupSessionsAsync(string userId)
        {
            return await _context.BackupSessions
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.StartTime)
                .ToListAsync();
        }

        // Media Upload
        public async Task<MediaItem> UploadMediaAsync(string sessionId, IFormFile file, MediaMetadata? metadata = null)
        {
            var session = await _context.BackupSessions.FindAsync(sessionId);
            if (session == null)
            {
                throw new ArgumentException($"Session with ID {sessionId} not found");
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var mediaType = PhotoExtensions.Contains(extension) ? MediaType.Photo : MediaType.Video;
            
            _logger.LogDebug("Upload received: {FileName} ({Extension}) - ContentType: {ContentType}, Size: {Size} bytes", 
                file.FileName, extension, file.ContentType, file.Length);
            
            // Validate file size
            if (file.Length > 100 * 1024 * 1024) // 100MB limit
            {
                throw new InvalidOperationException("File size exceeds maximum allowed size");
            }

            // Get user to access username for storage path
            var user = await _context.Users.FindAsync(session.UserId);
            if (user == null)
            {
                throw new ArgumentException($"User with ID {session.UserId} not found");
            }

            // Generate unique filename
            var fileName = $"{Guid.NewGuid()}{extension}";
            // Use username for storage path instead of userId
            var userPath = Path.Combine(_baseStoragePath, user.Username);
            Directory.CreateDirectory(userPath);
            var filePath = Path.Combine(userPath, fileName);

            // Save file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            
            // Verify file was saved correctly
            var savedFileInfo = new FileInfo(filePath);
            if (!savedFileInfo.Exists || savedFileInfo.Length == 0)
            {
                throw new IOException($"Failed to save file: {filePath}");
            }
            
            _logger.LogDebug("File saved successfully: {Path} ({Size} bytes, Content-Type: {ContentType})", 
                filePath, savedFileInfo.Length, file.ContentType);

            // Create media item
            var mediaItem = new MediaItem
            {
                SessionId = sessionId,
                FileName = file.FileName,
                ServerPath = filePath,
                FileExtension = extension,
                FileSize = file.Length,
                Type = mediaType,
                Metadata = metadata ?? new MediaMetadata(),
                Status = BackupStatus.Completed
            };

            // Calculate file hash
            mediaItem.Metadata.FileHash = await CalculateFileHashAsync(filePath);

            _context.MediaItems.Add(mediaItem);
            
            // Update session
            session.Items.Add(mediaItem);
            session.TotalItems++;
            session.SuccessfulBackups++;
            session.TotalSize += file.Length;

            // Update user stats
            user.Stats.TotalSize += file.Length;
            user.Stats.LastBackupDate = DateTime.UtcNow;
            
            if (mediaType == MediaType.Photo)
                user.Stats.TotalPhotos++;
            else
                user.Stats.TotalVideos++;
            
            if (mediaItem.Status == BackupStatus.Completed)
                user.Stats.SuccessfulBackups++;
            else if (mediaItem.Status == BackupStatus.Failed)
                user.Stats.FailedBackups++;
            
            // Mark the user as modified so EF detects the JSON changes
            _context.Entry(user).Property(u => u.Stats).IsModified = true;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Media uploaded: {FileName} ({FileSize} bytes)", file.FileName, file.Length);
            
            return mediaItem;
        }

        public async Task<MediaItem> GetMediaItemAsync(string mediaId)
        {
            var mediaItem = await _context.MediaItems.FindAsync(mediaId);
            if (mediaItem == null)
            {
                throw new ArgumentException($"Media item with ID {mediaId} not found");
            }
            
            return mediaItem;
        }

        public async Task<List<MediaItem>> GetUserMediaAsync(string userId, int page = 1, int pageSize = 50)
        {
            return await _context.MediaItems
                .Include(m => m.Session)
                .Where(m => m.Session.UserId == userId)
                .OrderByDescending(m => m.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<bool> DeleteMediaItemAsync(string mediaId)
        {
            var mediaItem = await _context.MediaItems.FindAsync(mediaId);
            if (mediaItem == null)
            {
                return false;
            }

            _context.MediaItems.Remove(mediaItem);
            await _context.SaveChangesAsync();
            
            // Delete file
            if (File.Exists(mediaItem.ServerPath))
            {
                File.Delete(mediaItem.ServerPath);
            }
            
            // Delete thumbnail if exists
            if (!string.IsNullOrEmpty(mediaItem.ThumbnailPath) && File.Exists(mediaItem.ThumbnailPath))
            {
                File.Delete(mediaItem.ThumbnailPath);
            }
            
            _logger.LogInformation("Media item deleted: {FileName}", mediaItem.FileName);
            return true;
        }

        // Thumbnails
        public async Task<string> GenerateThumbnailAsync(string mediaId)
        {
            var mediaItem = await _context.MediaItems.FindAsync(mediaId);
            if (mediaItem == null)
            {
                throw new ArgumentException($"Media item with ID {mediaId} not found");
            }

            var thumbnailPath = Path.Combine(_thumbnailsPath, $"{mediaId}_thumb.jpg");
            
            // Skip if thumbnail already exists
            if (File.Exists(thumbnailPath))
            {
                mediaItem.ThumbnailPath = thumbnailPath;
                await _context.SaveChangesAsync();
                return thumbnailPath;
            }

            // Check if source file exists
            if (!File.Exists(mediaItem.ServerPath))
            {
                throw new FileNotFoundException($"Source file not found: {mediaItem.ServerPath}");
            }

            try
            {
                // For photos, resize to thumbnail
                if (mediaItem.Type == MediaType.Photo)
                {
                    try
                    {
                        // Check file exists and has content
                        var fileInfo = new FileInfo(mediaItem.ServerPath);
                        if (!fileInfo.Exists)
                        {
                            throw new FileNotFoundException($"Image file not found: {mediaItem.ServerPath}");
                        }
                        
                        if (fileInfo.Length == 0)
                        {
                            throw new InvalidDataException($"Image file is empty: {mediaItem.ServerPath}");
                        }
                        
                        _logger.LogDebug("Decoding image: {Path} ({Size} bytes)", mediaItem.ServerPath, fileInfo.Length);
                        
                        // Check file magic bytes to verify it's actually a JPEG
                        byte[] header = new byte[12];
                        using (var fs = File.OpenRead(mediaItem.ServerPath))
                        {
                            await fs.ReadAsync(header, 0, 12);
                        }
                        
                        string magicBytes = BitConverter.ToString(header).Replace("-", " ");
                        _logger.LogDebug("File magic bytes: {MagicBytes}", magicBytes);
                        
                        // JPEG starts with FF D8 FF
                        bool isJpeg = header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;
                        // PNG starts with 89 50 4E 47
                        bool isPng = header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47;
                        
                        if (!isJpeg && !isPng)
                        {
                            _logger.LogError("File is not JPEG or PNG. Magic bytes: {MagicBytes}", magicBytes);
                            throw new InvalidDataException($"File format not recognized. Expected JPEG or PNG, got magic bytes: {magicBytes}");
                        }
                        
                        _logger.LogDebug("File format verified: {Format}", isJpeg ? "JPEG" : "PNG");
                        
                        // Try to decode the image
                        using var originalBitmap = SKBitmap.Decode(mediaItem.ServerPath);
                        
                        if (originalBitmap == null)
                        {
                            // File exists but can't be decoded - log details for debugging
                            _logger.LogError("SkiaSharp decode failed: {Path} ({Extension}, {Size} bytes, MagicBytes: {MagicBytes})", 
                                mediaItem.ServerPath, mediaItem.FileExtension, fileInfo.Length, magicBytes);
                            throw new InvalidDataException($"Unable to decode image with SkiaSharp: {mediaItem.ServerPath}. File may be corrupted.");
                        }
                        
                        _logger.LogDebug("Image decoded successfully: {Width}x{Height}", originalBitmap.Width, originalBitmap.Height);
                        
                        // Calculate thumbnail size (max 300px on longest side)
                        int maxSize = 300;
                        var ratio = (double)maxSize / Math.Max(originalBitmap.Width, originalBitmap.Height);
                        var newWidth = (int)(originalBitmap.Width * ratio);
                        var newHeight = (int)(originalBitmap.Height * ratio);

                        // Create resized bitmap with high quality
                        var imageInfo = new SKImageInfo(newWidth, newHeight, SKColorType.Rgba8888, SKAlphaType.Premul);
                        var samplingOptions = new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear);
                        using var resizedBitmap = originalBitmap.Resize(imageInfo, samplingOptions);
                        
                        if (resizedBitmap == null)
                        {
                            throw new InvalidOperationException("Failed to resize image");
                        }

                        // Save as JPEG with quality 85
                        using var image = SKImage.FromBitmap(resizedBitmap);
                        using var data = image.Encode(SKEncodedImageFormat.Jpeg, 85);
                        
                        using var outputStream = File.OpenWrite(thumbnailPath);
                        data.SaveTo(outputStream);
                        
                        _logger.LogInformation("Generated thumbnail for photo {MediaId} ({Extension}) - {Width}x{Height} from {OrigWidth}x{OrigHeight}", 
                            mediaId, mediaItem.FileExtension, newWidth, newHeight, originalBitmap.Width, originalBitmap.Height);
                    }
                    catch (Exception ex) when (ex is not NotSupportedException)
                    {
                        _logger.LogError(ex, "Failed to process image for {MediaId} ({Extension}). Ensure iOS app converts to JPEG before upload.", 
                            mediaId, mediaItem.FileExtension);
                        throw new NotSupportedException($"Image format not supported: {mediaItem.FileExtension}. Please ensure images are in JPEG format.", ex);
                    }
                }
                else if (mediaItem.Type == MediaType.Video)
                {
                    // For videos, for now just copy a placeholder or use first frame
                    // You could use FFMpeg here for proper video thumbnails
                    _logger.LogWarning("Video thumbnail generation not fully implemented for {MediaId}, using placeholder", mediaId);
                    
                    // For now, just note it's not available - the client will handle this
                    // Or you can create a placeholder image
                    throw new NotImplementedException("Video thumbnail generation requires FFMpeg");
                }
                
                mediaItem.ThumbnailPath = thumbnailPath;
                await _context.SaveChangesAsync();
                
                return thumbnailPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate thumbnail for {MediaId}", mediaId);
                throw;
            }
        }

        public async Task<byte[]> GetThumbnailAsync(string mediaId)
        {
            var mediaItem = await _context.MediaItems.FindAsync(mediaId);
            if (mediaItem == null)
            {
                throw new ArgumentException($"Media item with ID {mediaId} not found");
            }

            // If thumbnail doesn't exist, generate it on-the-fly
            if (string.IsNullOrEmpty(mediaItem.ThumbnailPath) || !File.Exists(mediaItem.ThumbnailPath))
            {
                _logger.LogInformation("Thumbnail not found for {MediaId}, generating now...", mediaId);
                await GenerateThumbnailAsync(mediaId);
                
                // Refresh mediaItem from database to get updated ThumbnailPath
                mediaItem = await _context.MediaItems.FindAsync(mediaId);
            }

            if (string.IsNullOrEmpty(mediaItem?.ThumbnailPath))
            {
                throw new FileNotFoundException($"Unable to generate or find thumbnail for media {mediaId}");
            }

            return await File.ReadAllBytesAsync(mediaItem.ThumbnailPath);
        }

        // Statistics
        public async Task<BackupStats> GetUserStatsAsync(string userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                throw new ArgumentException($"User with ID {userId} not found");
            }
            
            // Return stats from user object (stored in database)
            return user.Stats;
        }

        public async Task<SystemStats> GetSystemStatsAsync()
        {
            var users = await _context.Users.ToListAsync();
            var mediaItems = await _context.MediaItems.ToListAsync();
            
            var stats = new SystemStats
            {
                TotalUsers = users.Count,
                ActiveUsers = users.Count(u => u.IsActive),
                TotalMediaItems = mediaItems.Count,
                TotalPhotos = mediaItems.Count(m => m.Type == MediaType.Photo),
                TotalVideos = mediaItems.Count(m => m.Type == MediaType.Video),
                TotalStorageUsed = mediaItems.Sum(m => m.FileSize),
                LastBackupActivity = users.Any() ? users.Max(u => u.LastSeen) : DateTime.MinValue
            };

            // Calculate available storage
            try
            {
                var driveInfo = new DriveInfo(Path.GetPathRoot(_baseStoragePath) ?? "/");
                stats.AvailableStorage = driveInfo.AvailableFreeSpace;
            }
            catch
            {
                stats.AvailableStorage = -1;
            }
            
            return stats;
        }

        // Search and filtering
        public async Task<List<MediaItem>> SearchUserMediaAsync(string userId, string query, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var queryable = _context.MediaItems
                .Include(m => m.Session)
                .Where(m => m.Session.UserId == userId);
            
            if (!string.IsNullOrEmpty(query))
            {
                queryable = queryable.Where(m => 
                    m.FileName.Contains(query) || 
                    (m.Description != null && m.Description.Contains(query)));
            }
            
            if (fromDate.HasValue)
            {
                queryable = queryable.Where(m => m.CreatedDate >= fromDate.Value);
            }
            
            if (toDate.HasValue)
            {
                queryable = queryable.Where(m => m.CreatedDate <= toDate.Value);
            }
            
            return await queryable
                .OrderByDescending(m => m.CreatedDate)
                .ToListAsync();
        }

        public async Task<List<MediaItem>> GetMediaByDateRangeAsync(string userId, DateTime fromDate, DateTime toDate)
        {
            return await _context.MediaItems
                .Include(m => m.Session)
                .Where(m => m.Session.UserId == userId)
                .Where(m => m.CreatedDate >= fromDate && m.CreatedDate <= toDate)
                .OrderByDescending(m => m.CreatedDate)
                .ToListAsync();
        }

        // Helper methods
        private string GenerateApiKey()
        {
            return Convert.ToBase64String(Guid.NewGuid().ToByteArray()).Replace("/", "_").Replace("+", "-").Substring(0, 22);
        }

        private async Task<string> CalculateFileHashAsync(string filePath)
        {
            using var sha256 = SHA256.Create();
            await using var stream = File.OpenRead(filePath);
            var hash = await sha256.ComputeHashAsync(stream);
            return Convert.ToBase64String(hash);
        }
    }
}