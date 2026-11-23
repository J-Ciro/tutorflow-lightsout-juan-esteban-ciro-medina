using TutorFlow.Pruebas.Soporte;
using System.Text.Json;
using System.Text.RegularExpressions;
using TutorFlow.Core.Asistente;

namespace TutorFlow.Api.Tests.Dobles;

public delegate RespuestaLlm GuionLlm(SolicitudLlm solicitud, int numeroLlamada);

public sealed partial class ClienteLlmDoble : IClienteLlm
{
    public const string ParametroEstudianteId = "estudianteId";
    public static readonly UsoTokens UsoPorDefecto = new(100, 20);

    private readonly GuionLlm _guion;
    private readonly List<SolicitudLlm> _solicitudes = [];
    private readonly Lock _candado = new();

    public ClienteLlmDoble(GuionLlm guion)
    {
        _guion = guion;
    }

    public int Llamadas
    {
        get
        {
            lock (_candado)
            {
                return _solicitudes.Count;
            }
        }
    }

    public IReadOnlyList<SolicitudLlm> Solicitudes
    {
        get
        {
            lock (_candado)
            {
                return [.. _solicitudes];
            }
        }
    }

    public static ClienteLlmDoble Texto(string texto, UsoTokens? uso = null) =>
        new((_, _) => new RespuestaLlm(texto, [], uso ?? UsoPorDefecto, false));

    public static ClienteLlmDoble Rechaza() =>
        new((_, _) => new RespuestaLlm(null, [], UsoPorDefecto, true));

    public static ClienteLlmDoble Falla(Exception excepcion) =>
        new((_, _) => throw excepcion);

    public static ClienteLlmDoble QueNuncaResponde() =>
        new((_, _) => throw new TaskCanceledException("llm sin respuesta"));

    public static ClienteLlmDoble ConsultaDatos(string? estudianteIdParaCoordinador = null, Func<SolicitudLlm, string>? redactar = null, UsoTokens? uso = null)
    {
        var redactor = redactar ?? EcoDeHerramientas;
        var consumo = uso ?? UsoPorDefecto;
        return new ClienteLlmDoble((solicitud, n) => ConsultarOResponder(solicitud, n, estudianteIdParaCoordinador, redactor, consumo));
    }

    private static RespuestaLlm ConsultarOResponder(SolicitudLlm solicitud, int numero, string? estudianteId, Func<SolicitudLlm, string> redactor, UsoTokens consumo)
    {
        var llamadas = numero == 0 ? LlamarTodas(solicitud.Herramientas, estudianteId) : [];
        return llamadas.Count > 0
            ? new RespuestaLlm(null, llamadas, consumo, false)
            : new RespuestaLlm(redactor(solicitud), [], consumo, false);
    }

    public static ClienteLlmDoble PideEstudiante(string estudianteId, UsoTokens? uso = null) =>
        new((solicitud, n) =>
        {
            var herramienta = ElegirHerramienta(solicitud.Herramientas);
            return n == 0 && herramienta is not null
                ? new RespuestaLlm(null, [Llamada("pide-1", herramienta.Nombre, JsonSerializer.Serialize(new Dictionary<string, string> { [ParametroEstudianteId] = estudianteId }))], uso ?? UsoPorDefecto, false)
                : new RespuestaLlm("Sin datos", [], uso ?? UsoPorDefecto, false);
        });

    public static ClienteLlmDoble PideHerramientaSinParar() =>
        new((solicitud, n) => new RespuestaLlm(null, [Llamada($"sin-parar-{n}", solicitud.Herramientas[0].Nombre, "{}")], UsoPorDefecto, false));

    public static ClienteLlmDoble PideHerramientaVariasVecesEnUnaRespuesta(int veces) =>
        new((solicitud, _) => new RespuestaLlm(
            null,
            Enumerable.Range(0, veces).Select(i => Llamada($"lote-{i}", solicitud.Herramientas[0].Nombre, "{}")).ToList(),
            UsoPorDefecto,
            false));

    public static IReadOnlyList<string> ParametrosDeId(SolicitudLlm solicitud) =>
        solicitud.Herramientas
            .SelectMany(h => h.Parametros)
            .Select(p => p.Nombre)
            .Where(n => ParametroDeIdRegex().IsMatch(n))
            .ToList();

    public static string EcoDeHerramientas(SolicitudLlm solicitud) =>
        string.Join("\n", solicitud.Mensajes.Where(m => m.Rol == RolMensaje.Herramienta).Select(m => m.Contenido));

    public Task<RespuestaLlm> CompletarAsync(SolicitudLlm solicitud, CancellationToken cancellationToken)
    {
        int numero;
        lock (_candado)
        {
            numero = _solicitudes.Count;
            _solicitudes.Add(solicitud);
        }

        var respuesta = _guion(solicitud, numero);
        return Task.FromResult(respuesta with { Uso = respuesta.Uso with { Entrada = ConsumoRealista.Reportados(solicitud) } });
    }

    private static List<LlamadaHerramienta> LlamarTodas(IReadOnlyList<HerramientaLlm> herramientas, string? estudianteId) =>
        herramientas
            .Where(h => estudianteId is not null || !h.Parametros.Any(p => p.Nombre == ParametroEstudianteId))
            .Select(h => Llamada($"llamada-{h.Nombre}", h.Nombre, ArgumentosPara(h, estudianteId)))
            .ToList();

    private static string ArgumentosPara(HerramientaLlm herramienta, string? estudianteId) =>
        herramienta.Parametros.Any(p => p.Nombre == ParametroEstudianteId)
            ? $$"""{"{{ParametroEstudianteId}}":"{{estudianteId}}"}"""
            : "{}";

    private static HerramientaLlm? ElegirHerramienta(IReadOnlyList<HerramientaLlm> herramientas) =>
        herramientas.FirstOrDefault(h => h.Parametros.Any(p => p.Nombre == ParametroEstudianteId)) ?? (herramientas.Count > 0 ? herramientas[0] : null);

    private static LlamadaHerramienta Llamada(string id, string nombre, string argumentosJson) => new(id, nombre, argumentosJson);

    [GeneratedRegex("id$|estudiante|usuario|alumno", RegexOptions.IgnoreCase)]
    private static partial Regex ParametroDeIdRegex();
}
