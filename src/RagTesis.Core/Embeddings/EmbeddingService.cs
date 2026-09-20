using Microsoft.Extensions.AI;
using Mscc.GenerativeAI.Microsoft;
using Microsoft.Extensions.Logging;

namespace RagTesis.Embeddings;

public class EmbeddingService
{
    private readonly IEmbeddingGenerator<string, Embedding<float>> _generator;

    public EmbeddingService(string apiKey, string modelo = EmbeddingConfig.Modelo)
    {
        _generator = new GeminiEmbeddingGenerator(apiKey, modelo, (ILogger?)null);
    }

    public async Task<float[]> GenerarEmbeddingAsync(string texto)
    {
        var resultado = await _generator.GenerateAsync([texto]);
        return resultado[0].Vector.ToArray();
    }

    // Para procesar muchos chunks respetando el rate limit gratuito de Gemini
    public async Task<List<(ChunkMetadata Chunk, float[] Vector)>> GenerarEmbeddingsAsync(
        List<ChunkMetadata> chunks,
        int delayMs = 4000) // ~15 requests/minuto, con margen
    {
        var resultados = new List<(ChunkMetadata, float[])>();

        foreach (var chunk in chunks)
        {
            var vector = await GenerarEmbeddingAsync(chunk.Texto);
            resultados.Add((chunk, vector));
            Console.WriteLine($"Embedding generado: chunk #{chunk.NumeroChunk} ({resultados.Count}/{chunks.Count})");
            await Task.Delay(delayMs);
        }

        return resultados;
    }
}