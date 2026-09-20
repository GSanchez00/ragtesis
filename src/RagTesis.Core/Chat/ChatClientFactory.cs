using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Mscc.GenerativeAI.Microsoft;
using OpenAI;
using System.ClientModel;

public static class ChatClientFactory
{
    public static IChatClient Crear(ProveedorLlm proveedor, string apiKey)
    {
        return proveedor switch
        {
            ProveedorLlm.Gemini => new GeminiChatClient(apiKey, "gemini-3.6-flash", (ILogger?)null),

            ProveedorLlm.Nvidia => new OpenAIClient(
                    new ApiKeyCredential(apiKey),
                    new OpenAIClientOptions { Endpoint = new Uri("https://integrate.api.nvidia.com/v1") })
                .GetChatClient("nvidia/nemotron-3-ultra-550b-a55b") // confirmar el nombre exacto en build.nvidia.com
                .AsIChatClient(),

            _ => throw new NotSupportedException($"Proveedor {proveedor} no soportado")
        };
    }
}