using Microsoft.AspNetCore.Mvc;
using HighAgentsBackend.Models;
using HighAgentsBackend.Services;
using System.ComponentModel.DataAnnotations;

namespace HighAgentsBackend.Controllers;

/// <summary>
/// Controller for handling chat interactions with the AI agent.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private static readonly Dictionary<string, Conversation> _conversations = new();
    private readonly AgentService _agentService;

    public ChatController(AgentService agentService)
    {
        _agentService = agentService;
    }

    /// <summary>
    /// Starts a new conversation with the AI agent.
    /// </summary>
    /// <returns>The conversation ID.</returns>
    [HttpPost("start")]
    public IActionResult StartConversation()
    {
        var conversation = new Conversation();
        _conversations[conversation.Id] = conversation;
        return Ok(new { conversationId = conversation.Id });
    }

    /// <summary>
    /// Sends a message to the AI agent and receives a response.
    /// </summary>
    /// <param name="request">The message request containing conversation ID and message.</param>
    /// <returns>The AI agent's response.</returns>
    [HttpPost("send")]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
    {
        if (request == null)
        {
            return BadRequest("Request body is required");
        }

        if (string.IsNullOrEmpty(request.ConversationId))
        {
            return BadRequest("ConversationId is required");
        }

        if (string.IsNullOrEmpty(request.Message))
        {
            return BadRequest("Message is required");
        }

        if (!_conversations.TryGetValue(request.ConversationId, out var conversation))
        {
            return NotFound("Conversation not found");
        }

        var response = await _agentService.ProcessMessageAsync(conversation, request.Message);
        return Ok(new { response });
    }

    [HttpPost("test")]
    public IActionResult Test([FromBody] TestRequest request)
    {
        return Ok(new { received = request != null, conversationId = request?.ConversationId, message = request?.Message });
    }

public class TestRequest
{
    public string ConversationId { get; set; }
    public string Message { get; set; }
}
    [HttpGet("{conversationId}")]
    public IActionResult GetConversation(string conversationId)
    {
        if (!_conversations.TryGetValue(conversationId, out var conversation))
        {
            return NotFound("Conversation not found");
        }

        return Ok(conversation);
    }
}

/// <summary>
/// Request model for sending a message.
public class SendMessageRequest
{
    /// <summary>
    /// The conversation ID.
    /// </summary>
    [Required]
    public string ConversationId { get; set; }

    /// <summary>
    /// The message content.
    /// </summary>
    [Required]
    public string Message { get; set; }
}