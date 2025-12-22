using System.Net.Http;
using System.Text.Json;
using System.Text;

namespace HighAgentsBackend.Services;

public class PineconeService
{
    private readonly HttpClient _httpClient;
    private readonly OpenAIService _openAIService;
    private readonly string _apiKey;
    private readonly string _indexName;
    private readonly string _environment;

    public PineconeService(HttpClient httpClient, OpenAIService openAIService)
    {
        _httpClient = httpClient;
        _openAIService = openAIService;
        _apiKey = Environment.GetEnvironmentVariable("PINECONE_API_KEY") ?? "";
        _indexName = Environment.GetEnvironmentVariable("PINECONE_INDEX_NAME") ?? "high-agents-memory";
        _environment = Environment.GetEnvironmentVariable("PINECONE_ENVIRONMENT") ?? "us-east1-gcp";
        _httpClient.DefaultRequestHeaders.Add("Api-Key", _apiKey);
    }

    public async Task UpsertAsync(string text, string id)
    {
        try
        {
            var embedding = await GetEmbeddingAsync(text);
            if (embedding.Length == 0)
            {
                Console.WriteLine("Pinecone: Skipping upsert due to embedding failure");
                return;
            }

            var upsertRequest = new
            {
                vectors = new[]
                {
                    new
                    {
                        id = id,
                        values = embedding,
                        metadata = new { text = text }
                    }
                }
            };

            var content = new StringContent(JsonSerializer.Serialize(upsertRequest), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"https://{_indexName}-{_environment}.svc.pinecone.io/vectors/upsert", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Pinecone upsert error: {response.StatusCode} - {errorContent}");
                return;
            }

            Console.WriteLine($"Pinecone: Successfully upserted vector for {id}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Pinecone upsert error: {ex.Message}");
            // Continue without throwing - memory operations are not critical
        }
    }

    public async Task<string> QueryAsync(string query)
    {
        try
        {
            var embedding = await GetEmbeddingAsync(query);
            if (embedding.Length == 0)
            {
                Console.WriteLine("Pinecone: Skipping query due to embedding failure");
                return "";
            }

            var queryRequest = new
            {
                vector = embedding,
                topK = 5,
                includeMetadata = true
            };

            var content = new StringContent(JsonSerializer.Serialize(queryRequest), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"https://{_indexName}-{_environment}.svc.pinecone.io/query", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Pinecone query error: {response.StatusCode} - {errorContent}");
                return "";
            }

            var result = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<JsonElement>(result);
            var matches = data.GetProperty("matches");

            var relevant = "";
            foreach (var match in matches.EnumerateArray())
            {
                relevant += match.GetProperty("metadata").GetProperty("text").GetString() + "\n";
            }

            Console.WriteLine($"Pinecone: Found {matches.GetArrayLength()} relevant matches");
            return relevant;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Pinecone query error: {ex.Message}");
            // Return empty if Pinecone is not configured or fails
            return "";
        }
    }

    private async Task<float[]> GetEmbeddingAsync(string text)
    {
        return await _openAIService.GetEmbeddingAsync(text);
    }
}