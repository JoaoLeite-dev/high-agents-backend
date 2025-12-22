namespace HighAgentsBackend.Models;

public class Message
{
    public string Role { get; set; } // user, assistant
    public string Content { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}