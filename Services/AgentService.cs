using HighAgentsBackend.Services;
using HighAgentsBackend.Models;
using System.Text.Json;

/// <summary>
/// Serviço responsável por orquestrar a conversa com o agente de IA
/// Gerencia o fluxo de agendamento médico e integração com ferramentas externas
/// </summary>
public class AgentService
{
    private readonly OpenAIService _openAIService;
    private readonly PineconeService _pineconeService;

    public AgentService(OpenAIService openAIService, PineconeService pineconeService)
    {
        _openAIService = openAIService;
        _pineconeService = pineconeService;
    }

    /// <summary>
    /// Processa uma mensagem do usuário e retorna a resposta do agente
    /// </summary>
    /// <param name="conversation">Conversa atual</param>
    /// <param name="userMessage">Mensagem do usuário</param>
    /// <returns>Resposta do agente</returns>
    public async Task<string> ProcessMessageAsync(Conversation conversation, string userMessage)
    {
        Console.WriteLine($"Processando mensagem para conversa {conversation.Id}: {userMessage}");

        conversation.Messages.Add(new Message { Role = "user", Content = userMessage });

        var systemPrompt = GetSystemPrompt(conversation);

        var knowledge = await GetRelevantKnowledgeAsync(userMessage);

        var tools = GetTools();

        var messages = new List<Message>
        {
            new Message { Role = "system", Content = systemPrompt + "\n\nConhecimento Relevante:\n" + knowledge }
        };
        messages.AddRange(conversation.Messages);

        var (assistantMessage, toolCalls) = await _openAIService.GetChatCompletionWithToolsAsync(
            systemPrompt + "\n\nConhecimento Relevante:\n" + knowledge, messages, tools);

        if (toolCalls.Any())
        {
            Console.WriteLine($"Chamadas de ferramentas detectadas: {string.Join(", ", toolCalls.Select(tc => tc.Function.Name))}");
            foreach (var toolCall in toolCalls)
            {
                var result = await ExecuteToolAsync(toolCall);
                messages.Add(new Message { Role = "assistant", Content = assistantMessage });
                messages.Add(new Message { Role = "tool", Content = result });
            }

            var (finalResponse, _) = await _openAIService.GetChatCompletionWithToolsAsync(
                systemPrompt + "\n\nConhecimento Relevante:\n" + knowledge, messages, tools);
            assistantMessage = finalResponse;
        }

        conversation.Messages.Add(new Message { Role = "assistant", Content = assistantMessage });

        UpdateConversationState(conversation, userMessage, assistantMessage);

        await StoreInMemoryAsync(conversation);

        Console.WriteLine($"Conversa {conversation.Id} atualizada. Etapa atual: {conversation.CurrentStep}, Slots: {string.Join(", ", conversation.Slots.Select(kv => $"{kv.Key}={kv.Value}"))}");

        return assistantMessage;
    }

