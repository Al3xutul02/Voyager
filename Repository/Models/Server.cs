namespace Repository.Models;

public class Server
{
    public ulong Id { get; set; }
    
    public string Name { get; set; } = null!;

    public string Settings { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? DeletedAt { get; set; }
}
