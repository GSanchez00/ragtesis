using Microsoft.Extensions.AI;

public class ExtractorVisionService
{
    private const string PromptTranscripcionAcademica = """
        Transcribí el texto de esta página académica a Markdown, respetando
        el orden de lectura si hay columnas, y uniendo palabras cortadas por
        guión de fin de línea. No resumas, transcribí completo.
        """;

    private readonly IChatClient _clienteConVision;

    public ExtractorVisionService(IChatClient clienteConVision)
    {
        _clienteConVision = clienteConVision;
    }

    public async Task<string> TranscribirPaginaDesdeImagenAsync(string rutaImagenPagina)
    {
        var contenidoImagen = await LeerBytesDeImagenAsync(rutaImagenPagina);
        var mensajeConImagen = ArmarMensajeDeTranscripcion(contenidoImagen);

        var respuesta = await _clienteConVision.GetResponseAsync([mensajeConImagen]);
        return respuesta.Text;
    }

    private static async Task<byte[]> LeerBytesDeImagenAsync(string rutaImagen)
    {
        if (!File.Exists(rutaImagen))
            throw new FileNotFoundException($"No se encontró la imagen: {rutaImagen}");

        return await File.ReadAllBytesAsync(rutaImagen);
    }

    private static ChatMessage ArmarMensajeDeTranscripcion(byte[] contenidoImagen)
    {
        return new ChatMessage(ChatRole.User, [
            new TextContent(PromptTranscripcionAcademica),
            new DataContent(contenidoImagen, "image/png")
        ]);
    }
}