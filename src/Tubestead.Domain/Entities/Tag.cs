namespace Tubestead.Domain.Entities;

/// <summary>A free-form label used to organize and search videos.</summary>
public class Tag
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Display name as typed, e.g. "Chicken Coop".</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>URL/lookup-friendly form, e.g. "chicken-coop". Unique.</summary>
    public string Slug { get; set; } = string.Empty;

    public ICollection<Video> Videos { get; set; } = new List<Video>();
}
