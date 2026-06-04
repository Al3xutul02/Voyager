namespace Repository.Models;

/// <summary>
/// Database model for user information.
/// </summary>
public class User
{
    public ulong Id { get; set; }
    public string Name { get; set; } = null!;
    public string Settings { get; set; } = null!;
}