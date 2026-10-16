using CarPrices.Domain.Entities;
using CarPrices.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CarPrices.Infrastructure;

/// <summary>Applies migrations, enables WAL, and seeds reference/lookup data (idempotent).</summary>
public static class DbInitializer
{
    public static void Initialize(AppDbContext db)
    {
        db.Database.Migrate();
        db.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
        db.Database.ExecuteSqlRaw("PRAGMA busy_timeout=5000;");
        Seed(db);
    }

    public static void Seed(AppDbContext db)
    {
        SeedTitleTypes(db);
        SeedDamageTypes(db);
        SeedFeatures(db);
        SeedMakers(db);
        db.SaveChanges();
        SeedFeatureMappings(db); // needs feature ids
        db.SaveChanges();
    }

    private static void SeedTitleTypes(AppDbContext db)
    {
        if (db.TitleTypes.Any()) return;
        db.TitleTypes.AddRange(
            new TitleType { Code = "CLEAN", Description = "Clean Title", Group = TitleGroup.Clean },
            new TitleType { Code = "SALVAGE", Description = "Salvage Certificate", Group = TitleGroup.Salvage },
            new TitleType { Code = "REBUILT", Description = "Rebuilt / Reconstructed", Group = TitleGroup.Rebuilt },
            new TitleType { Code = "JUNK", Description = "Junk / Certificate of Destruction", Group = TitleGroup.Junk },
            new TitleType { Code = "NONREPAIRABLE", Description = "Nonrepairable", Group = TitleGroup.Nonrepairable },
            new TitleType { Code = "FLOOD", Description = "Flood / Water Damage", Group = TitleGroup.Salvage },
            new TitleType { Code = "BILL_OF_SALE", Description = "Bill of Sale", Group = TitleGroup.Other },
            new TitleType { Code = "PARTS_ONLY", Description = "Parts Only", Group = TitleGroup.Nonrepairable });
    }

    private static void SeedDamageTypes(AppDbContext db)
    {
        if (db.DamageTypes.Any()) return;
        db.DamageTypes.AddRange(
            new DamageType { Code = "FRONT", Description = "Front End", Group = DamageGroup.Front },
            new DamageType { Code = "REAR", Description = "Rear End", Group = DamageGroup.Rear },
            new DamageType { Code = "SIDE", Description = "Side", Group = DamageGroup.Side },
            new DamageType { Code = "ALL_OVER", Description = "All Over", Group = DamageGroup.AllOver },
            new DamageType { Code = "WATER", Description = "Water / Flood", Group = DamageGroup.Water },
            new DamageType { Code = "FIRE", Description = "Burn / Fire", Group = DamageGroup.Fire },
            new DamageType { Code = "HAIL", Description = "Hail", Group = DamageGroup.Hail },
            new DamageType { Code = "VANDALISM", Description = "Vandalism", Group = DamageGroup.Vandalism },
            new DamageType { Code = "MECHANICAL", Description = "Mechanical", Group = DamageGroup.Mechanical },
            new DamageType { Code = "NORMAL_WEAR", Description = "Normal Wear", Group = DamageGroup.Normal },
            new DamageType { Code = "UNDERCARRIAGE", Description = "Undercarriage", Group = DamageGroup.Undercarriage },
            new DamageType { Code = "ROLLOVER", Description = "Rollover", Group = DamageGroup.AllOver },
            new DamageType { Code = "MINOR_DENT", Description = "Minor Dents / Scratches", Group = DamageGroup.Other },
            new DamageType { Code = "UNKNOWN", Description = "Unknown", Group = DamageGroup.Unknown });
    }

    private static void SeedFeatures(AppDbContext db)
    {
        if (db.Features.Any()) return;

        var cats = new Dictionary<string, FeatureCategory>();
        foreach (var name in new[] { "Safety", "Comfort", "Infotainment", "Drivetrain", "Exterior", "Convenience" })
            cats[name] = new FeatureCategory { Name = name };

        void F(string code, string display, string cat) =>
            db.Features.Add(new Feature { Code = code, DisplayName = display, Category = cats[cat] });

        F("ADAPTIVE_CRUISE", "Adaptive Cruise Control", "Safety");
        F("BLIND_SPOT", "Blind Spot Monitor", "Safety");
        F("LANE_KEEP", "Lane Keep Assist", "Safety");
        F("BACKUP_CAMERA", "Backup Camera", "Safety");
        F("PARKING_SENSORS", "Parking Sensors", "Safety");
        F("AEB", "Automatic Emergency Braking", "Safety");

        F("CAMERA_360", "360 Camera", "Convenience");
        F("KEYLESS_ENTRY", "Keyless Entry", "Convenience");
        F("PUSH_BUTTON_START", "Push-Button Start", "Convenience");
        F("POWER_TAILGATE", "Power Tailgate", "Convenience");

        F("HEATED_SEATS", "Heated Seats", "Comfort");
        F("VENTILATED_SEATS", "Ventilated Seats", "Comfort");
        F("LEATHER_SEATS", "Leather Seats", "Comfort");
        F("HEATED_STEERING", "Heated Steering Wheel", "Comfort");

        F("NAVIGATION", "Navigation", "Infotainment");
        F("APPLE_CARPLAY", "Apple CarPlay", "Infotainment");
        F("ANDROID_AUTO", "Android Auto", "Infotainment");
        F("PREMIUM_AUDIO", "Premium Audio", "Infotainment");

        F("SUNROOF", "Sunroof", "Exterior");
        F("PANO_ROOF", "Panoramic Roof", "Exterior");
        F("LED_HEADLIGHTS", "LED Headlights", "Exterior");

        F("AWD", "All-Wheel Drive", "Drivetrain");
        F("FWD", "Front-Wheel Drive", "Drivetrain");
        F("RWD", "Rear-Wheel Drive", "Drivetrain");
        F("FOUR_WD", "4-Wheel Drive", "Drivetrain");
    }

