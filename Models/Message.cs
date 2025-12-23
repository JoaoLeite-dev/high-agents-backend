namespace HighAgentsBackend.Models;

/// <summary>
/// Representa uma mensagem na conversa
/// Pode ser do usuário ou do assistente
/// </summary>
public class Message
{
    /// <summary>
    /// Papel do emissor da mensagem: "user" ou "assistant"
    /// </summary>
    public string Role { get; set; }

    /// <summary>
    /// Conteúdo da mensagem
    /// </summary>
    public string Content { get; set; }

    /// <summary>
    /// Data e hora em que a mensagem foi criada
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}