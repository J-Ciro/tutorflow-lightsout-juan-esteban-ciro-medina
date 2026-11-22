using System.Text.Json;
using TutorFlow.Core.Asistente;

namespace TutorFlow.Api.Tests;

public static class Afirmaciones
{
    public const string CitaNegativa = "reglamento_academico_2025.md · Artículo 22";

    private static readonly string[] PropiedadesDeSalida = ["respuesta", "citas", "usoDatosEstudiante", "tokens", "costoUsd"];
    private static readonly string[] PropiedadesDeCita = ["documento", "seccion"];
    private static readonly string[] FragmentosInternos = ["StackTrace", "   at ", "System.", "Exception", "Microsoft.", "TutorFlow.", "localhost:4000", ".cs:line"];

    public static void SalidaDelContrato(RespuestaApi respuesta)
    {
        var json = respuesta.Json;
        Assert.Equal(JsonValueKind.Object, json.ValueKind);
        Assert.Equal(PropiedadesDeSalida.Order(), json.EnumerateObject().Select(p => p.Name).Order());
        Assert.Equal(JsonValueKind.String, json.GetProperty("respuesta").ValueKind);
        Assert.Equal(JsonValueKind.Array, json.GetProperty("citas").ValueKind);
        Assert.True(json.GetProperty("usoDatosEstudiante").ValueKind is JsonValueKind.True or JsonValueKind.False);
        Assert.True(json.GetProperty("tokens").TryGetInt32(out var tokens) && tokens >= 0);
        Assert.True(json.GetProperty("costoUsd").TryGetDecimal(out var costo) && costo >= 0);
        foreach (var cita in json.GetProperty("citas").EnumerateArray())
        {
            Assert.Equal(PropiedadesDeCita.Order(), cita.EnumerateObject().Select(p => p.Name).Order());
            Assert.Equal(JsonValueKind.String, cita.GetProperty("documento").ValueKind);
            Assert.Equal(JsonValueKind.String, cita.GetProperty("seccion").ValueKind);
        }
    }

    public static void Negativa(RespuestaApi respuesta, string rol)
    {
        Assert.Equal(403, respuesta.Estado);
        SalidaDelContrato(respuesta);
        Assert.Equal(rol == "estudiante" ? Textos.NegativaEstudiante : Textos.NegativaCoordinador, respuesta.Respuesta);
        Assert.Equal([CitaNegativa], respuesta.Citas);
        Assert.False(respuesta.UsoDatosEstudiante);
        Assert.Equal(0, respuesta.Tokens);
        Assert.Equal(0m, respuesta.CostoUsd);
    }

    public static void ProblemDetails(RespuestaApi respuesta, int estado)
    {
        Assert.Equal(estado, respuesta.Estado);
        Assert.Contains("application/problem+json", respuesta.TipoContenido, StringComparison.OrdinalIgnoreCase);
        var json = respuesta.Json;
        Assert.Equal(estado, json.GetProperty("status").GetInt32());
        Assert.Equal(JsonValueKind.String, json.GetProperty("title").ValueKind);
        SinDetallesInternos(respuesta);
    }

    public static void SinDetallesInternos(RespuestaApi respuesta)
    {
        foreach (var fragmento in FragmentosInternos)
        {
            Assert.DoesNotContain(fragmento, respuesta.Texto, StringComparison.Ordinal);
        }
    }

    public static void SinEncabezadoCorsComodin(RespuestaApi respuesta)
    {
        var valor = respuesta.Encabezado("Access-Control-Allow-Origin");
        Assert.True(valor is null || (valor != "*" && !valor.Contains("evil.example", StringComparison.Ordinal)));
    }
}