    private static void SeedFeatureMappings(AppDbContext db)
    {
        if (db.FeatureMappings.Any()) return;
        var byCode = db.Features.ToDictionary(f => f.Code, f => f.Id);

        void M(string pattern, string featureCode, FeatureMatchType type = FeatureMatchType.Contains, int priority = 0)
        {
            if (byCode.TryGetValue(featureCode, out var fid))
                db.FeatureMappings.Add(new FeatureMapping { Pattern = pattern, FeatureId = fid, MatchType = type, Priority = priority });
        }

        M("adaptive cruise", "ADAPTIVE_CRUISE");
        M("blind spot", "BLIND_SPOT");
        M("lane keep", "LANE_KEEP");
        M("lane departure", "LANE_KEEP");
        M("backup camera", "BACKUP_CAMERA");
        M("rear view camera", "BACKUP_CAMERA");
        M("360", "CAMERA_360");
        M("surround view", "CAMERA_360");
        M("keyless", "KEYLESS_ENTRY");
        M("push button start", "PUSH_BUTTON_START");
        M("heated seat", "HEATED_SEATS");
        M("ventilated seat", "VENTILATED_SEATS");
        M("cooled seat", "VENTILATED_SEATS");
        M("leather", "LEATHER_SEATS");
        M("navigation", "NAVIGATION");
        M("apple carplay", "APPLE_CARPLAY");
        M("carplay", "APPLE_CARPLAY");
        M("android auto", "ANDROID_AUTO");
        M("sunroof", "SUNROOF");
        M("moonroof", "SUNROOF");
        M("panoramic", "PANO_ROOF");
        M("led headlight", "LED_HEADLIGHTS");
        M("all wheel drive", "AWD");
        M("all-wheel drive", "AWD");
        M("awd", "AWD");
        M("quattro", "AWD");
        M("xdrive", "AWD");
        M("4matic", "AWD");
        M("front wheel drive", "FWD");
        M("rear wheel drive", "RWD");
        M("4 wheel drive", "FOUR_WD");
        M("4wd", "FOUR_WD");
        M("4x4", "FOUR_WD");
    }

    private static void SeedMakers(AppDbContext db)
    {
        if (db.Makers.Any()) return;

        Maker Mk(string name, string country, params string[] aliases)
        {
            var m = new Maker { Name = name, NormalizedName = Normalize(name), CountryCode = country };
            foreach (var a in aliases)
                m.Aliases.Add(new MakerAlias { Alias = a.ToUpperInvariant(), Source = "seed" });
            return m;
        }

        var bmw = Mk("BMW", "DE", "BMW", "BMW GROUP");
        db.Makers.AddRange(
            bmw,
            Mk("Audi", "DE", "AUDI"),
            Mk("Mercedes-Benz", "DE", "MERCEDES-BENZ", "MERCEDES", "MERCEDES BENZ", "MB"),
            Mk("Volkswagen", "DE", "VOLKSWAGEN", "VW"),
            Mk("Toyota", "JP", "TOYOTA"),
            Mk("Lexus", "JP", "LEXUS"),
            Mk("Honda", "JP", "HONDA"),
            Mk("Nissan", "JP", "NISSAN"),
            Mk("Subaru", "JP", "SUBARU"),
            Mk("Mazda", "JP", "MAZDA"),
            Mk("Hyundai", "KR", "HYUNDAI"),
            Mk("Kia", "KR", "KIA"),
            Mk("Ford", "US", "FORD"),
            Mk("Chevrolet", "US", "CHEVROLET", "CHEVY"),
            Mk("Tesla", "US", "TESLA"));

        // Seed one model family to demonstrate alias resolution (the user's example).
        var threeSeries = new Model
        {
            Maker = bmw,
            Name = "3 Series",
            NormalizedName = Normalize("3 Series"),
            DefaultBodyStyle = "Sedan",
        };
        foreach (var a in new[] { "3 SERIES", "3-SERIES", "3ER", "3 SERIE" })
            threeSeries.Aliases.Add(new ModelAlias { Alias = a, Source = "seed" });
        foreach (var t in new[] { "320i", "328i", "330i", "330e", "335i", "340i", "M3" })
            threeSeries.Trims.Add(new Trim { Name = t, NormalizedName = Normalize(t) });
        db.Models.Add(threeSeries);
    }

    private static string Normalize(string s) =>
        new string(s.Trim().ToLowerInvariant().Where(c => !char.IsWhiteSpace(c) || c == ' ').ToArray());
}
