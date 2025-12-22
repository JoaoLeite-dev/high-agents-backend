using HighAgentsBackend.Services;
using HighAgentsBackend.Models;
using System.Text.Json;

public class AgentService
{
    private readonly OpenAIService _openAIService;
    private readonly PineconeService _pineconeService;

    public AgentService(OpenAIService openAIService, PineconeService pineconeService)
    {
        _openAIService = openAIService;
        _pineconeService = pineconeService;
    }

    public async Task<string> ProcessMessageAsync(Conversation conversation, string userMessage)
    {
        // Add user message
        conversation.Messages.Add(new Message { Role = "user", Content = userMessage });

        // Build prompt with context
        var systemPrompt = GetSystemPrompt(conversation);

        // Get relevant knowledge from RAG
        var knowledge = await GetRelevantKnowledgeAsync(userMessage);

        // Build messages for AI
        var messages = new List<Message>
        {
            new Message { Role = "system", Content = systemPrompt + "\n\nRelevant Knowledge:\n" + knowledge }
        };

        messages.AddRange(conversation.Messages);

        var assistantMessage = await _openAIService.GetChatCompletionAsync(systemPrompt + "\n\nRelevant Knowledge:\n" + knowledge, messages);

        // Add assistant message
        conversation.Messages.Add(new Message { Role = "assistant", Content = assistantMessage });

        // Update slots and step
        UpdateConversationState(conversation, userMessage, assistantMessage);

        // Store in long-term memory
        await StoreInMemoryAsync(conversation);

        return assistantMessage;
    }

    private string GetSystemPrompt(Conversation conversation)
    {
        var steps = new[]
        {
            "1. Recepção inicial do paciente: Saudar e apresentar-se como assistente da clínica.",
            "2. Coleta do nome e tipo de procedimento desejado: Perguntar nome e procedimento.",
            "3. Confirmação da unidade e horários disponíveis: Confirmar unidade e mostrar horários.",
            "4. Verificação de disponibilidade: Verificar se o horário escolhido está disponível.",
            "5. Agendamento: Agendar o procedimento e enviar confirmação."
        };

        var currentStepDesc = steps[conversation.CurrentStep];

        return $@"
Você é um assistente de IA para clínicas, atuando como SDR digital. Seu objetivo é guiar o paciente através de um fluxo de agendamento de procedimentos médicos.

Fluxo atual: {currentStepDesc}

Slots preenchidos: {JsonSerializer.Serialize(conversation.Slots)}

Instruções:
- Seja amigável e profissional.
- Guie a conversa para preencher os slots necessários.
- Use function calling para ações como verificar disponibilidade ou agendar.
- Se não conseguir lidar com a solicitação, redirecione para um humano.
- Mantenha o contexto da conversa.
";
    }

    private async Task<string> GetRelevantKnowledgeAsync(string query)
    {
        // Use Pinecone to retrieve relevant docs
        return await _pineconeService.QueryAsync(query);
    }

    private void UpdateConversationState(Conversation conversation, string userMessage, string assistantMessage)
    {
        // Simple slot filling logic
        if (!conversation.Slots.ContainsKey("name") && userMessage.Contains("meu nome é"))
        {
            conversation.Slots["name"] = userMessage.Split("meu nome é")[1].Trim();
        }
        // Add more logic for other slots

        // Advance step if conditions met
        if (conversation.CurrentStep < 4 && conversation.Slots.Count >= conversation.CurrentStep + 1)
        {
            conversation.CurrentStep++;
        }
        if (conversation.CurrentStep == 4)
        {
            conversation.IsCompleted = true;
        }
    }

    private async Task StoreInMemoryAsync(Conversation conversation)
    {
        // Store summary in Pinecone
        var summary = $"Conversation {conversation.Id}: {string.Join(" ", conversation.Slots)}";
        await _pineconeService.UpsertAsync(summary, conversation.Id);
    }
}