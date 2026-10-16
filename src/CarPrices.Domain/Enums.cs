namespace CarPrices.Domain.Enums;

/// <summary>Where a lot / data point came from.</summary>
public enum Source
{
    Copart = 1,
    Iaai = 2,
    StatVin = 3,
    BidFax = 4,
    VinCheck = 5,
    Manual = 6,
    Other = 99,
}

/// <summary>Normalized sale status for a lot (auction appearance).</summary>
public enum SaleStatus
{
    Unknown = 0,
    Upcoming = 1,
    Live = 2,
    Sold = 3,
    NotSold = 4,
    OnApproval = 5,
    Cancelled = 6,
}

public enum RunAndDrive
{
    Unknown = 0,
    RunAndDrive = 1,
    StartsAndMoves = 2,
    EngineStartProgram = 3,
    Stationary = 4,
    Enhanced = 5,
}

public enum OdometerBrand
{
    Unknown = 0,
    Actual = 1,
    NotActual = 2,
    Exempt = 3,
    ExceedsMechanicalLimit = 4,
}

/// <summary>How a feature was determined for a car.</summary>
public enum FeatureSource
{
    VinDecode = 1,
    BuildData = 2,
    ListingText = 3,
    Photo = 4,
    Manual = 5,
}

/// <summary>How the full VIN on a car was obtained.</summary>
public enum VinSource
{
    Unknown = 0,
    TextField = 1,
    PhotoBarcode = 2,
    PhotoOcr = 3,
    Manual = 4,
}

public enum PeriodType
{
    All = 0,
    Year = 1,
    Quarter = 2,
    Month = 3,
    Rolling90 = 4,
}

public enum TitleGroup
{
    Unknown = 0,
    Clean = 1,
    Salvage = 2,
    Rebuilt = 3,
    Junk = 4,
    Nonrepairable = 5,
    Other = 99,
}

public enum DamageGroup
{
    Unknown = 0,
    Front = 1,
    Rear = 2,
    Side = 3,
    AllOver = 4,
    Water = 5,
    Fire = 6,
    Hail = 7,
    Vandalism = 8,
    Mechanical = 9,
    Normal = 10,
    Undercarriage = 11,
    Other = 99,
}

public enum FeatureMatchType
{
    Exact = 1,
    Contains = 2,
    Regex = 3,
}

public enum VinExtractionMethod
{
    Barcode = 1,
    Ocr = 2,
}

public enum LotImageKind
{
    Unknown = 0,
    Label = 1,
    Jamb = 2,
    VinPlate = 3,
    Dash = 4,
    Exterior = 5,
    Interior = 6,
}

/// <summary>Which year a collection-target range filters on.</summary>
public enum YearField
{
    Model = 0,
    Manufacture = 1,
}
