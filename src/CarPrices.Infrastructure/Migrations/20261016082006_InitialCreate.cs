using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarPrices.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DamageTypes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Group = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DamageTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FeatureCategories",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeatureCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Makers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    NormalizedName = table.Column<string>(type: "TEXT", nullable: false),
                    CountryCode = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Makers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PriceIndexDaily",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SegmentKey = table.Column<string>(type: "TEXT", nullable: false),
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    MedianPrice = table.Column<decimal>(type: "TEXT", nullable: true),
                    SampleSize = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceIndexDaily", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PriceStats",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DimMakerId = table.Column<long>(type: "INTEGER", nullable: true),
                    DimModelId = table.Column<long>(type: "INTEGER", nullable: true),
                    DimTrimId = table.Column<long>(type: "INTEGER", nullable: true),
                    DimModelYear = table.Column<int>(type: "INTEGER", nullable: true),
                    DimTitleGroup = table.Column<int>(type: "INTEGER", nullable: true),
                    DimDamageGroup = table.Column<int>(type: "INTEGER", nullable: true),
                    DimRegion = table.Column<string>(type: "TEXT", nullable: true),
                    PeriodType = table.Column<int>(type: "INTEGER", nullable: false),
                    PeriodKey = table.Column<string>(type: "TEXT", nullable: true),
                    SampleSize = table.Column<int>(type: "INTEGER", nullable: false),
                    MinPrice = table.Column<decimal>(type: "TEXT", nullable: true),
                    MaxPrice = table.Column<decimal>(type: "TEXT", nullable: true),
                    MeanPrice = table.Column<decimal>(type: "TEXT", nullable: true),
                    MedianPrice = table.Column<decimal>(type: "TEXT", nullable: true),
                    P25 = table.Column<decimal>(type: "TEXT", nullable: true),
                    P75 = table.Column<decimal>(type: "TEXT", nullable: true),
                    P90 = table.Column<decimal>(type: "TEXT", nullable: true),
                    StdDev = table.Column<decimal>(type: "TEXT", nullable: true),
                    MeanOdometer = table.Column<int>(type: "INTEGER", nullable: true),
                    ComputedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceStats", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RawPayloads",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Source = table.Column<int>(type: "INTEGER", nullable: false),
                    SourceLotNumber = table.Column<string>(type: "TEXT", nullable: true),
                    CapturedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RawPayloads", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScrapeRuns",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Source = table.Column<int>(type: "INTEGER", nullable: false),
                    JobType = table.Column<string>(type: "TEXT", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FinishedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    LotsSeen = table.Column<int>(type: "INTEGER", nullable: false),
                    LotsNew = table.Column<int>(type: "INTEGER", nullable: false),
                    LotsUpdated = table.Column<int>(type: "INTEGER", nullable: false),
                    PricesCaptured = table.Column<int>(type: "INTEGER", nullable: false),
                    ErrorCount = table.Column<int>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScrapeRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TitleTypes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Group = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TitleTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VinCheckIndex",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ModelYear = table.Column<int>(type: "INTEGER", nullable: false),
                    Maker = table.Column<string>(type: "TEXT", nullable: false),
                    Model = table.Column<string>(type: "TEXT", nullable: false),
                    SampleSize = table.Column<int>(type: "INTEGER", nullable: false),
                    P25 = table.Column<decimal>(type: "TEXT", nullable: true),
                    P50 = table.Column<decimal>(type: "TEXT", nullable: true),
                    P75 = table.Column<decimal>(type: "TEXT", nullable: true),
                    GeneratedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ImportedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Source = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VinCheckIndex", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VinDecodes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    VinOrPattern = table.Column<string>(type: "TEXT", nullable: false),
                    Source = table.Column<string>(type: "TEXT", nullable: false),
                    DecodedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RawJson = table.Column<string>(type: "TEXT", nullable: true),
                    MakerName = table.Column<string>(type: "TEXT", nullable: true),
                    ModelName = table.Column<string>(type: "TEXT", nullable: true),
                    TrimName = table.Column<string>(type: "TEXT", nullable: true),
                    ModelYear = table.Column<int>(type: "INTEGER", nullable: true),
                    BodyClass = table.Column<string>(type: "TEXT", nullable: true),
                    DriveType = table.Column<string>(type: "TEXT", nullable: true),
                    FuelType = table.Column<string>(type: "TEXT", nullable: true),
                    EngineModel = table.Column<string>(type: "TEXT", nullable: true),
                    PlantCountry = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VinDecodes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Features",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", nullable: false),
                    CategoryId = table.Column<long>(type: "INTEGER", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Features", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Features_FeatureCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "FeatureCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "MakerAliases",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MakerId = table.Column<long>(type: "INTEGER", nullable: false),
                    Alias = table.Column<string>(type: "TEXT", nullable: false),
                    Source = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MakerAliases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MakerAliases_Makers_MakerId",
                        column: x => x.MakerId,
                        principalTable: "Makers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Models",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MakerId = table.Column<long>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    NormalizedName = table.Column<string>(type: "TEXT", nullable: false),
                    DefaultBodyStyle = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Models", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Models_Makers_MakerId",
                        column: x => x.MakerId,
                        principalTable: "Makers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IngestErrors",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ScrapeRunId = table.Column<long>(type: "INTEGER", nullable: true),
                    Stage = table.Column<string>(type: "TEXT", nullable: false),
                    SourceLotNumber = table.Column<string>(type: "TEXT", nullable: true),
                    Message = table.Column<string>(type: "TEXT", nullable: false),
                    Payload = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IngestErrors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IngestErrors_ScrapeRuns_ScrapeRunId",
                        column: x => x.ScrapeRunId,
                        principalTable: "ScrapeRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FeatureMappings",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MakerId = table.Column<long>(type: "INTEGER", nullable: true),
                    MatchType = table.Column<int>(type: "INTEGER", nullable: false),
                    Pattern = table.Column<string>(type: "TEXT", nullable: false),
                    FeatureId = table.Column<long>(type: "INTEGER", nullable: false),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeatureMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeatureMappings_Features_FeatureId",
                        column: x => x.FeatureId,
                        principalTable: "Features",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FeatureMappings_Makers_MakerId",
                        column: x => x.MakerId,
                        principalTable: "Makers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FeaturePriceImpacts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FeatureId = table.Column<long>(type: "INTEGER", nullable: false),
                    SegmentKey = table.Column<string>(type: "TEXT", nullable: false),
                    WithN = table.Column<int>(type: "INTEGER", nullable: false),
                    WithoutN = table.Column<int>(type: "INTEGER", nullable: false),
                    MeanWith = table.Column<decimal>(type: "TEXT", nullable: true),
                    MeanWithout = table.Column<decimal>(type: "TEXT", nullable: true),
                    DeltaAbs = table.Column<decimal>(type: "TEXT", nullable: true),
                    DeltaPct = table.Column<double>(type: "REAL", nullable: true),
                    ComputedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeaturePriceImpacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeaturePriceImpacts_Features_FeatureId",
                        column: x => x.FeatureId,
                        principalTable: "Features",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CollectionTargets",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MakeText = table.Column<string>(type: "TEXT", nullable: false),
                    MakerId = table.Column<long>(type: "INTEGER", nullable: true),
                    ModelText = table.Column<string>(type: "TEXT", nullable: true),
                    ModelId = table.Column<long>(type: "INTEGER", nullable: true),
                    TrimText = table.Column<string>(type: "TEXT", nullable: true),
                    YearFrom = table.Column<int>(type: "INTEGER", nullable: true),
                    YearTo = table.Column<int>(type: "INTEGER", nullable: true),
                    YearField = table.Column<int>(type: "INTEGER", nullable: false),
                    TitleGroups = table.Column<string>(type: "TEXT", nullable: true),
                    Regions = table.Column<string>(type: "TEXT", nullable: true),
                    Sources = table.Column<string>(type: "TEXT", nullable: true),
                    Enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollectionTargets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CollectionTargets_Makers_MakerId",
                        column: x => x.MakerId,
                        principalTable: "Makers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CollectionTargets_Models_ModelId",
                        column: x => x.ModelId,
                        principalTable: "Models",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ModelAliases",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ModelId = table.Column<long>(type: "INTEGER", nullable: false),
                    Alias = table.Column<string>(type: "TEXT", nullable: false),
                    Source = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModelAliases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModelAliases_Models_ModelId",
                        column: x => x.ModelId,
                        principalTable: "Models",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Trims",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ModelId = table.Column<long>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    NormalizedName = table.Column<string>(type: "TEXT", nullable: false),
                    EngineCode = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Trims_Models_ModelId",
                        column: x => x.ModelId,
                        principalTable: "Models",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Cars",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Vin = table.Column<string>(type: "TEXT", nullable: true),
                    VinPattern = table.Column<string>(type: "TEXT", nullable: true),
                    VinSource = table.Column<int>(type: "INTEGER", nullable: false),
                    VinConfidence = table.Column<double>(type: "REAL", nullable: true),
                    MakerId = table.Column<long>(type: "INTEGER", nullable: true),
                    ModelId = table.Column<long>(type: "INTEGER", nullable: true),
                    TrimId = table.Column<long>(type: "INTEGER", nullable: true),
                    ModelYear = table.Column<int>(type: "INTEGER", nullable: true),
                    ManufactureYear = table.Column<int>(type: "INTEGER", nullable: true),
                    BodyStyle = table.Column<string>(type: "TEXT", nullable: true),
                    DriveType = table.Column<string>(type: "TEXT", nullable: true),
                    FuelType = table.Column<string>(type: "TEXT", nullable: true),
                    Transmission = table.Column<string>(type: "TEXT", nullable: true),
                    EngineDisplacement = table.Column<string>(type: "TEXT", nullable: true),
                    EngineCylinders = table.Column<int>(type: "INTEGER", nullable: true),
                    EngineDescription = table.Column<string>(type: "TEXT", nullable: true),
                    ExteriorColor = table.Column<string>(type: "TEXT", nullable: true),
                    VinDecoded = table.Column<bool>(type: "INTEGER", nullable: false),
                    VinDecodedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cars", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cars_Makers_MakerId",
                        column: x => x.MakerId,
                        principalTable: "Makers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Cars_Models_ModelId",
                        column: x => x.ModelId,
                        principalTable: "Models",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Cars_Trims_TrimId",
                        column: x => x.TrimId,
                        principalTable: "Trims",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TrimAliases",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TrimId = table.Column<long>(type: "INTEGER", nullable: false),
                    Alias = table.Column<string>(type: "TEXT", nullable: false),
                    Source = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrimAliases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrimAliases_Trims_TrimId",
                        column: x => x.TrimId,
                        principalTable: "Trims",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CarFeatures",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CarId = table.Column<long>(type: "INTEGER", nullable: false),
                    FeatureId = table.Column<long>(type: "INTEGER", nullable: false),
                    Source = table.Column<int>(type: "INTEGER", nullable: false),
                    Confidence = table.Column<double>(type: "REAL", nullable: false),
                    FirstSeenAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarFeatures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CarFeatures_Cars_CarId",
                        column: x => x.CarId,
                        principalTable: "Cars",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CarFeatures_Features_FeatureId",
                        column: x => x.FeatureId,
                        principalTable: "Features",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Lots",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CarId = table.Column<long>(type: "INTEGER", nullable: false),
                    Source = table.Column<int>(type: "INTEGER", nullable: false),
                    SourceLotNumber = table.Column<string>(type: "TEXT", nullable: false),
                    LotUrl = table.Column<string>(type: "TEXT", nullable: true),
                    AuctionDateUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SaleStatusRaw = table.Column<string>(type: "TEXT", nullable: true),
                    SaleStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    YardCode = table.Column<string>(type: "TEXT", nullable: true),
                    LocationCity = table.Column<string>(type: "TEXT", nullable: true),
                    LocationState = table.Column<string>(type: "TEXT", nullable: true),
                    LocationCountry = table.Column<string>(type: "TEXT", nullable: true),
                    TitleTypeId = table.Column<long>(type: "INTEGER", nullable: true),
                    TitleState = table.Column<string>(type: "TEXT", nullable: true),
                    PrimaryDamageId = table.Column<long>(type: "INTEGER", nullable: true),
                    SecondaryDamageId = table.Column<long>(type: "INTEGER", nullable: true),
                    Odometer = table.Column<int>(type: "INTEGER", nullable: true),
                    OdometerUnit = table.Column<string>(type: "TEXT", nullable: true),
                    OdometerBrand = table.Column<int>(type: "INTEGER", nullable: false),
                    HasKeys = table.Column<bool>(type: "INTEGER", nullable: true),
                    RunAndDrive = table.Column<int>(type: "INTEGER", nullable: false),
                    EstimatedRetailValue = table.Column<decimal>(type: "TEXT", nullable: true),
                    RepairCostEstimate = table.Column<decimal>(type: "TEXT", nullable: true),
                    CurrentBid = table.Column<decimal>(type: "TEXT", nullable: true),
                    BuyNowPrice = table.Column<decimal>(type: "TEXT", nullable: true),
                    FinalBid = table.Column<decimal>(type: "TEXT", nullable: true),
                    SalePrice = table.Column<decimal>(type: "TEXT", nullable: true),
                    Currency = table.Column<string>(type: "TEXT", nullable: true),
                    IsSold = table.Column<bool>(type: "INTEGER", nullable: false),
                    SaleCapturedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FirstSeenUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastSeenUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RawPayloadId = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Lots_Cars_CarId",
                        column: x => x.CarId,
                        principalTable: "Cars",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Lots_DamageTypes_PrimaryDamageId",
                        column: x => x.PrimaryDamageId,
                        principalTable: "DamageTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Lots_DamageTypes_SecondaryDamageId",
                        column: x => x.SecondaryDamageId,
                        principalTable: "DamageTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Lots_RawPayloads_RawPayloadId",
                        column: x => x.RawPayloadId,
                        principalTable: "RawPayloads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Lots_TitleTypes_TitleTypeId",
                        column: x => x.TitleTypeId,
                        principalTable: "TitleTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RawOptions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CarId = table.Column<long>(type: "INTEGER", nullable: true),
                    VinPattern = table.Column<string>(type: "TEXT", nullable: true),
                    MakerId = table.Column<long>(type: "INTEGER", nullable: true),
                    RawCode = table.Column<string>(type: "TEXT", nullable: true),
                    RawText = table.Column<string>(type: "TEXT", nullable: false),
                    Source = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RawOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RawOptions_Cars_CarId",
                        column: x => x.CarId,
                        principalTable: "Cars",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RawOptions_Makers_MakerId",
                        column: x => x.MakerId,
                        principalTable: "Makers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BidHistory",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LotId = table.Column<long>(type: "INTEGER", nullable: false),
                    BidAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    BidTimeUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Source = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BidHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BidHistory_Lots_LotId",
                        column: x => x.LotId,
                        principalTable: "Lots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LotImages",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LotId = table.Column<long>(type: "INTEGER", nullable: false),
                    Url = table.Column<string>(type: "TEXT", nullable: false),
                    Kind = table.Column<int>(type: "INTEGER", nullable: false),
                    Sequence = table.Column<int>(type: "INTEGER", nullable: false),
                    LocalPath = table.Column<string>(type: "TEXT", nullable: true),
                    Width = table.Column<int>(type: "INTEGER", nullable: true),
                    Height = table.Column<int>(type: "INTEGER", nullable: true),
                    OcrProcessed = table.Column<bool>(type: "INTEGER", nullable: false),
                    CapturedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LotImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LotImages_Lots_LotId",
                        column: x => x.LotId,
                        principalTable: "Lots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VinExtractions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LotId = table.Column<long>(type: "INTEGER", nullable: false),
                    LotImageId = table.Column<long>(type: "INTEGER", nullable: true),
                    Method = table.Column<int>(type: "INTEGER", nullable: false),
                    Engine = table.Column<string>(type: "TEXT", nullable: true),
                    RawText = table.Column<string>(type: "TEXT", nullable: true),
                    CandidateVin = table.Column<string>(type: "TEXT", nullable: true),
                    CheckDigitValid = table.Column<bool>(type: "INTEGER", nullable: false),
                    MatchesMaskedPattern = table.Column<bool>(type: "INTEGER", nullable: false),
                    Confidence = table.Column<double>(type: "REAL", nullable: false),
                    Accepted = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VinExtractions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VinExtractions_LotImages_LotImageId",
                        column: x => x.LotImageId,
                        principalTable: "LotImages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_VinExtractions_Lots_LotId",
                        column: x => x.LotId,
                        principalTable: "Lots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BidHistory_LotId",
                table: "BidHistory",
                column: "LotId");

            migrationBuilder.CreateIndex(
                name: "IX_CarFeatures_CarId_FeatureId_Source",
                table: "CarFeatures",
                columns: new[] { "CarId", "FeatureId", "Source" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CarFeatures_FeatureId",
                table: "CarFeatures",
                column: "FeatureId");

            migrationBuilder.CreateIndex(
                name: "IX_Cars_MakerId_ModelId_TrimId_ModelYear",
                table: "Cars",
                columns: new[] { "MakerId", "ModelId", "TrimId", "ModelYear" });

            migrationBuilder.CreateIndex(
                name: "IX_Cars_ModelId",
                table: "Cars",
                column: "ModelId");

            migrationBuilder.CreateIndex(
                name: "IX_Cars_TrimId",
                table: "Cars",
                column: "TrimId");

            migrationBuilder.CreateIndex(
                name: "IX_Cars_Vin",
                table: "Cars",
                column: "Vin",
                unique: true,
                filter: "\"Vin\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Cars_VinPattern",
                table: "Cars",
                column: "VinPattern");

            migrationBuilder.CreateIndex(
                name: "IX_CollectionTargets_MakerId",
                table: "CollectionTargets",
                column: "MakerId");

            migrationBuilder.CreateIndex(
                name: "IX_CollectionTargets_ModelId",
                table: "CollectionTargets",
                column: "ModelId");

            migrationBuilder.CreateIndex(
                name: "IX_DamageTypes_Code",
                table: "DamageTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FeatureMappings_FeatureId",
                table: "FeatureMappings",
                column: "FeatureId");

            migrationBuilder.CreateIndex(
                name: "IX_FeatureMappings_MakerId",
                table: "FeatureMappings",
                column: "MakerId");

            migrationBuilder.CreateIndex(
                name: "IX_FeaturePriceImpacts_FeatureId_SegmentKey",
                table: "FeaturePriceImpacts",
                columns: new[] { "FeatureId", "SegmentKey" });

            migrationBuilder.CreateIndex(
                name: "IX_Features_CategoryId",
                table: "Features",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Features_Code",
                table: "Features",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IngestErrors_ScrapeRunId",
                table: "IngestErrors",
                column: "ScrapeRunId");

            migrationBuilder.CreateIndex(
                name: "IX_LotImages_LotId",
                table: "LotImages",
                column: "LotId");

            migrationBuilder.CreateIndex(
                name: "IX_Lots_AuctionDateUtc",
                table: "Lots",
                column: "AuctionDateUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Lots_CarId",
                table: "Lots",
                column: "CarId");

            migrationBuilder.CreateIndex(
                name: "IX_Lots_IsSold_SaleStatus",
                table: "Lots",
                columns: new[] { "IsSold", "SaleStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_Lots_PrimaryDamageId",
                table: "Lots",
                column: "PrimaryDamageId");

            migrationBuilder.CreateIndex(
                name: "IX_Lots_RawPayloadId",
                table: "Lots",
                column: "RawPayloadId");

            migrationBuilder.CreateIndex(
                name: "IX_Lots_SecondaryDamageId",
                table: "Lots",
                column: "SecondaryDamageId");

            migrationBuilder.CreateIndex(
                name: "IX_Lots_Source_SourceLotNumber_AuctionDateUtc",
                table: "Lots",
                columns: new[] { "Source", "SourceLotNumber", "AuctionDateUtc" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lots_TitleTypeId",
                table: "Lots",
                column: "TitleTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_MakerAliases_Alias",
                table: "MakerAliases",
                column: "Alias",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MakerAliases_MakerId",
                table: "MakerAliases",
                column: "MakerId");

            migrationBuilder.CreateIndex(
                name: "IX_Makers_NormalizedName",
                table: "Makers",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModelAliases_ModelId",
                table: "ModelAliases",
                column: "ModelId");

            migrationBuilder.CreateIndex(
                name: "IX_Models_MakerId_NormalizedName",
                table: "Models",
                columns: new[] { "MakerId", "NormalizedName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PriceIndexDaily_SegmentKey_Date",
                table: "PriceIndexDaily",
                columns: new[] { "SegmentKey", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PriceStats_DimMakerId_DimModelId_DimModelYear_PeriodType",
                table: "PriceStats",
                columns: new[] { "DimMakerId", "DimModelId", "DimModelYear", "PeriodType" });

            migrationBuilder.CreateIndex(
                name: "IX_RawOptions_CarId",
                table: "RawOptions",
                column: "CarId");

            migrationBuilder.CreateIndex(
                name: "IX_RawOptions_MakerId",
                table: "RawOptions",
                column: "MakerId");

            migrationBuilder.CreateIndex(
                name: "IX_TitleTypes_Code",
                table: "TitleTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrimAliases_TrimId",
                table: "TrimAliases",
                column: "TrimId");

            migrationBuilder.CreateIndex(
                name: "IX_Trims_ModelId_NormalizedName",
                table: "Trims",
                columns: new[] { "ModelId", "NormalizedName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VinCheckIndex_ModelYear_Maker_Model",
                table: "VinCheckIndex",
                columns: new[] { "ModelYear", "Maker", "Model" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VinDecodes_VinOrPattern",
                table: "VinDecodes",
                column: "VinOrPattern",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VinExtractions_LotId",
                table: "VinExtractions",
                column: "LotId");

            migrationBuilder.CreateIndex(
                name: "IX_VinExtractions_LotImageId",
                table: "VinExtractions",
                column: "LotImageId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BidHistory");

            migrationBuilder.DropTable(
                name: "CarFeatures");

            migrationBuilder.DropTable(
                name: "CollectionTargets");

            migrationBuilder.DropTable(
                name: "FeatureMappings");

            migrationBuilder.DropTable(
                name: "FeaturePriceImpacts");

            migrationBuilder.DropTable(
                name: "IngestErrors");

            migrationBuilder.DropTable(
                name: "MakerAliases");

            migrationBuilder.DropTable(
                name: "ModelAliases");

            migrationBuilder.DropTable(
                name: "PriceIndexDaily");

            migrationBuilder.DropTable(
                name: "PriceStats");

            migrationBuilder.DropTable(
                name: "RawOptions");

            migrationBuilder.DropTable(
                name: "TrimAliases");

            migrationBuilder.DropTable(
                name: "VinCheckIndex");

            migrationBuilder.DropTable(
                name: "VinDecodes");

            migrationBuilder.DropTable(
                name: "VinExtractions");

            migrationBuilder.DropTable(
                name: "Features");

            migrationBuilder.DropTable(
                name: "ScrapeRuns");

            migrationBuilder.DropTable(
                name: "LotImages");

            migrationBuilder.DropTable(
                name: "FeatureCategories");

            migrationBuilder.DropTable(
                name: "Lots");

            migrationBuilder.DropTable(
                name: "Cars");

            migrationBuilder.DropTable(
                name: "DamageTypes");

            migrationBuilder.DropTable(
                name: "RawPayloads");

            migrationBuilder.DropTable(
                name: "TitleTypes");

            migrationBuilder.DropTable(
                name: "Trims");

            migrationBuilder.DropTable(
                name: "Models");

            migrationBuilder.DropTable(
                name: "Makers");
        }
    }
}
