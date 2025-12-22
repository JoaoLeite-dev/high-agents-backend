using System.Collections.Generic;

namespace HighAgentsBackend.Models;

public class Conversation
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public List<Message> Messages { get; set; } = new();
    public Dictionary<string, string> Slots { get; set; } = new();
    public int CurrentStep { get; set; } = 0;
    public bool IsCompleted { get; set; } = false;
}