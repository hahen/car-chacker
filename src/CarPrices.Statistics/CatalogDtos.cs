namespace CarPrices.Statistics;

public record CatalogFacetDto(string Name, int Count);

/// <summary>Query filter for browsing lots (bound from the query string).</summary>
public record LotFilter(
    string? Make = null,
    string? Model = null,
    string? Source = null,
    int? YearFrom = null,
    int? YearTo = null,
    string? TitleGroup = null,
    string? DamageGroup = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    int? MaxOdometer = null,
    string? Status = null,   // "upcoming" | "sold" | null = all
    bool? HasVin = null,
    string? Sort = null,     // "auction" | "price" | "year" | null = recent
    int Page = 1,
    int PageSize = 24);

public record LotListItemDto(
    long Id,
    string Source,
    string LotNumber,
    string? LotUrl,
    string? Make,
    string? Model,
    string? Trim,
    int? Year,
    string? Vin,
    bool VinFull,
    decimal? CurrentBid,
    decimal? BuyNowPrice,
    decimal? SalePrice,
    bool IsSold,
    string Status,
    string? TitleCode,
    string? DamageCode,
    int? Odometer,
    string? LocationCity,
    string? LocationState,
    DateTime? AuctionDateUtc,
    string? ThumbnailUrl,
    IReadOnlyList<string> Features);

public record LotSearchResult(int Total, int Page, int PageSize, IReadOnlyList<LotListItemDto> Items);
