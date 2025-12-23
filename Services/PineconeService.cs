using System.Net.Http;
using System.Text.Json;
using System.Text;

namespace HighAgentsBackend.Services;

/// <summary>
/// Serviço para integração com Pinecone (banco de dados vetorial)
/// Gerencia armazenamento e recuperação de memórias de longo prazo
/// </summary>
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
        Console.WriteLine($"Chave da API Pinecone carregada: {!string.IsNullOrEmpty(_apiKey)}");
        Console.WriteLine($"Índice Pinecone: {_indexName}, Ambiente: {_environment}");
        _httpClient.DefaultRequestHeaders.Add("Api-Key", _apiKey);
    }

    /// <summary>
    /// Armazena um vetor no índice Pinecone
    /// </summary>
    /// <param name="text">Texto para gerar embedding e armazenar</param>
    /// <param name="id">ID único do vetor</param>
    public async Task UpsertAsync(string text, string id)
    {
        try
        {
            var embedding = await GetEmbeddingAsync(text);
            if (embedding.Length == 0)
            {
                Console.WriteLine("Pinecone: Pulando upsert devido a falha no embedding");
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
                Console.WriteLine($"Erro no upsert Pinecone: {response.StatusCode} - {errorContent}");
                return;
            }

            Console.WriteLine($"Pinecone: Vetor inserido com sucesso para {id}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro no upsert Pinecone: {ex.Message}");
            // Continua sem lançar exceção - operações de memória não são críticas
        }
    }

    /// <summary>
    /// Consulta vetores similares no índice Pinecone
    /// </summary>
    /// <param name="query">Texto para buscar vetores similares</param>
    /// <returns>Texto relevante encontrado na busca</returns>
    public async Task<string> QueryAsync(string query)
    {
        try
        {
            var embedding = await GetEmbeddingAsync(query);
            if (embedding.Length == 0)
            {
                Console.WriteLine("Pinecone: Pulando consulta devido a falha no embedding");
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
                Console.WriteLine($"Erro na consulta Pinecone: {response.StatusCode} - {errorContent}");
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

            Console.WriteLine($"Pinecone: Encontrados {matches.GetArrayLength()} resultados relevantes");
            return relevant;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro na consulta Pinecone: {ex.Message}");
            // Retorna vazio se Pinecone não estiver configurado ou falhar
            return "";
        }
    }

    /// <summary>
    /// Gera embedding para um texto usando OpenAI
    /// </summary>
    private async Task<float[]> GetEmbeddingAsync(string text)
    {
        return await _openAIService.GetEmbeddingAsync(text);
    }
}