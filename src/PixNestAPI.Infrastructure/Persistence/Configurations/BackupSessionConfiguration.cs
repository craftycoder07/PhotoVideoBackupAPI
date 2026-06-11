using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PixNestAPI.Domain.Entities;
using PixNestAPI.Domain.ValueObjects;

namespace PixNestAPI.Infrastructure.Persistence.Configurations;

public class BackupSessionConfiguration : IEntityTypeConfiguration<BackupSession>
{
    public void Configure(EntityTypeBuilder<BackupSession> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasMaxLength(50);
        builder.Property(e => e.UserId).IsRequired().HasMaxLength(50);
        builder.Property(e => e.ErrorMessage).HasMaxLength(1000);

        builder.Property(e => e.SessionInfo)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<BackupSessionInfo>(v, (JsonSerializerOptions?)null) ?? new BackupSessionInfo());

        builder.Property(e => e.Errors)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
            .Metadata.SetValueComparer(new ValueComparer<List<string>>(
                (c1, c2) => c1!.SequenceEqual(c2!),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c.ToList()));

        builder.HasMany(e => e.Items)
            .WithOne(m => m.Session)
            .HasForeignKey(m => m.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => e.StartTime);
        builder.HasIndex(e => e.Status);
    }
}
