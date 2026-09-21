using FluentChunker;
using FluentChunker.Tokenizers;

public class MarkdownChunker
{
    public static async Task<List<ChunkMetadata>> ChunkearMarkdownAsync(
        string rutaArchivoMd,
        string nombreLibro,
        int maxTokens = 300)
    {
        var texto = await File.ReadAllTextAsync(rutaArchivoMd);
        return await ChunkearMarkdownDesdeTextoAsync(texto, nombreLibro, maxTokens);
    }

    public static async Task<List<ChunkMetadata>> ChunkearMarkdownDesdeTextoAsync(
        string textoMarkdown,
        string nombreLibro,
        int maxTokens = 300)
    {
        var chunks = new List<ChunkMetadata>();

        var tokenizer = new MicrosoftMLTokenizerAdapter("cl100k_base");

        var pipeline = ChunkingPipeline.CreateBuilder()
            .UseTokenizer(tokenizer)
            .UseSplitter(s => s
                .WithStructureAwareChunking()
                .WithMaxTokenSize(maxTokens))
            .Build();

        await foreach (var chunk in pipeline.ChunkAsync(textoMarkdown))
        {
            chunks.Add(new ChunkMetadata
            {
                Libro = nombreLibro,
                Seccion = chunk.Metadata["headingPath"].GetString() ?? "",
                NumeroChunk = chunks.Count,
                Texto = chunk.Text
            });
        }

        return chunks;
    }

    public static void GuardarChunks(List<ChunkMetadata> chunks, string rutaJson)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(rutaJson)!);
        var json = System.Text.Json.JsonSerializer.Serialize(chunks, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(rutaJson, json);
        Console.WriteLine($"Chunks guardados en: {rutaJson}");
    }

    public static List<ChunkMetadata> LeerChunks(string rutaJson)
    {
        if (!File.Exists(rutaJson))
            throw new FileNotFoundException($"No se encontró el archivo de chunks: {rutaJson}");
        var json = File.ReadAllText(rutaJson);
        return System.Text.Json.JsonSerializer.Deserialize<List<ChunkMetadata>>(json)!;
    }
}