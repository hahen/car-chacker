namespace CarPrices.Domain.Entities;

/// <summary>A vehicle make, e.g. BMW, Audi, Toyota.</summary>
public class Maker
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public string? CountryCode { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Model> Models { get; set; } = new List<Model>();
    public ICollection<MakerAlias> Aliases { get; set; } = new List<MakerAlias>();
}

/// <summary>An alternate spelling of a make as seen in a source (e.g. "VW" -> Volkswagen).</summary>
public class MakerAlias
{
    public long Id { get; set; }
    public long MakerId { get; set; }
    public Maker? Maker { get; set; }
    public string Alias { get; set; } = string.Empty;
    public string? Source { get; set; }
}

/// <summary>A model within a make, e.g. 3 Series, A4, Camry.</summary>
public class Model
{
    public long Id { get; set; }
    public long MakerId { get; set; }
    public Maker? Maker { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public string? DefaultBodyStyle { get; set; }

    public ICollection<Trim> Trims { get; set; } = new List<Trim>();
    public ICollection<ModelAlias> Aliases { get; set; } = new List<ModelAlias>();
}

public class ModelAlias
{
    public long Id { get; set; }
    public long ModelId { get; set; }
    public Model? Model { get; set; }
    public string Alias { get; set; } = string.Empty;
    public string? Source { get; set; }
}

/// <summary>A trim/variant within a model, e.g. 330i, S4, SE.</summary>
public class Trim
{
    public long Id { get; set; }
    public long ModelId { get; set; }
    public Model? Model { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public string? EngineCode { get; set; }

    public ICollection<TrimAlias> Aliases { get; set; } = new List<TrimAlias>();
}

public class TrimAlias
{
    public long Id { get; set; }
    public long TrimId { get; set; }
    public Trim? Trim { get; set; }
    public string Alias { get; set; } = string.Empty;
    public string? Source { get; set; }
}
