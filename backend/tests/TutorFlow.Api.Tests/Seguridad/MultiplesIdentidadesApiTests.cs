using TutorFlow.Core.Asistente;
using TutorFlow.Core.Autorizacion;

namespace TutorFlow.Api.Tests.Seguridad;

public sealed class MultiplesIdentidadesApiTests
{
    private const string Pregunta = "¿Cuál es mi promedio?";
    private const string Conexion = "10.0.0.5";
    private static readonly string[] CuatroIds = ["EST-001", "EST-002", "EST-003", "EST-004"];

    private sealed class Sesion(FabricaApi fabrica)
    {
        private readonly DateTimeOffset _origen = fabrica.Reloj.AhoraUtc;

        public FabricaApi Fabrica { get; } = fabrica;

        public async Task<RespuestaApi> PreguntarEn(double minutos, string id, string conexion = Conexion)
        {
            Fabrica.Reloj.Fijar(_origen + TimeSpan.FromMinutes(minutos));
            return await Fabrica.PreguntarAsync("estudiante", id, Pregunta, conexion);
        }

        public async Task RegistrarTresIdentidades()
        {
            foreach (var (id, minuto) in new[] { ("EST-001", 0), ("EST-002", 1), ("EST-003", 2) })
            {
                Assert.Equal(200, (await PreguntarEn(minuto, id)).Estado);
            }
        }
    }

    [Fact]
    [Trait("CA", "CA-13")]
    [Trait("ESC", "ESC-140")]
    [Trait("SEG", "SEG-16")]
    public async Task ESC_140_La_cuarta_identidad_responde_200_y_emite_un_evento_con_las_cuatro_sin_campo_bloqueada()
    {
        using var fabrica = new FabricaApi();
        var sesion = new Sesion(fabrica);
        var respuestas = new List<RespuestaApi>();

        foreach (var (id, minuto) in CuatroIds.Select((id, i) => (id, i)))
        {
            respuestas.Add(await sesion.PreguntarEn(minuto, id));
        }

        Assert.All(respuestas, r => Assert.Equal(200, r.Estado));
        var evento = Assert.Single(fabrica.Auditoria.MultiplesIdentidades);
        Assert.Equal(Conexion, evento.IpOrigen);
        Assert.Equal(CuatroIds, evento.UsuarioIds);
        Assert.Null(typeof(EventoMultiplesIdentidades).GetProperty("Bloqueada"));
    }

    [Fact]
    [Trait("CA", "CA-13")]
    [Trait("ESC", "ESC-141")]
    [Trait("SEG", "SEG-16")]
    public async Task ESC_141a_Una_identidad_nueva_desde_otra_conexion_responde_200_y_no_emite_evento()
    {
        using var fabrica = new FabricaApi();
        var sesion = new Sesion(fabrica);
        await sesion.RegistrarTresIdentidades();

        var otraIp = await sesion.PreguntarEn(3, "EST-004", "10.0.0.6");

        Assert.Equal(200, otraIp.Estado);
        Assert.Empty(fabrica.Auditoria.MultiplesIdentidades);
    }

    [Fact]
    [Trait("CA", "CA-13")]
    [Trait("ESC", "ESC-141")]
    [Trait("SEG", "SEG-03")]
    [Trait("SEG", "SEG-16")]
    public async Task ESC_141b_Una_identidad_inexistente_es_403_identidad_invalida_nunca_429_y_no_cuenta()
    {
        using var fabrica = new FabricaApi();
        var sesion = new Sesion(fabrica);
        await sesion.RegistrarTresIdentidades();

        var inexistente = await sesion.PreguntarEn(3, "EST-999");

        Afirmaciones.Negativa(inexistente, "estudiante");
        Assert.Equal(MotivoDenegacion.IdentidadInvalida, fabrica.Auditoria.Eventos[^1].MotivoDenegacion);
        Assert.Empty(fabrica.Auditoria.MultiplesIdentidades);
    }

    [Fact]
    [Trait("CA", "CA-13")]
    [Trait("ESC", "ESC-097")]
    [Trait("SEG", "SEG-16")]
    public async Task ESC_097_Se_emite_un_evento_por_identidad_nueva_y_ninguno_por_repeticion()
    {
        using var fabrica = new FabricaApi();
        var sesion = new Sesion(fabrica);
        var respuestas = new List<RespuestaApi>();

        foreach (var (id, minuto) in new[] { ("EST-001", 0), ("EST-002", 1), ("EST-003", 2), ("EST-004", 3), ("EST-005", 4), ("EST-004", 5) })
        {
            respuestas.Add(await sesion.PreguntarEn(minuto, id));
        }

        Assert.All(respuestas, r => Assert.Equal(200, r.Estado));
        var eventos = fabrica.Auditoria.MultiplesIdentidades;
        Assert.Equal(2, eventos.Count);
        Assert.Equal(CuatroIds, eventos[0].UsuarioIds);
        Assert.Equal([.. CuatroIds, "EST-005"], eventos[1].UsuarioIds);
    }
}
