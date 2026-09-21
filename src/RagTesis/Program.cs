using System.Reflection;
using Microsoft.Extensions.Configuration;
using RagTesis.Embeddings;

// ── Config ──────────────────────────────────────────────────────────────
var nombreColeccion = "mi-tesis";
var proveedor = args.Contains("--gemini") ? ProveedorLlm.Gemini : ProveedorLlm.Nvidia;
var pregunta = string.Join(' ', args.Where(a => a != "--gemini" && a != "--nvidia"));

var config = new ConfigurationBuilder()
    .AddUserSecrets(Assembly.GetExecutingAssembly())
    .Build();

var googleApiKey = config["GoogleApiKey"]
    ?? throw new Exception("Falta configurar GoogleApiKey con dotnet user-secrets");
var nvidiaApiKey = config["NvidiaApiKey"]
    ?? throw new Exception("Falta configurar NvidiaApiKey con dotnet user-secrets");

if (string.IsNullOrWhiteSpace(pregunta))
{
    Console.Write($"Pregunta para \"{nombreColeccion}\" (proveedor: {proveedor}): ");
    pregunta = Console.ReadLine() ?? "";
}

if (string.IsNullOrWhiteSpace(pregunta))
{
    Console.WriteLine("No escribiste ninguna pregunta.");
    return;
}

// ── Embedding de la pregunta (siempre Gemini) + búsqueda en Qdrant ───────
var embeddingService = new EmbeddingService(googleApiKey);
var vectorStoreSetup = new VectorStoreSetup(nombreColeccion);
await vectorStoreSetup.InicializarAsync();

Console.WriteLine("Generando embedding de la pregunta...");
var vectorPregunta = await embeddingService.GenerarEmbeddingAsync(pregunta);

Console.WriteLine($"Buscando fragmentos relevantes en \"{nombreColeccion}\"...");
var resultadosBusqueda = await vectorStoreSetup.BuscarAsync(vectorPregunta, top: 5);

if (resultadosBusqueda.Count == 0)
{
    Console.WriteLine($"No hay chunks guardados en \"{nombreColeccion}\" todavía.");
    return;
}

Console.WriteLine($"\n--- Top {resultadosBusqueda.Count} fragmentos ---");
foreach (var (chunk, score) in resultadosBusqueda)
{
    Console.WriteLine($"\n[Score: {score:F4}] {chunk.Seccion}");
    Console.WriteLine(chunk.Texto[..Math.Min(200, chunk.Texto.Length)] + "...");
}

// ── Respuesta del LLM ──────────────────────────────────────────────────
var apiKeyProveedor = proveedor == ProveedorLlm.Gemini ? googleApiKey : nvidiaApiKey;
var chatClient = ChatClientFactory.Crear(proveedor, apiKeyProveedor);
var consultaService = new ConsultaTesisService(chatClient);

Console.WriteLine($"\nGenerando respuesta con {proveedor} (puede tardar, especialmente NVIDIA)...");
var respuesta = await consultaService.ResponderPreguntaAsync(pregunta, resultadosBusqueda);

Console.WriteLine("\n--- Respuesta ---");
Console.WriteLine(respuesta);
