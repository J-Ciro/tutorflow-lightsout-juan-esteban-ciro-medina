using TutorFlow.Pruebas.Soporte;
using System.Text.Json;

namespace TutorFlow.Api.Tests.Dobles;

public static class ConsumoRealistaHttp
{
    public static int Estimados(PeticionLlm peticion)
    {
        using var cuerpo = JsonDocument.Parse(peticion.Cuerpo);
        var raiz = cuerpo.RootElement;
        var caracteres = raiz.GetProperty("messages").EnumerateArray().Sum(m => m.TryGetProperty("content", out var contenido) ? CaracteresDe(contenido) : 0);
        return ConsumoRealista.EstimadosDeCaracteres(raiz.TryGetProperty("system", out var sistema) ? caracteres + CaracteresDe(sistema) : caracteres);
    }

    public static int Reportados(PeticionLlm peticion) => ConsumoRealista.Reportados(Estimados(peticion));

    private static int CaracteresDe(JsonElement contenido) =>
        contenido.ValueKind switch
        {
            JsonValueKind.String => contenido.GetString()!.Length,
            JsonValueKind.Array => contenido.EnumerateArray().Sum(CaracteresDe),
            JsonValueKind.Object => contenido.TryGetProperty("text", out var texto) ? CaracteresDe(texto)
                : contenido.TryGetProperty("content", out var anidado) ? CaracteresDe(anidado) : 0,
            _ => 0
        };
}
