using System.Reflection;
using Microsoft.Extensions.Configuration;
using RagTesis.Embeddings;

// ── Rutas y configuración ──────────────────────────────────────────────
var rutaMd = "../../output/markdown/dunn_capitulo_26_la_mision_de_pedro.md";
var rutaChunks = "../../output/chunks/dunn_capitulo_26_la_mision_de_pedro.chunks.json";
var forzarRechunkeo = args.Contains("--rechunk");

// ── Chunking (o carga desde disco si ya existe) ────────────────────────
List<ChunkMetadata> chunks;

if (File.Exists(rutaChunks) && !forzarRechunkeo)
{
    chunks = MarkdownChunker.LeerChunks(rutaChunks);
    Console.WriteLine($"Chunks cargados desde disco: {chunks.Count}");
}
else
{
    chunks = await MarkdownChunker.ChunkearMarkdownAsync(rutaMd, "Dunn - Beginning from Jerusalem, Cap. 26");
    MarkdownChunker.GuardarChunks(chunks, rutaChunks);
}

// ── Filtramos chunks sin sección (metadata YAML, etc.) ─────────────────
chunks = chunks.Where(c => !string.IsNullOrWhiteSpace(c.Seccion)).ToList();
Console.WriteLine($"Chunks después de filtrar metadata sin sección: {chunks.Count}");

// ── API key desde user-secrets ─────────────────────────────────────────
var config = new ConfigurationBuilder()
    .AddUserSecrets(Assembly.GetExecutingAssembly())
    .Build();

var apiKey = config["GoogleApiKey"]
    ?? throw new Exception("Falta configurar GoogleApiKey con dotnet user-secrets");

// ── Embeddings (llamadas a Gemini, ~14 min para 204 chunks) ────────────
var embeddingService = new EmbeddingService(apiKey);

var vectorStoreSetup = new VectorStoreSetup("mi-tesis"); 
await vectorStoreSetup.InicializarAsync();

var yaTieneChunks = await vectorStoreSetup.TieneChunksGuardadosAsync();

if (yaTieneChunks)
{
    Console.WriteLine("Qdrant ya tiene chunks guardados — salteando el paso de embeddings.");
}
else
{
    Console.WriteLine("Generando embeddings para todos los chunks (esto va a tardar)...");
    var chunksConEmbeddings = await embeddingService.GenerarEmbeddingsAsync(chunks);
    await vectorStoreSetup.GuardarChunksAsync(chunksConEmbeddings);
    Console.WriteLine($"¡Listo! {chunksConEmbeddings.Count} chunks con embeddings guardados en el vector store.");
}

// ── Prueba de retrieval ─────────────────────────────────────────────────
var pregunta = "¿Por qué Pedro dudaba en comer con gentiles?";
Console.WriteLine($"\nBuscando: \"{pregunta}\"");

var vectorPregunta = await embeddingService.GenerarEmbeddingAsync(pregunta);
var resultadosBusqueda = await vectorStoreSetup.BuscarAsync(vectorPregunta, top: 5);

Console.WriteLine($"\n--- Top {resultadosBusqueda.Count} resultados ---");
foreach (var (chunk, score) in resultadosBusqueda)
{
    Console.WriteLine($"\n[Score: {score:F4}] {chunk.Libro} — {chunk.Seccion}");
    Console.WriteLine(chunk.Texto.Substring(0, Math.Min(200, chunk.Texto.Length)) + "...");
}

// ── Revisor: evaluamos la afirmación con dos proveedores distintos ────
var nvidiaApiKey = config["NvidiaApiKey"] ?? throw new Exception("Falta NvidiaApiKey en user-secrets");

var afirmacionDePrueba = "Pedro aceptó comer con Cornelio sin ninguna resistencia inicial, mostrando total apertura hacia los gentiles desde el principio.";

foreach (var proveedor in new[] { ProveedorLlm.Gemini, ProveedorLlm.Nvidia })
{
    Console.WriteLine($"\n=== Revisor con {proveedor} ===");
    var chatClient = ChatClientFactory.Crear(proveedor, proveedor == ProveedorLlm.Gemini ? apiKey : nvidiaApiKey);
    var revisor = new RevisorService(chatClient);
    var resultado = await revisor.EvaluarAfirmacionAsync(afirmacionDePrueba, resultadosBusqueda, habilitarRazonamiento: proveedor == ProveedorLlm.Nvidia);
    Console.WriteLine(resultado);
}

//Codigo para usar el ExtractorVision de Gemini, pagina por pagina. No Usado, se asume un PDF digitalizado correctamente o un MD correcto. 
/*
var extractorVision = new ExtractorVisionService(
    ChatClientFactory.Crear(ProveedorLlm.Gemini, apiKey));

var textoTranscripto = await extractorVision.TranscribirPaginaDesdeImagenAsync(
    "../../../pagina_test-45.png");

Console.WriteLine(textoTranscripto);
*/