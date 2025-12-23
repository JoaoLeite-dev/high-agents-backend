using System.Collections.Generic;

namespace HighAgentsBackend.Models;

/// <summary>
/// Representa uma conversa com o agente de IA
/// Contém histórico de mensagens, slots preenchidos e estado da conversa
/// </summary>
public class Conversation
{
    /// <summary>
    /// ID único da conversa
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Lista de mensagens trocadas na conversa
    /// </summary>
    public List<Message> Messages { get; set; } = new();

    /// <summary>
    /// Dicionário com informações extraídas da conversa (slots)
    /// Ex: nome, procedimento, unidade, data, horário
    /// </summary>
    public Dictionary<string, string> Slots { get; set; } = new();

    /// <summary>
    /// Etapa atual do fluxo de agendamento (0-4)
    /// </summary>
    public int CurrentStep { get; set; } = 0;

    /// <summary>
    /// Indica se a conversa foi concluída com sucesso
    /// </summary>
    public bool IsCompleted { get; set; } = false;
}