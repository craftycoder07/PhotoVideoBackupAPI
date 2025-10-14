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

        // Device Management
        public async Task<Device> RegisterDeviceAsync(DeviceRegistrationRequest request)
        {
            var deviceId = request.DeviceId ?? Guid.NewGuid().ToString();
            var apiKey = GenerateApiKey();
            
            var device = new Device
            {
                Id = deviceId,
                DeviceName = request.DeviceName,
                DeviceModel = request.DeviceModel,
                DeviceId = deviceId,
                ApiKey = apiKey,
                Settings = request.Settings ?? new DeviceSettings()
            };

            _context.Devices.Add(device);
            await _context.SaveChangesAsync();
            
            // Create device storage directory
            var devicePath = Path.Combine(_baseStoragePath, deviceId);
            Directory.CreateDirectory(devicePath);
            
            _logger.LogInformation("Device registered: {DeviceName} ({DeviceId})", device.DeviceName, deviceId);
            
            return device;
        }

        public async Task<Device?> GetDeviceAsync(string deviceId)
        {
            return await _context.Devices
                .Include(d => d.MediaItems)
                .FirstOrDefaultAsync(d => d.Id == deviceId);
        }

        public async Task<List<Device>> GetAllDevicesAsync()
        {
            return await _context.Devices
                .Include(d => d.MediaItems)
                .ToListAsync();
        }

        public async Task<Device> UpdateDeviceSettingsAsync(string deviceId, DeviceSettings settings)
        {
            var device = await _context.Devices.FindAsync(deviceId);
            if (device == null)
            {
                throw new ArgumentException($"Device with ID {deviceId} not found");
            }

            device.Settings = settings;
            device.LastSeen = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Device settings updated for: {DeviceName}", device.DeviceName);
            
            return device;
        }

        public async Task<bool> DeleteDeviceAsync(string deviceId)
        {
            var device = await _context.Devices.FindAsync(deviceId);
            if (device == null)
            {
                return false;
            }

            _context.Devices.Remove(device);
            await _context.SaveChangesAsync();
            
            // Delete device storage directory
            var devicePath = Path.Combine(_baseStoragePath, deviceId);
            if (Directory.Exists(devicePath))
            {
                Directory.Delete(devicePath, true);
            }
            
            _logger.LogInformation("Device deleted: {DeviceName}", device.DeviceName);
            return true;
        }

        // Backup Sessions
        public async Task<BackupSession> StartBackupSessionAsync(string deviceId, BackupSessionInfo sessionInfo)
        {
            var device = await _context.Devices.FindAsync(deviceId);
            if (device == null)
            {
                throw new ArgumentException($"Device with ID {deviceId} not found");
            }

            var session = new BackupSession
            {
                DeviceId = deviceId,
                SessionInfo = sessionInfo
            };

            _context.BackupSessions.Add(session);
            device.LastSeen = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Backup session started for device: {DeviceName} (Session: {SessionId})", 
                device.DeviceName, session.Id);
            
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

        public async Task<List<BackupSession>> GetDeviceBackupSessionsAsync(string deviceId)
        {
            return await _context.BackupSessions
                .Where(s => s.DeviceId == deviceId)
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

            // Generate unique filename
            var fileName = $"{Guid.NewGuid()}{extension}";
            var devicePath = Path.Combine(_baseStoragePath, session.DeviceId);
            var filePath = Path.Combine(devicePath, fileName);

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
                DeviceId = session.DeviceId,
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

            // Update device stats
            var device = await _context.Devices.FindAsync(session.DeviceId);
            if (device != null)
            {
                device.MediaItems.Add(mediaItem);
                device.Stats.TotalSize += file.Length;
                device.Stats.LastBackupDate = DateTime.UtcNow;
                
                if (mediaType == MediaType.Photo)
                    device.Stats.TotalPhotos++;
                else
                    device.Stats.TotalVideos++;
            }

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

        public async Task<List<MediaItem>> GetDeviceMediaAsync(string deviceId, int page = 1, int pageSize = 50)
        {
            return await _context.MediaItems
                .Where(m => m.DeviceId == deviceId)
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
        public async Task<BackupStats> GetDeviceStatsAsync(string deviceId)
        {
            var device = await _context.Devices.FindAsync(deviceId);
            if (device == null)
            {
                throw new ArgumentException($"Device with ID {deviceId} not found");
            }
            
            return device.Stats;
        }

        public async Task<SystemStats> GetSystemStatsAsync()
        {
            var devices = await _context.Devices.ToListAsync();
            var mediaItems = await _context.MediaItems.ToListAsync();
            
            var stats = new SystemStats
            {
                TotalDevices = devices.Count,
                ActiveDevices = devices.Count(d => d.IsActive),
                TotalMediaItems = mediaItems.Count,
                TotalPhotos = mediaItems.Count(m => m.Type == MediaType.Photo),
                TotalVideos = mediaItems.Count(m => m.Type == MediaType.Video),
                TotalStorageUsed = mediaItems.Sum(m => m.FileSize),
                LastBackupActivity = devices.Max(d => d.LastSeen)
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
        public async Task<List<MediaItem>> SearchMediaAsync(string deviceId, string query, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var queryable = _context.MediaItems.Where(m => m.DeviceId == deviceId);
            
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

        public async Task<List<MediaItem>> GetMediaByDateRangeAsync(string deviceId, DateTime fromDate, DateTime toDate)
        {
            return await _context.MediaItems
                .Where(m => m.DeviceId == deviceId)
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