    /// <summary>
    /// Define as ferramentas disponíveis para o agente de IA
    /// </summary>
    /// <returns>Lista de ferramentas configuradas</returns>
    private List<Tool> GetTools()
    {
        return new List<Tool>
        {
            new Tool
            {
                Name = "check_availability",
                Description = "Verifica a disponibilidade de horários para um procedimento em uma unidade específica.",
                Parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        procedure = new { type = "string", description = "Tipo de procedimento desejado" },
                        unit = new { type = "string", description = "Unidade da clínica" },
                        date = new { type = "string", description = "Data desejada (formato YYYY-MM-DD)" }
                    },
                    required = new[] { "procedure", "unit", "date" }
                }
            },
            new Tool
            {
                Name = "schedule_appointment",
                Description = "Agenda um procedimento para o paciente.",
                Parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        name = new { type = "string", description = "Nome do paciente" },
                        procedure = new { type = "string", description = "Tipo de procedimento" },
                        unit = new { type = "string", description = "Unidade da clínica" },
                        date = new { type = "string", description = "Data do agendamento" },
                        time = new { type = "string", description = "Horário do agendamento" }
                    },
                    required = new[] { "name", "procedure", "unit", "date", "time" }
                }
            },
            new Tool
            {
                Name = "send_confirmation",
                Description = "Envia uma mensagem de confirmação para o paciente.",
                Parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        name = new { type = "string", description = "Nome do paciente" },
                        procedure = new { type = "string", description = "Tipo de procedimento" },
                        unit = new { type = "string", description = "Unidade da clínica" },
                        date = new { type = "string", description = "Data do agendamento" },
                        time = new { type = "string", description = "Horário do agendamento" }
                    },
                    required = new[] { "name", "procedure", "unit", "date", "time" }
                }
            }
        };
    }

    /// <summary>
    /// Executa uma ferramenta chamada pelo agente de IA
    /// </summary>
    /// <param name="toolCall">Informações da chamada da ferramenta</param>
    /// <returns>Resultado da execução da ferramenta</returns>
    private async Task<string> ExecuteToolAsync(ToolCall toolCall)
    {
        switch (toolCall.Function.Name)
        {
            case "check_availability":
                var checkArgs = JsonSerializer.Deserialize<CheckAvailabilityArgs>(toolCall.Function.Arguments);
                return await CheckAvailabilityAsync(checkArgs);
            case "schedule_appointment":
                var scheduleArgs = JsonSerializer.Deserialize<ScheduleAppointmentArgs>(toolCall.Function.Arguments);
                return await ScheduleAppointmentAsync(scheduleArgs);
            case "send_confirmation":
                var sendArgs = JsonSerializer.Deserialize<SendConfirmationArgs>(toolCall.Function.Arguments);
                return await SendConfirmationAsync(sendArgs);
            default:
                return "Função não reconhecida.";
        }
    }

    /// <summary>
    /// Verifica disponibilidade de horários (implementação mock)
    /// </summary>
    private async Task<string> CheckAvailabilityAsync(CheckAvailabilityArgs args)
    {
        // Simulação - em produção isso consultaria o sistema de agendamento real
        var availableTimes = new[] { "09:00", "10:00", "14:00", "15:00" };
        return $"Horários disponíveis para {args.Procedure} na unidade {args.Unit} em {args.Date}: {string.Join(", ", availableTimes)}";
    }

    /// <summary>
    /// Agenda um procedimento (implementação mock)
    /// </summary>
    private async Task<string> ScheduleAppointmentAsync(ScheduleAppointmentArgs args)
    {
        // Simulação - em produção isso integraria com o sistema de agendamento
        return $"Agendamento confirmado para {args.Name}: {args.Procedure} na unidade {args.Unit} em {args.Date} às {args.Time}.";
    }

    /// <summary>
    /// Envia confirmação para o paciente (implementação mock)
    /// </summary>
    private async Task<string> SendConfirmationAsync(SendConfirmationArgs args)
    {
        // Simulação - em produção isso enviaria SMS/WhatsApp real
        return $"Confirmação enviada para {args.Name} via SMS/WhatsApp: Agendamento de {args.Procedure} na unidade {args.Unit} em {args.Date} às {args.Time}.";
    }

    /// <summary>
    /// Monta o prompt do sistema baseado no estado da conversa
    /// </summary>
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
- Se não conseguir lidar com a solicitação, redirecione para um humano: 'Por favor, aguarde enquanto transfiro para um atendente humano.'
- Mantenha o contexto da conversa.
";
    }

    /// <summary>
    /// Busca conhecimento relevante na base de dados vetorial
    /// </summary>
    private async Task<string> GetRelevantKnowledgeAsync(string query)
    {
        // Usa Pinecone para recuperar documentos relevantes via RAG
        return await _pineconeService.QueryAsync(query);
    }

    /// <summary>
    /// Atualiza o estado da conversa baseado nas mensagens
    /// Preenche slots automaticamente e avança etapas
    /// </summary>
    private void UpdateConversationState(Conversation conversation, string userMessage, string assistantMessage)
    {
        // Lógica melhorada de preenchimento de slots
        var lowerMessage = userMessage.ToLower();
        if (!conversation.Slots.ContainsKey("name") && (lowerMessage.Contains("meu nome é") || lowerMessage.Contains("eu sou")))
        {
            conversation.Slots["name"] = ExtractName(userMessage);
        }
        if (!conversation.Slots.ContainsKey("procedure") && (lowerMessage.Contains("procedimento") || lowerMessage.Contains("consulta")))
        {
            conversation.Slots["procedure"] = ExtractProcedure(userMessage);
        }
        if (!conversation.Slots.ContainsKey("unit") && lowerMessage.Contains("unidade"))
        {
            conversation.Slots["unit"] = ExtractUnit(userMessage);
        }
        if (!conversation.Slots.ContainsKey("date") && (lowerMessage.Contains("dia") || lowerMessage.Contains("data")))
        {
            conversation.Slots["date"] = ExtractDate(userMessage);
        }
        if (!conversation.Slots.ContainsKey("time") && lowerMessage.Contains("horário"))
        {
            conversation.Slots["time"] = ExtractTime(userMessage);
        }

        // Avança etapa se condições forem atendidas
        if (conversation.CurrentStep < 4 && conversation.Slots.Count >= conversation.CurrentStep + 1)
        {
            conversation.CurrentStep++;
        }
        if (conversation.CurrentStep == 4 && conversation.Slots.ContainsKey("name") && conversation.Slots.ContainsKey("procedure") && conversation.Slots.ContainsKey("unit") && conversation.Slots.ContainsKey("date") && conversation.Slots.ContainsKey("time"))
        {
            conversation.IsCompleted = true;
        }
    }

    /// <summary>
    /// Extrai o nome do paciente da mensagem (implementação simples)
    /// </summary>
    private string ExtractName(string message)
    {
        // Extração simples - pode ser melhorada com regex ou NLP
        var parts = message.Split(new[] { "meu nome é", "eu sou" }, StringSplitOptions.None);
        return parts.Length > 1 ? parts[1].Trim().Split(' ')[0] : "";
    }

    /// <summary>
    /// Extrai o tipo de procedimento da mensagem
    /// </summary>
    private string ExtractProcedure(string message)
    {
        // Extração mock - em produção usaria NLP ou lista de procedimentos
        if (message.ToLower().Contains("limpeza")) return "Limpeza Dental";
        if (message.ToLower().Contains("consulta")) return "Consulta Geral";
        return "Procedimento não especificado";
    }

    /// <summary>
    /// Extrai a unidade da mensagem (implementação mock)
    /// </summary>
    private string ExtractUnit(string message)
    {
        // Extração mock - em produção usaria regex para unidades
        if (message.ToLower().Contains("central")) return "Unidade Central";
        if (message.ToLower().Contains("norte")) return "Unidade Norte";
        return "Unidade Central";
    }

    /// <summary>
    /// Extrai a data da mensagem (implementação mock)
    /// </summary>
    private string ExtractDate(string message)
    {
        // Extração mock - em produção usaria regex para datas
        return DateTime.Now.AddDays(1).ToString("yyyy-MM-dd");
    }

    /// <summary>
    /// Extrai o horário da mensagem (implementação mock)
    /// </summary>
    private string ExtractTime(string message)
    {
        // Extração mock - em produção usaria regex para horários
        return "10:00";
    }

    /// <summary>
    /// Armazena um resumo da conversa na memória de longo prazo
    /// </summary>
    private async Task StoreInMemoryAsync(Conversation conversation)
    {
        // Armazena resumo no Pinecone para recuperação futura
        var summary = $"Conversa {conversation.Id}: {string.Join(" ", conversation.Slots)}";
        await _pineconeService.UpsertAsync(summary, conversation.Id);
    }
}

/// <summary>
/// Argumentos para verificação de disponibilidade
/// </summary>
public class CheckAvailabilityArgs
{
    public string Procedure { get; set; }
    public string Unit { get; set; }
    public string Date { get; set; }
}

/// <summary>
/// Argumentos para agendamento de procedimento
/// </summary>
public class ScheduleAppointmentArgs
{
    public string Name { get; set; }
    public string Procedure { get; set; }
    public string Unit { get; set; }
    public string Date { get; set; }
    public string Time { get; set; }
}

public class SendConfirmationArgs
{
    public string Name { get; set; }
    public string Procedure { get; set; }
    public string Unit { get; set; }
    public string Date { get; set; }
    public string Time { get; set; }
}