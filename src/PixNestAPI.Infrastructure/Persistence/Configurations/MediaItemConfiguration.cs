using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PixNestAPI.Domain.Entities;
using PixNestAPI.Domain.ValueObjects;

namespace PixNestAPI.Infrastructure.Persistence.Configurations;

public class MediaItemConfiguration : IEntityTypeConfiguration<MediaItem>
{
    public void Configure(EntityTypeBuilder<MediaItem> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasMaxLength(50);
        builder.Property(e => e.SessionId).IsRequired().HasMaxLength(50);
        builder.Property(e => e.FileName).IsRequired().HasMaxLength(500);
        builder.Property(e => e.OriginalPath).HasMaxLength(1000);
        builder.Property(e => e.ServerPath).IsRequired().HasMaxLength(1000);
        builder.Property(e => e.FileExtension).IsRequired().HasMaxLength(20);
        builder.Property(e => e.Description).HasMaxLength(1000);
        builder.Property(e => e.ErrorMessage).HasMaxLength(1000);
        builder.Property(e => e.ThumbnailPath).HasMaxLength(1000);

        builder.Property(e => e.Metadata)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<MediaMetadata>(v, (JsonSerializerOptions?)null) ?? new MediaMetadata());

        builder.Property(e => e.Tags)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
            .Metadata.SetValueComparer(new ValueComparer<List<string>>(
                (c1, c2) => c1!.SequenceEqual(c2!),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c.ToList()));

        builder.HasIndex(e => e.SessionId);
        builder.HasIndex(e => e.CreatedDate);
        builder.HasIndex(e => e.Type);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.FileName);
    }
}
