namespace CarPrices.Statistics;

public record MarketOverviewDto(
    int Makes,
    int Models,
    int IndexRows,
    long TotalObservations,
    decimal? MedianPrice,
    int? MinYear,
    int? MaxYear);

public record MakeSummaryDto(
    string Make,
    int Models,
    long Observations,
    decimal? MedianPrice);

public record ModelYearPriceDto(
    int Year,
    int SampleSize,
    decimal? P25,
    decimal? P50,
    decimal? P75);

public record ModelPriceSeriesDto(
    string Make,
    string Model,
    IReadOnlyList<ModelYearPriceDto> Points);
