using Microsoft.Extensions.AI;

public class ConsultaTesisService
{
    private readonly IChatClient _chatClient;

    public ConsultaTesisService(IChatClient chatClient)
    {
        _chatClient = chatClient;
    }

    public async Task<string> ResponderPreguntaAsync(
        string pregunta,
        List<(ChunkVectorRecord Chunk, double Score)> contexto,
        CancellationToken cancellationToken = default)
    {
        var fragmentos = string.Join("\n\n---\n\n", contexto.Select((c, i) =>
            $"[Fragmento {i + 1}] Sección: {c.Chunk.Seccion}\n{c.Chunk.Texto}"));

        var prompt = $"""
            Sos un asistente que responde preguntas sobre una tesis de teología,
            usando únicamente el contenido de la propia tesis como contexto.

            PREGUNTA:
            "{pregunta}"

            FRAGMENTOS DE LA TESIS:
            {fragmentos}

            Respondé la pregunta basándote solo en los fragmentos dados, citando
            entre paréntesis la sección de la que sale cada dato. Si los fragmentos
            no alcanzan para responder, decilo explícitamente en vez de inventar
            contenido.
            """;

        // Task.Run saca la llamada del SynchronizationContext del caller (ej. el de
        // Blazor Server) para evitar un posible deadlock si el SDK de Gemini hace
        // alguna espera sincronica (.Result/.Wait()) internamente en el path de chat.
        var respuesta = await Task.Run(
            () => _chatClient.GetResponseAsync(prompt, cancellationToken: cancellationToken),
            cancellationToken);
        return respuesta.Text;
    }
}
