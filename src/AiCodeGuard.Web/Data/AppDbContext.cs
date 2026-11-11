using Microsoft.EntityFrameworkCore;

namespace AiCodeGuard.Web.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<AnalysisRecord> Analyses => Set<AnalysisRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AnalysisRecord>(entity =>
        {
            entity.ToTable("Analyses");
            entity.HasKey(a => a.Id);
            entity.Property(a => a.FileName).HasMaxLength(260).IsRequired();
            entity.Property(a => a.Verdict).HasMaxLength(20).IsRequired();
            entity.HasIndex(a => a.CreatedAt);
        });
    }
}

/// <summary>Trace d'une analyse : sert d'historique et de preuve de contrôle (traçabilité).</summary>
public class AnalysisRecord
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string SourceCode { get; set; } = string.Empty;
    public int Score { get; set; }
    public string Verdict { get; set; } = string.Empty;
    public int FindingsCount { get; set; }
    public int CriticalCount { get; set; }
    public string FindingsJson { get; set; } = "[]";
    public string? LlmReview { get; set; }
    public DateTime CreatedAt { get; set; }
}
