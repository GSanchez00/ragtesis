using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Mscc.GenerativeAI.Microsoft;
using OpenAI;
using System.ClientModel;

public static class ChatClientFactory
{
    private const string ModeloGemini = "gemini-3.6-flash";
    private const string ModeloGeminiFlashLite = "gemini-3.5-flash-lite";
    private const string ModeloNvidia = "nvidia/nemotron-3-ultra-550b-a55b"; // confirmar el nombre exacto en build.nvidia.com

    public static IChatClient Crear(ProveedorLlm proveedor, string apiKey)
    {
        return proveedor switch
        {
            ProveedorLlm.Gemini => new GeminiChatClient(apiKey, ModeloGemini, (ILogger?)null),

            ProveedorLlm.GeminiFlashLite => new GeminiChatClient(apiKey, ModeloGeminiFlashLite, (ILogger?)null),

            ProveedorLlm.Nvidia => new OpenAIClient(
                    new ApiKeyCredential(apiKey),
                    new OpenAIClientOptions { Endpoint = new Uri("https://integrate.api.nvidia.com/v1") })
                .GetChatClient(ModeloNvidia)
                .AsIChatClient(),

            _ => throw new NotSupportedException($"Proveedor {proveedor} no soportado")
        };
    }

    public static string ModeloDe(ProveedorLlm proveedor) => proveedor switch
    {
        ProveedorLlm.Gemini => ModeloGemini,
        ProveedorLlm.GeminiFlashLite => ModeloGeminiFlashLite,
        ProveedorLlm.Nvidia => ModeloNvidia,
        _ => throw new NotSupportedException($"Proveedor {proveedor} no soportado")
    };
}