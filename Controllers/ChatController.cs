using Microsoft.AspNetCore.Mvc;
using HighAgentsBackend.Models;
using HighAgentsBackend.Services;
using System.ComponentModel.DataAnnotations;

namespace HighAgentsBackend.Controllers;

/// <summary>
/// Controller para gerenciar interações de chat com o agente de IA
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private static readonly Dictionary<string, Conversation> _conversations = new();
    private readonly AgentService _agentService;

    /// <summary>
    /// Construtor do ChatController
    /// </summary>
    /// <param name="agentService">Serviço do agente de IA</param>
    public ChatController(AgentService agentService)
    {
        _agentService = agentService;
    }

    /// <summary>
    /// Inicia uma nova conversa com o agente de IA
    /// </summary>
    /// <returns>ID da conversa criada</returns>
    [HttpPost("start")]
    public IActionResult StartConversation()
    {
        var conversation = new Conversation();
        _conversations[conversation.Id] = conversation;
        return Ok(new { conversationId = conversation.Id });
    }

    /// <summary>
    /// Envia uma mensagem para o agente de IA e recebe a resposta
    /// </summary>
    /// <param name="request">Requisição contendo ID da conversa e mensagem</param>
    /// <returns>Resposta do agente de IA</returns>
    [HttpPost("send")]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
    {
        if (request == null)
        {
            return BadRequest("Corpo da requisição é obrigatório");
        }

        if (string.IsNullOrEmpty(request.ConversationId))
        {
            return BadRequest("ConversationId é obrigatório");
        }

        if (string.IsNullOrEmpty(request.Message))
        {
            return BadRequest("Mensagem é obrigatória");
        }

        if (!_conversations.TryGetValue(request.ConversationId, out var conversation))
        {
            return NotFound("Conversa não encontrada");
        }

        var response = await _agentService.ProcessMessageAsync(conversation, request.Message);
        return Ok(new { response });
    }

    /// <summary>
    /// Endpoint de teste para verificar se a API está funcionando
    /// </summary>
    /// <param name="request">Requisição de teste</param>
    /// <returns>Confirmação de recebimento</returns>
    [HttpPost("test")]
    public IActionResult Test([FromBody] TestRequest request)
    {
        return Ok(new { received = request != null, conversationId = request?.ConversationId, message = request?.Message });
    }

    /// <summary>
    /// Obtém os detalhes de uma conversa específica
    /// </summary>
    /// <param name="conversationId">ID da conversa</param>
    /// <returns>Detalhes da conversa</returns>
    [HttpGet("{conversationId}")]
    public IActionResult GetConversation(string conversationId)
    {
        if (!_conversations.TryGetValue(conversationId, out var conversation))
        {
            return NotFound("Conversa não encontrada");
        }

        return Ok(conversation);
    }
}

/// <summary>
/// Modelo de requisição para envio de mensagem
/// </summary>
public class SendMessageRequest
{
    /// <summary>
    /// ID da conversa
    /// </summary>
    [Required]
    public string ConversationId { get; set; }

    /// <summary>
    /// Conteúdo da mensagem
    /// </summary>
    [Required]
    public string Message { get; set; }
}

/// <summary>
/// Modelo de requisição para teste da API
/// </summary>
public class TestRequest
{
    /// <summary>
    /// ID da conversa
    /// </summary>
    public string ConversationId { get; set; }

    /// <summary>
    /// Conteúdo da mensagem
    /// </summary>
    public string Message { get; set; }
}