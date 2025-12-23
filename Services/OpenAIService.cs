using System.Net.Http.Json;
using System.Text.Json;
using System.Text;
using HighAgentsBackend.Models;

namespace HighAgentsBackend.Services;

/// <summary>
/// Serviço para integração com a API da OpenAI
/// Gerencia chamadas para GPT-4o e GPT-3.5-turbo com suporte a function calling
/// </summary>
public class OpenAIService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public OpenAIService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "";
        Console.WriteLine($"Chave da API OpenAI carregada: {!string.IsNullOrEmpty(_apiKey)}");
        if (!string.IsNullOrEmpty(_apiKey))
        {
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
        }
    }

    /// <summary>
    /// Obtém resposta do chat GPT com suporte a ferramentas (function calling)
    /// </summary>
    /// <param name="prompt">Prompt do sistema (não usado diretamente)</param>
    /// <param name="messages">Histórico de mensagens da conversa</param>
    /// <param name="tools">Lista de ferramentas disponíveis</param>
    /// <returns>Tupla com resposta e chamadas de ferramentas</returns>
    public async Task<(string response, List<ToolCall> toolCalls)> GetChatCompletionWithToolsAsync(string prompt, List<Message> messages, List<Tool> tools)
    {
        try
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                return ("Desculpe, a chave da API da OpenAI não está configurada.", new List<ToolCall>());
            }

            var requestBody = new
            {
                model = "gpt-4o",
                messages = messages.Select(m => new
                {
                    role = m.Role,
                    content = m.Content
                }).ToList(),
                max_tokens = 1000,
                temperature = 0.7,
                tools = tools.Select(t => new
                {
                    type = "function",
                    function = new
                    {
                        name = t.Name,
                        description = t.Description,
                        parameters = t.Parameters
                    }
                }).ToList()
            };

            var response = await _httpClient.PostAsJsonAsync("https://api.openai.com/v1/chat/completions", requestBody);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Erro na API OpenAI: {response.StatusCode} - {errorContent}");

                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests ||
                    errorContent.Contains("insufficient_quota"))
                {
                    // Tenta fallback para GPT-3.5-turbo em caso de quota excedida
                    Console.WriteLine("Tentando fallback para GPT-3.5-turbo devido a limite de quota...");
                    return await GetChatCompletionWithToolsFallbackAsync(prompt, messages, tools);
                }

                return ("Desculpe, houve um erro ao processar sua mensagem. Tente novamente.", new List<ToolCall>());
            }

            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            var message = result.GetProperty("choices")[0].GetProperty("message");
            var content = message.GetProperty("content").GetString() ?? "";
            var toolCalls = new List<ToolCall>();

            if (message.TryGetProperty("tool_calls", out var toolCallsElement))
            {
                foreach (var toolCall in toolCallsElement.EnumerateArray())
                {
                    toolCalls.Add(new ToolCall
                    {
                        Id = toolCall.GetProperty("id").GetString(),
                        Type = toolCall.GetProperty("type").GetString(),
                        Function = new FunctionCall
                        {
                            Name = toolCall.GetProperty("function").GetProperty("name").GetString(),
                            Arguments = toolCall.GetProperty("function").GetProperty("arguments").GetString()
                        }
                    });
                }
            }

            return (content, toolCalls);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro na API OpenAI: {ex.Message}");
            return ("Desculpe, houve um erro ao processar sua mensagem. Tente novamente.", new List<ToolCall>());
        }
    }

    /// <summary>
    /// Obtém resposta simples do chat GPT (sem ferramentas)
    /// </summary>
    public async Task<string> GetChatCompletionAsync(string prompt, List<Message> messages)
    {
        var (response, _) = await GetChatCompletionWithToolsAsync(prompt, messages, new List<Tool>());
        return response;
    }

    /// <summary>
    /// Gera embedding vetorial para um texto usando OpenAI
    /// </summary>
    /// <param name="text">Texto para gerar embedding</param>
    /// <returns>Array de floats representando o embedding</returns>
    public async Task<float[]> GetEmbeddingAsync(string text)
    {
        try
        {
            var requestBody = new
            {
                input = text,
                model = "text-embedding-ada-002"
            };

            var response = await _httpClient.PostAsJsonAsync("https://api.openai.com/v1/embeddings", requestBody);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Erro na API de Embeddings OpenAI: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            }

            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            var embeddingArray = result.GetProperty("data")[0].GetProperty("embedding");

            return embeddingArray.EnumerateArray().Select(x => (float)x.GetDouble()).ToArray();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro na API de Embeddings OpenAI: {ex.Message}");
            return Array.Empty<float>();
        }
    }

    /// <summary>
    /// Método de fallback que usa GPT-3.5-turbo quando GPT-4o falha por quota
    /// </summary>
    private async Task<(string response, List<ToolCall> toolCalls)> GetChatCompletionWithToolsFallbackAsync(string prompt, List<Message> messages, List<Tool> tools)
    {
        try
        {
            var requestBody = new
            {
                model = "gpt-3.5-turbo",
                messages = messages.Select(m => new
                {
                    role = m.Role,
                    content = m.Content
                }).ToList(),
                max_tokens = 1000,
                temperature = 0.7,
                tools = tools.Select(t => new
                {
                    type = "function",
                    function = new
                    {
                        name = t.Name,
                        description = t.Description,
                        parameters = t.Parameters
                    }
                }).ToList()
            };

            var response = await _httpClient.PostAsJsonAsync("https://api.openai.com/v1/chat/completions", requestBody);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Erro na API GPT-3.5-turbo OpenAI: {response.StatusCode} - {errorContent}");

                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests ||
                    errorContent.Contains("insufficient_quota"))
                {
                    return ("Desculpe, a quota da API da OpenAI foi excedida para ambos os modelos (GPT-4 e GPT-3.5). Por favor, verifique seus créditos na plataforma da OpenAI.", new List<ToolCall>());
                }

                return ("Desculpe, houve um erro ao processar sua mensagem. Tente novamente.", new List<ToolCall>());
            }

            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            var message = result.GetProperty("choices")[0].GetProperty("message");
            var content = message.GetProperty("content").GetString() ?? "";
            var toolCalls = new List<ToolCall>();

            if (message.TryGetProperty("tool_calls", out var toolCallsElement))
            {
                foreach (var toolCall in toolCallsElement.EnumerateArray())
                {
                    toolCalls.Add(new ToolCall
                    {
                        Id = toolCall.GetProperty("id").GetString(),
                        Type = toolCall.GetProperty("type").GetString(),
                        Function = new FunctionCall
                        {
                            Name = toolCall.GetProperty("function").GetProperty("name").GetString(),
                            Arguments = toolCall.GetProperty("function").GetProperty("arguments").GetString()
                        }
                    });
                }
            }

            return (content, toolCalls);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"OpenAI GPT-3.5-turbo API error: {ex.Message}");
            return ("Desculpe, houve um erro ao processar sua mensagem. Tente novamente.", new List<ToolCall>());
        }
    }
}

public class Tool
{
    public string Name { get; set; }
    public string Description { get; set; }
    public object Parameters { get; set; }
}

public class ToolCall
{
    public string Id { get; set; }
    public string Type { get; set; }
    public FunctionCall Function { get; set; }
}

public class FunctionCall
{
    public string Name { get; set; }
    public string Arguments { get; set; }
}