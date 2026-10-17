using CarPrices.Domain.Enums;

namespace CarPrices.Statistics;

public record SoldOverviewDto(
    int SoldLots,
    decimal? MedianSoldPrice,
    int? MinYear,
    int? MaxYear,
    int PriceStatRows,
    int FeatureImpactRows,
    DateTime? LastComputedUtc);

public record PriceStatDto(
    long? MakerId,
    long? ModelId,
    int? ModelYear,
    int SampleSize,
    decimal? Median,
    decimal? P25,
    decimal? P75,
    decimal? Mean,
    decimal? Min,
    decimal? Max,
    int? MeanOdometer);

public record FeatureImpactDto(
    long FeatureId,
    string FeatureCode,
    string SegmentKey,
    int WithN,
    int WithoutN,
    decimal? MeanWith,
    decimal? MeanWithout,
    decimal? DeltaAbs,
    double? DeltaPct);

public record EstimateQuery(
    long? MakerId,
    long? ModelId,
    long? TrimId,
    int? Year,
    int YearTolerance = 1,
    int? Mileage = null,
    TitleGroup? TitleGroup = null,
    DamageGroup? DamageGroup = null);

public record EstimateResult(
    int Comparables,
    decimal? Median,
    decimal? P25,
    decimal? P75,
    decimal? Mean,
    decimal? Suggested,
    decimal? MileageAdjusted,
    string Basis);
