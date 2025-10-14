using Microsoft.EntityFrameworkCore;
using PhotoVideoBackupAPI.Models;

namespace PhotoVideoBackupAPI.Data
{
    public class MediaBackupDbContext : DbContext
    {
        public MediaBackupDbContext(DbContextOptions<MediaBackupDbContext> options) : base(options)
        {
        }

        public DbSet<Device> Devices { get; set; }
        public DbSet<BackupSession> BackupSessions { get; set; }
        public DbSet<MediaItem> MediaItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Device configuration
            modelBuilder.Entity<Device>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasMaxLength(50);
                entity.Property(e => e.DeviceName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.DeviceModel).IsRequired().HasMaxLength(200);
                entity.Property(e => e.DeviceId).HasMaxLength(100);
                entity.Property(e => e.ApiKey).HasMaxLength(100);
                entity.Property(e => e.RegisteredDate).IsRequired();
                entity.Property(e => e.LastSeen).IsRequired();
                entity.Property(e => e.IsActive).IsRequired();

                // Configure JSON column for Settings
                entity.Property(e => e.Settings)
                    .HasConversion(
                        v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions)null),
                        v => System.Text.Json.JsonSerializer.Deserialize<DeviceSettings>(v, (System.Text.Json.JsonSerializerOptions)null) ?? new DeviceSettings()
                    );

                // Configure JSON column for Stats
                entity.Property(e => e.Stats)
                    .HasConversion(
                        v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions)null),
                        v => System.Text.Json.JsonSerializer.Deserialize<BackupStats>(v, (System.Text.Json.JsonSerializerOptions)null) ?? new BackupStats()
                    );

                // Configure one-to-many relationship with MediaItems
                entity.HasMany(e => e.MediaItems)
                    .WithOne()
                    .HasForeignKey(m => m.DeviceId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.DeviceId).IsUnique();
                entity.HasIndex(e => e.ApiKey).IsUnique();
            });

            // BackupSession configuration
            modelBuilder.Entity<BackupSession>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasMaxLength(50);
                entity.Property(e => e.DeviceId).IsRequired().HasMaxLength(50);
                entity.Property(e => e.StartTime).IsRequired();
                entity.Property(e => e.Status).IsRequired();
                entity.Property(e => e.TotalItems).IsRequired();
                entity.Property(e => e.ProcessedItems).IsRequired();
                entity.Property(e => e.SuccessfulBackups).IsRequired();
                entity.Property(e => e.FailedBackups).IsRequired();
                entity.Property(e => e.SkippedItems).IsRequired();
                entity.Property(e => e.TotalSize).IsRequired();
                entity.Property(e => e.ErrorMessage).HasMaxLength(1000);

                // Configure JSON column for SessionInfo
                entity.Property(e => e.SessionInfo)
                    .HasConversion(
                        v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions)null),
                        v => System.Text.Json.JsonSerializer.Deserialize<BackupSessionInfo>(v, (System.Text.Json.JsonSerializerOptions)null) ?? new BackupSessionInfo()
                    );

                // Configure JSON column for Errors
                entity.Property(e => e.Errors)
                    .HasConversion(
                        v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions)null),
                        v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions)null) ?? new List<string>()
                    )
                    .Metadata.SetValueComparer(new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<List<string>>(
                        (c1, c2) => c1!.SequenceEqual(c2!),
                        c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                        c => c.ToList()));

                // Configure one-to-many relationship with MediaItems
                entity.HasMany(e => e.Items)
                    .WithOne()
                    .HasForeignKey(m => m.SessionId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Configure foreign key relationship with Device
                entity.HasOne<Device>()
                    .WithMany()
                    .HasForeignKey(e => e.DeviceId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.DeviceId);
                entity.HasIndex(e => e.StartTime);
                entity.HasIndex(e => e.Status);
            });

            // MediaItem configuration
            modelBuilder.Entity<MediaItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasMaxLength(50);
                entity.Property(e => e.DeviceId).IsRequired().HasMaxLength(50);
                entity.Property(e => e.FileName).IsRequired().HasMaxLength(500);
                entity.Property(e => e.OriginalPath).HasMaxLength(1000);
                entity.Property(e => e.ServerPath).IsRequired().HasMaxLength(1000);
                entity.Property(e => e.FileExtension).IsRequired().HasMaxLength(20);
                entity.Property(e => e.FileSize).IsRequired();
                entity.Property(e => e.CreatedDate).IsRequired();
                entity.Property(e => e.Type).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.Status).IsRequired();
                entity.Property(e => e.ErrorMessage).HasMaxLength(1000);
                entity.Property(e => e.ThumbnailPath).HasMaxLength(1000);
                entity.Property(e => e.IsFavorite).IsRequired();

                // Configure JSON column for Metadata
                entity.Property(e => e.Metadata)
                    .HasConversion(
                        v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions)null),
                        v => System.Text.Json.JsonSerializer.Deserialize<MediaMetadata>(v, (System.Text.Json.JsonSerializerOptions)null) ?? new MediaMetadata()
                    );

                // Configure JSON column for Tags
                entity.Property(e => e.Tags)
                    .HasConversion(
                        v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions)null),
                        v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions)null) ?? new List<string>()
                    )
                    .Metadata.SetValueComparer(new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<List<string>>(
                        (c1, c2) => c1!.SequenceEqual(c2!),
                        c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                        c => c.ToList()));

                // Configure foreign key relationship with Device
                entity.HasOne<Device>()
                    .WithMany(d => d.MediaItems)
                    .HasForeignKey(e => e.DeviceId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.DeviceId);
                entity.HasIndex(e => e.CreatedDate);
                entity.HasIndex(e => e.Type);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.FileName);
            });
        }
    }
}
