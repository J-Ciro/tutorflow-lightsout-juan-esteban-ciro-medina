using System.Text;
using System.Text.Json;
using TutorFlow.Api.Tests.Dobles;
using TutorFlow.Core.Asistente;

namespace TutorFlow.Evals;

public sealed record UsoEval(int Entrada, int Salida);

public sealed record ArtefactoCorrida(
    string Pregunta,
    UsuarioEval Usuario,
    int Corrida,
    IReadOnlyList<string> TextosCrudos,
    IReadOnlyList<string> SeccionesEntregadas,
    bool ConResumen,
    bool ConPrelectura,
    IReadOnlyList<UsoEval> Usage,
    string? Respuesta,
    IReadOnlyList<string> Citas,
    string TextoResumen = "",
    string TextoPrelectura = "",
    string? CorrelationId = null);

public sealed class GrabadorLlm(IClienteLlm interno) : IClienteLlm
{
    private readonly Lock _candado = new();
    private readonly List<(SolicitudLlm Solicitud, RespuestaLlm Respuesta)> _llamadas = [];

    public IReadOnlyList<(SolicitudLlm Solicitud, RespuestaLlm Respuesta)> Llamadas
    {
        get
        {
            lock (_candado)
            {
                return [.. _llamadas];
            }
        }
    }

    public async Task<RespuestaLlm> CompletarAsync(SolicitudLlm solicitud, CancellationToken cancellationToken)
    {
        var respuesta = await interno.CompletarAsync(solicitud, cancellationToken);
        lock (_candado)
        {
            _llamadas.Add((solicitud, respuesta));
        }

        return respuesta;
    }
}

public static class ArtefactosFaseV
{
    private const string FuenteDelResumen = Textos.FuenteResumenPropio;
    private const string PrefijoDeDatos = "Datos del";

    private static readonly JsonSerializerOptions OpcionesDeEscritura = new(Json.Opciones) { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    public static string Registrar(string carpeta, CasoEval caso, int corrida, GrabadorLlm? grabador, string cuerpo, string? correlationId, string? llave) =>
        Guardar(carpeta, $"{TablaFaseV.Pregunta(caso)}-{caso.Id}-corrida-{corrida}", Armar(caso, corrida, grabador, Json.LeerSalida(cuerpo), correlationId), llave);

    public static string Guardar(string carpeta, string nombre, ArtefactoCorrida artefacto, string? llave)
    {
        Directory.CreateDirectory(carpeta);
        var contenido = JsonSerializer.Serialize(artefacto, OpcionesDeEscritura);
        var ruta = Path.Combine(carpeta, $"{nombre}.json");
        File.WriteAllText(ruta, SinLaLlave(contenido, llave), Encoding.UTF8);
        return ruta;
    }

    public static ArtefactoCorrida Armar(CasoEval caso, int corrida, GrabadorLlm? grabador, SalidaEval? salida, string? correlationId = null)
    {
        var llamadas = grabador?.Llamadas ?? [];
        var bloques = BloquesDeLaPrimeraLlamada(llamadas);
        return new ArtefactoCorrida(
            caso.Pregunta,
            caso.Usuario,
            corrida,
            [.. llamadas.Select(l => l.Respuesta.Texto ?? string.Empty)],
            [.. bloques.Select(b => b.Fuente).Where(EsSeccionDeDocumento)],
            bloques.Any(b => b.Fuente == FuenteDelResumen),
            bloques.Any(b => b.Fuente.StartsWith(Textos.DocumentoDatosPrograma, StringComparison.Ordinal)),
            [.. llamadas.Select(l => new UsoEval(l.Respuesta.Uso.Entrada, l.Respuesta.Uso.Salida))],
            salida?.Respuesta,
            CitasDe(salida),
            CuerpoDe(bloques, b => b.Fuente == FuenteDelResumen),
            CuerpoDe(bloques, b => b.Fuente.StartsWith(Textos.DocumentoDatosPrograma, StringComparison.Ordinal)),
            correlationId);
    }

    private static IReadOnlyList<BloqueDelimitado> BloquesDeLaPrimeraLlamada(IReadOnlyList<(SolicitudLlm Solicitud, RespuestaLlm Respuesta)> llamadas) =>
        llamadas.Count == 0 ? [] : GuionesLlm.Bloques(llamadas[0].Solicitud);

    private static string CuerpoDe(IReadOnlyList<BloqueDelimitado> bloques, Func<BloqueDelimitado, bool> criterio) =>
        bloques.FirstOrDefault(criterio)?.Cuerpo ?? string.Empty;

    private static string[] CitasDe(SalidaEval? salida) =>
        [.. (salida?.Citas ?? []).Select(c => c.Texto)];

    private static bool EsSeccionDeDocumento(string fuente) =>
        fuente != FuenteDelResumen && !fuente.StartsWith(PrefijoDeDatos, StringComparison.Ordinal);

    private static string SinLaLlave(string contenido, string? llave) =>
        string.IsNullOrWhiteSpace(llave) ? contenido : contenido.Replace(llave, "[LLAVE]", StringComparison.Ordinal);
}
