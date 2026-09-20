using Microsoft.Extensions.AI;

public class RevisorService
{
    private readonly IChatClient _chatClient;

    public RevisorService(IChatClient chatClient)
    {
        _chatClient = chatClient;
    }

    public async Task<string> EvaluarAfirmacionAsync(
    string afirmacion,
    List<(ChunkVectorRecord Chunk, double Score)> evidencia,
    bool habilitarRazonamiento = false)
    {
        var contextoEvidencia = string.Join("\n\n---\n\n", evidencia.Select((e, i) =>
            $"[Fragmento {i + 1}] Fuente: {e.Chunk.Libro} — {e.Chunk.Seccion}\n{e.Chunk.Texto}"));

        var prompt = $"""
            Sos un revisor académico riguroso especializado en teología bíblica.
            Evaluá si la siguiente afirmación de una tesis está respaldada por la evidencia dada.

            AFIRMACIÓN A EVALUAR:
            "{afirmacion}"

            EVIDENCIA RECUPERADA:
            {contextoEvidencia}

            Respondé en este formato exacto:
            VEREDICTO: [Respaldada / Parcialmente respaldada / Sin respaldo / Contradicha]
            JUSTIFICACIÓN: (2-4 oraciones, citando el fragmento específico usado)
            CITA SUGERIDA: (libro y sección exactos de la fuente, si corresponde)

            No inventes información que no esté en los fragmentos. Si la evidencia no alcanza, decilo.
            """;

        var opciones = new ChatOptions();
        if (habilitarRazonamiento)
        {
            opciones.AdditionalProperties = new AdditionalPropertiesDictionary
            {
                ["chat_template_kwargs"] = new Dictionary<string, object> { ["enable_thinking"] = true }
            };
        }

        Console.WriteLine("Llamando a la API...");
        var respuesta = await _chatClient.GetResponseAsync(prompt, opciones);
        Console.WriteLine("Respuesta recibida.");
        return respuesta.Text;
    }
}