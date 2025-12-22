using System.Net.Http.Json;
using System.Text.Json;
using System.Text;
using HighAgentsBackend.Models;

namespace HighAgentsBackend.Services;

public class OpenAIService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public OpenAIService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? throw new InvalidOperationException("OPENAI_API_KEY not found");
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
    }

    public async Task<string> GetChatCompletionAsync(string prompt, List<Message> messages)
    {
        try
        {
            var requestBody = new
            {
                model = "gpt-4o",
                messages = messages.Select(m => new
                {
                    role = m.Role,
                    content = m.Content
                }).ToList(),
                max_tokens = 1000,
                temperature = 0.7
            };

            var response = await _httpClient.PostAsJsonAsync("https://api.openai.com/v1/chat/completions", requestBody);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"OpenAI API error: {response.StatusCode} - {errorContent}");

                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests ||
                    errorContent.Contains("insufficient_quota"))
                {
                    return "Desculpe, houve um problema com a API da OpenAI (quota excedida). Por favor, verifique seus créditos na plataforma da OpenAI.";
                }

                return "Desculpe, houve um erro ao processar sua mensagem. Tente novamente.";
            }

            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            return result.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"OpenAI API error: {ex.Message}");
            return "Desculpe, houve um erro ao processar sua mensagem. Tente novamente.";
        }
    }

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
                throw new Exception($"OpenAI Embeddings API error: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            }

            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            var embeddingArray = result.GetProperty("data")[0].GetProperty("embedding");

            return embeddingArray.EnumerateArray().Select(x => (float)x.GetDouble()).ToArray();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"OpenAI Embeddings API error: {ex.Message}");
            return Array.Empty<float>();
        }
    }
}