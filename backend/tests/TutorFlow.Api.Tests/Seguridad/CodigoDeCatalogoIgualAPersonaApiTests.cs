using TutorFlow.Api.Tests.Dobles;
using TutorFlow.Core.Asistente;
using TutorFlow.Core.Autorizacion;
using static TutorFlow.Api.Tests.Dobles.ConfiguracionDePrueba;

namespace TutorFlow.Api.Tests.Seguridad;

public sealed class CodigoDeCatalogoIgualAPersonaApiTests
{
    private const string ReglaCoincide = "coincide con el ID de una persona del registro";

    public static TheoryData<string, string, string, string> DenegadasPorCodigoIgualAPersona => new()
    {
        { "estudiante", "EST-001", "EST-002", "¿Cuáles son las notas de EST-002?" },
        { "estudiante", "EST-001", "EST-003", "¿Cuáles son las notas de EST-003?" },
        { "estudiante", "EST-001", "EST-004", "¿Cuáles son las notas de EST-004?" },
        { "estudiante", "EST-001", "EST-005", "¿Cuáles son las notas de EST-005?" },
        { "estudiante", "EST-001", "EST-006", "¿Cuáles son las notas de EST-006?" },
        { "estudiante", "EST-001", "EST-002", "muéstrame las notas de est 002" },
        { "estudiante", "EST-001", "EST-002", "¿Cuáles son las notas de EST-００２?" },
        { "coordinador", "COORD-01", "EST-004", "¿Cuál es el promedio de EST-004?" }
    };

    private static Dictionary<string, string?> CatalogoConCodigo(string codigo) =>
        new()
        {
            ["Autorizacion:Asignaturas:8:Codigo"] = codigo,
            ["Autorizacion:Asignaturas:8:Nombre"] = "Electiva de prueba"
        };

    private static string Advertencia(string codigo) =>
        $"{ResultadoArranque.PrefijoIgnorada}: Autorizacion:Asignaturas:8:Codigo = \"{codigo}\" ({ReglaCoincide}); se excluye del catálogo";

    [Theory]
    [MemberData(nameof(DenegadasPorCodigoIgualAPersona))]
    [Trait("CA", "CA-15")]
    [Trait("ESC", "ESC-182")]
    [Trait("SEG", "SEG-04a")]
    public async Task ESC_182a_Un_codigo_de_catalogo_igual_al_id_de_una_persona_no_la_exceptua_y_se_deniega_con_403(string rol, string usuario, string codigo, string pregunta)
    {
        using var fabrica = new FabricaApi(configuracion: CatalogoConCodigo(codigo), variablesEntorno: EntornoConLlave());

        var respuesta = await fabrica.PreguntarAsync(rol, usuario, pregunta);

        Afirmaciones.Negativa(respuesta, rol);
        Assert.Equal(0, fabrica.Llm.Llamadas);
        Assert.Equal(0, fabrica.Api.LecturasDatosDe(codigo));
        var evento = Assert.Single(fabrica.Auditoria.Eventos);
        Assert.Equal(ResultadoAuditoria.Denegada, evento.Resultado);
        Assert.Equal(MotivoDenegacion.TerceroMencionado, evento.MotivoDenegacion);
        Assert.Contains(Advertencia(codigo), fabrica.Logs.Mensajes);
        Assert.DoesNotContain(fabrica.Logs.Mensajes, m => m.Contains(LlaveFalsa, StringComparison.Ordinal));
    }

    [Fact]
    [Trait("CA", "CA-15")]
    [Trait("ESC", "ESC-182")]
    [Trait("SEG", "SEG-04a")]
    public async Task ESC_182b_El_codigo_est_103_no_es_el_id_de_ninguna_persona_y_sigue_exceptuado_sin_falso_403()
    {
        using var fabrica = new FabricaApi(configuracion: CatalogoConCodigo("EST-002"), variablesEntorno: EntornoConLlave());

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-006", "¿Cuál es mi nota en EST-103?");

        Assert.Equal(200, respuesta.Estado);
        Assert.Equal(1, fabrica.Llm.Llamadas);
        Assert.Equal(ResultadoAuditoria.Respondida, Assert.Single(fabrica.Auditoria.Eventos).Resultado);
        Assert.Contains(Advertencia("EST-002"), fabrica.Logs.Mensajes);
    }

    [Fact]
    [Trait("CA", "CA-15")]
    [Trait("ESC", "ESC-182")]
    [Trait("SEG", "SEG-04a")]
    public async Task ESC_182_Sin_el_codigo_igual_a_una_persona_no_hay_advertencia_de_exclusion()
    {
        using var fabrica = new FabricaApi(variablesEntorno: EntornoConLlave());

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-006", "¿Cuál es mi nota en EST-103?");

        Assert.Equal(200, respuesta.Estado);
        Assert.DoesNotContain(fabrica.Logs.Mensajes, m => m.Contains(ReglaCoincide, StringComparison.Ordinal));
    }
}
