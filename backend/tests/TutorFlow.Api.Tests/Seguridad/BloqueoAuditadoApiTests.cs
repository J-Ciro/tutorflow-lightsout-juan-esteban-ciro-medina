using System.Text.Json;
using TutorFlow.Api.Tests.Dobles;

namespace TutorFlow.Api.Tests.Seguridad;

[Collection("Consola")]
public sealed class BloqueoAuditadoApiTests
{
    private const string Pregunta = "¿Cuál es mi promedio?";
    private const string Conexion = "10.0.0.5";
    private const int Limite = 600;
    private const int PreguntasDuranteElBloqueo = 50;
    private const string CampoBloqueo = "bloqueoSegundos";

    private static async Task<List<RespuestaApi>> PreguntarAsync(FabricaApi fabrica, int cantidad)
    {
        var respuestas = new List<RespuestaApi>();
        for (var i = 0; i < cantidad; i++)
        {
            respuestas.Add(await fabrica.PreguntarEstudianteAsync("EST-001", Pregunta, Conexion));
        }

        return respuestas;
    }

    private static async Task<IReadOnlyList<JsonElement>> CapturarEventosAsync(Func<Task> accion)
    {
        var original = Console.Out;
        using var escritor = new EscritorSincronizado();
        Console.SetOut(escritor);
        try
        {
            await accion();
        }
        finally
        {
            Console.SetOut(original);
        }

        return escritor.ToString()
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.TrimEnd('\r'))
            .Where(l => l.StartsWith('{') && l.Contains("\"correlationId\"", StringComparison.Ordinal))
            .Select(l => JsonElement.Parse(l))
            .ToList();
    }

    private static List<JsonElement> ConBloqueo(IEnumerable<JsonElement> eventos) =>
        [.. eventos.Where(e => e.TryGetProperty(CampoBloqueo, out _))];

    [Fact]
    [Trait("CA", "CA-13")]
    [Trait("CA", "CA-08")]
    [Trait("ESC", "ESC-142")]
    [Trait("D", "D-81")]
    public async Task ESC_142g_Solo_la_pregunta_601_de_cada_tanda_se_audita_con_bloqueo_segundos_y_las_50_del_segundo_10_no()
    {
        using var fabrica = new FabricaApi(auditoriaReal: true);
        List<RespuestaApi> primera = [];
        List<RespuestaApi> durante = [];
        List<RespuestaApi> segunda = [];

        var eventos = await CapturarEventosAsync(async () =>
        {
            primera = await PreguntarAsync(fabrica, Limite + 1);
            fabrica.Reloj.Avanzar(TimeSpan.FromSeconds(10));
            durante = await PreguntarAsync(fabrica, PreguntasDuranteElBloqueo);
            fabrica.Reloj.Avanzar(TimeSpan.FromSeconds(20));
            segunda = await PreguntarAsync(fabrica, Limite + 1);
        });

        Assert.Equal(429, primera[Limite].Estado);
        Assert.All(durante, r => Assert.Equal("20", r.Encabezado("Retry-After")));
        Assert.Equal("60", segunda[Limite].Encabezado("Retry-After"));
        var bloqueos = ConBloqueo(eventos);
        Assert.Equal([30, 60], bloqueos.Select(e => e.GetProperty(CampoBloqueo).GetInt32()));
        Assert.Equal(2 * (Limite + 1), eventos.Count);
    }

    [Fact]
    [Trait("CA", "CA-13")]
    [Trait("CA", "CA-08")]
    [Trait("ESC", "ESC-142")]
    [Trait("D", "D-81")]
    public async Task ESC_142g_El_evento_del_primer_429_es_el_de_la_peticion_bloqueada_y_el_resto_de_eventos_no_trae_bloqueo_segundos()
    {
        using var fabrica = new FabricaApi(auditoriaReal: true);
        List<RespuestaApi> tanda = [];

        var eventos = await CapturarEventosAsync(async () => tanda = await PreguntarAsync(fabrica, Limite + 1));

        var bloqueado = Assert.Single(ConBloqueo(eventos));
        Assert.Equal(tanda[Limite].Encabezado("X-Correlation-Id"), bloqueado.GetProperty("correlationId").GetString());
        Assert.Equal(Conexion, bloqueado.GetProperty("ipOrigen").GetString());
        Assert.Equal(Limite, eventos.Count(e => !e.TryGetProperty(CampoBloqueo, out _)));
    }
}
