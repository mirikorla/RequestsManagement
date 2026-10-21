using Microsoft.EntityFrameworkCore;
using RequestsManagement.Core.Models;

namespace RequestsManagement.Infrastructure.DAL;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<ServiceRequest> Requests => Set<ServiceRequest>();
    public DbSet<RequestStatusHistory> StatusHistory => Set<RequestStatusHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ServiceRequest>(e =>
        {
            e.ToTable("Requests");
            e.HasKey(r => r.Id);
            e.Property(r => r.Title).HasMaxLength(200).IsRequired();
            e.Property(r => r.OrganizationName).HasMaxLength(200).IsRequired();
            e.Property(r => r.AssignedTo).HasMaxLength(100);
            e.Property(r => r.Status).HasConversion<byte>();
            e.Property(r => r.Priority).HasConversion<byte>();
            e.Property(r => r.CreatedAt).HasColumnType("datetime2(3)");
            e.Property(r => r.UpdatedAt).HasColumnType("datetime2(3)");
            e.Property(r => r.RowVersion).IsRowVersion();

            e.HasMany(r => r.History).WithOne().HasForeignKey(h => h.RequestId).OnDelete(DeleteBehavior.Cascade);

            // Indexes follow the main list scenarios. The default view is "newest first", so CreatedAt is the
            // trailing column: filter by the leading column, then read rows already in sort order (no SORT operator).
            // See docs/PERFORMANCE.md for plans and measurements.
            e.HasIndex(r => r.CreatedAt).HasDatabaseName("IX_Requests_CreatedAt");
            e.HasIndex(r => new { r.Status, r.CreatedAt }).HasDatabaseName("IX_Requests_Status_CreatedAt");
            e.HasIndex(r => new { r.Priority, r.CreatedAt }).HasDatabaseName("IX_Requests_Priority_CreatedAt");
            e.HasIndex(r => new { r.AssignedTo, r.CreatedAt }).HasDatabaseName("IX_Requests_AssignedTo_CreatedAt");
            e.HasIndex(r => r.OrganizationName).HasDatabaseName("IX_Requests_OrganizationName");
        });

        modelBuilder.Entity<RequestStatusHistory>(e =>
        {
            e.ToTable("RequestStatusHistory");
            e.HasKey(h => h.Id);
            e.Property(h => h.PreviousStatus).HasConversion<byte>();
            e.Property(h => h.NewStatus).HasConversion<byte>();
            e.Property(h => h.ChangedBy).HasMaxLength(100).IsRequired();
            e.Property(h => h.ChangedAt).HasColumnType("datetime2(3)");
            e.HasIndex(h => new { h.RequestId, h.ChangedAt }).HasDatabaseName("IX_RequestStatusHistory_RequestId_ChangedAt");
        });
    }
}
