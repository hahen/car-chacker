using CarPrices.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CarPrices.Infrastructure;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    // Taxonomy
    public DbSet<Maker> Makers => Set<Maker>();
    public DbSet<MakerAlias> MakerAliases => Set<MakerAlias>();
    public DbSet<Model> Models => Set<Model>();
    public DbSet<ModelAlias> ModelAliases => Set<ModelAlias>();
    public DbSet<Trim> Trims => Set<Trim>();
    public DbSet<TrimAlias> TrimAliases => Set<TrimAlias>();

    // Vehicles & auctions
    public DbSet<Car> Cars => Set<Car>();
    public DbSet<Lot> Lots => Set<Lot>();
    public DbSet<RawPayload> RawPayloads => Set<RawPayload>();
    public DbSet<LotImage> LotImages => Set<LotImage>();
    public DbSet<BidHistory> BidHistory => Set<BidHistory>();

    // Features
    public DbSet<FeatureCategory> FeatureCategories => Set<FeatureCategory>();
    public DbSet<Feature> Features => Set<Feature>();
    public DbSet<CarFeature> CarFeatures => Set<CarFeature>();
    public DbSet<RawOption> RawOptions => Set<RawOption>();
    public DbSet<FeatureMapping> FeatureMappings => Set<FeatureMapping>();

    // VIN
    public DbSet<VinDecode> VinDecodes => Set<VinDecode>();
    public DbSet<VinExtraction> VinExtractions => Set<VinExtraction>();

    // Statistics
    public DbSet<PriceStat> PriceStats => Set<PriceStat>();
    public DbSet<FeaturePriceImpact> FeaturePriceImpacts => Set<FeaturePriceImpact>();
    public DbSet<PriceIndexDaily> PriceIndexDaily => Set<PriceIndexDaily>();
    public DbSet<VinCheckIndex> VinCheckIndex => Set<VinCheckIndex>();

    // Lookups & ops
    public DbSet<TitleType> TitleTypes => Set<TitleType>();
    public DbSet<DamageType> DamageTypes => Set<DamageType>();
    public DbSet<ScrapeRun> ScrapeRuns => Set<ScrapeRun>();
    public DbSet<IngestError> IngestErrors => Set<IngestError>();
    public DbSet<CollectionTarget> CollectionTargets => Set<CollectionTarget>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // --- Taxonomy ---
        b.Entity<Maker>().HasIndex(x => x.NormalizedName).IsUnique();
        b.Entity<MakerAlias>().HasIndex(x => x.Alias).IsUnique();
        b.Entity<MakerAlias>()
            .HasOne(x => x.Maker).WithMany(m => m.Aliases)
            .HasForeignKey(x => x.MakerId).OnDelete(DeleteBehavior.Cascade);

        b.Entity<Model>().HasIndex(x => new { x.MakerId, x.NormalizedName }).IsUnique();
        b.Entity<Model>()
            .HasOne(x => x.Maker).WithMany(m => m.Models)
            .HasForeignKey(x => x.MakerId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<ModelAlias>()
            .HasOne(x => x.Model).WithMany(m => m.Aliases)
            .HasForeignKey(x => x.ModelId).OnDelete(DeleteBehavior.Cascade);

        b.Entity<Trim>().HasIndex(x => new { x.ModelId, x.NormalizedName }).IsUnique();
        b.Entity<Trim>()
            .HasOne(x => x.Model).WithMany(m => m.Trims)
            .HasForeignKey(x => x.ModelId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<TrimAlias>()
            .HasOne(x => x.Trim).WithMany(t => t.Aliases)
            .HasForeignKey(x => x.TrimId).OnDelete(DeleteBehavior.Cascade);

        // --- Cars & lots ---
        b.Entity<Car>().HasIndex(x => x.VinPattern);
        b.Entity<Car>().HasIndex(x => x.Vin).IsUnique().HasFilter("\"Vin\" IS NOT NULL");
        b.Entity<Car>().HasIndex(x => new { x.MakerId, x.ModelId, x.TrimId, x.ModelYear });
        b.Entity<Car>().HasOne(x => x.Maker).WithMany().HasForeignKey(x => x.MakerId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Car>().HasOne(x => x.Model).WithMany().HasForeignKey(x => x.ModelId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Car>().HasOne(x => x.Trim).WithMany().HasForeignKey(x => x.TrimId).OnDelete(DeleteBehavior.Restrict);

        b.Entity<Lot>().HasIndex(x => new { x.Source, x.SourceLotNumber, x.AuctionDateUtc }).IsUnique();
        b.Entity<Lot>().HasIndex(x => x.CarId);
        b.Entity<Lot>().HasIndex(x => x.AuctionDateUtc);
        b.Entity<Lot>().HasIndex(x => new { x.IsSold, x.SaleStatus });
        b.Entity<Lot>().HasOne(x => x.Car).WithMany(c => c.Lots).HasForeignKey(x => x.CarId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<Lot>().HasOne(x => x.TitleType).WithMany().HasForeignKey(x => x.TitleTypeId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Lot>().HasOne(x => x.PrimaryDamage).WithMany().HasForeignKey(x => x.PrimaryDamageId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Lot>().HasOne(x => x.SecondaryDamage).WithMany().HasForeignKey(x => x.SecondaryDamageId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Lot>().HasOne(x => x.RawPayload).WithMany().HasForeignKey(x => x.RawPayloadId).OnDelete(DeleteBehavior.SetNull);

        b.Entity<LotImage>().HasOne(x => x.Lot).WithMany(l => l.Images).HasForeignKey(x => x.LotId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<BidHistory>().HasOne(x => x.Lot).WithMany(l => l.Bids).HasForeignKey(x => x.LotId).OnDelete(DeleteBehavior.Cascade);

        // --- Features ---
        b.Entity<Feature>().HasIndex(x => x.Code).IsUnique();
        b.Entity<Feature>().HasOne(x => x.Category).WithMany(c => c.Features).HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.SetNull);
        b.Entity<CarFeature>().HasIndex(x => new { x.CarId, x.FeatureId, x.Source }).IsUnique();
        b.Entity<CarFeature>().HasOne(x => x.Car).WithMany(c => c.Features).HasForeignKey(x => x.CarId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<CarFeature>().HasOne(x => x.Feature).WithMany(f => f.CarFeatures).HasForeignKey(x => x.FeatureId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<FeatureMapping>().HasOne(x => x.Feature).WithMany(f => f.Mappings).HasForeignKey(x => x.FeatureId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<FeatureMapping>().HasOne(x => x.Maker).WithMany().HasForeignKey(x => x.MakerId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<RawOption>().HasOne(x => x.Car).WithMany().HasForeignKey(x => x.CarId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<RawOption>().HasOne(x => x.Maker).WithMany().HasForeignKey(x => x.MakerId).OnDelete(DeleteBehavior.Restrict);

        // --- VIN ---
        b.Entity<VinDecode>().HasIndex(x => x.VinOrPattern).IsUnique();
        b.Entity<VinExtraction>().HasOne(x => x.Lot).WithMany().HasForeignKey(x => x.LotId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<VinExtraction>().HasOne(x => x.LotImage).WithMany().HasForeignKey(x => x.LotImageId).OnDelete(DeleteBehavior.SetNull);

        // --- Statistics ---
        b.Entity<PriceStat>().HasIndex(x => new { x.DimMakerId, x.DimModelId, x.DimModelYear, x.PeriodType });
        b.Entity<FeaturePriceImpact>().HasIndex(x => new { x.FeatureId, x.SegmentKey });
        b.Entity<FeaturePriceImpact>().HasOne(x => x.Feature).WithMany().HasForeignKey(x => x.FeatureId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<PriceIndexDaily>().HasIndex(x => new { x.SegmentKey, x.Date }).IsUnique();
        b.Entity<VinCheckIndex>().HasIndex(x => new { x.ModelYear, x.Maker, x.Model }).IsUnique();

        // --- Lookups & ops ---
        b.Entity<TitleType>().HasIndex(x => x.Code).IsUnique();
        b.Entity<DamageType>().HasIndex(x => x.Code).IsUnique();
        b.Entity<IngestError>().HasOne(x => x.ScrapeRun).WithMany(r => r.Errors).HasForeignKey(x => x.ScrapeRunId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<CollectionTarget>().HasOne(x => x.Maker).WithMany().HasForeignKey(x => x.MakerId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<CollectionTarget>().HasOne(x => x.Model).WithMany().HasForeignKey(x => x.ModelId).OnDelete(DeleteBehavior.Restrict);
    }
}
