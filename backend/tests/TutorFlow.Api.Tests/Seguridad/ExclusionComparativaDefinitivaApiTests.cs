using TutorFlow.Api.Tests.Dobles;
using TutorFlow.Core.Asistente;
using TutorFlow.Pruebas.Soporte;

namespace TutorFlow.Api.Tests.Seguridad;

public sealed class ExclusionComparativaDefinitivaApiTests
{
    private const string PreguntaHabilitar = "¿Puedo habilitar Microeconomía?";
    private const string PreguntaAprobar = "¿Qué necesito para aprobar Estadística?";
    private const string PreguntaContabilidad = "¿Qué nota definitiva tengo en Contabilidad?";
    private const string PreguntaEstadistica = "¿Cuál es mi nota definitiva en Estadística?";
    private const string Notas = "Datos del estudiante · Notas";

    public static TheoryData<string, string, string, string> ComparativasQueNoSeVerifican => new()
    {
        { "m", "EST-005", PreguntaHabilitar, "Microeconomía no es habilitable porque tu definitiva es menor a 2.0." },
        { "n", "EST-005", PreguntaAprobar, "Para aprobar Estadística necesitas una definitiva de al menos 3.0; hoy tienes 2.3." },
        { "o", "EST-005", PreguntaAprobar, "Para aprobar Estadística necesitas una definitiva de 3.0." },
        { "p", "EST-005", PreguntaAprobar, "Estadística se aprueba con definitiva ≥ 3.0 y la tuya es 2.3." }
    };

    public static TheoryData<string, string, string, string> ComparativasQueSiSeVerifican => new()
    {
        { "q", "EST-006", PreguntaEstadistica, "Tu definitiva en Estadística es 4.0, mayor a 3.0." },
        { "s", "EST-004", PreguntaContabilidad, "Tu definitiva en Contabilidad, que no necesita habilitación, es 3.3." }
    };

    [Theory]
    [MemberData(nameof(ComparativasQueNoSeVerifican))]
    [Trait("CA", "CA-06")]
    [Trait("ESC", "ESC-179")]
    [Trait("RF", "SEG-10g")]
    public async Task ESC_179_La_cifra_de_una_comparacion_o_de_un_requisito_no_se_verifica_como_definitiva(string caso, string id, string pregunta, string texto)
    {
        using var fabrica = new FabricaApi(llm: ClienteLlmDoble.Texto(texto), conocimiento: new IndiceAprobacionYHabilitacion());

        var respuesta = await fabrica.PreguntarEstudianteAsync(id, pregunta);

        Assert.True(respuesta.Estado == 200, caso);
        Afirmaciones.SalidaDelContrato(respuesta);
        Assert.Equal(texto, respuesta.Respuesta);
        Assert.Contains(Notas, respuesta.Citas);
        Assert.True(respuesta.UsoDatosEstudiante, caso);
        Assert.Equal(ResultadoAuditoria.Respondida, Assert.Single(fabrica.Auditoria.Eventos).Resultado);
    }

    [Theory]
    [MemberData(nameof(ComparativasQueSiSeVerifican))]
    [Trait("CA", "CA-06")]
    [Trait("ESC", "ESC-179")]
    [Trait("RF", "SEG-10g")]
    public async Task ESC_179_Un_comparador_despues_o_lejos_de_la_cifra_asociada_no_excluye_la_verificacion(string caso, string id, string pregunta, string texto)
    {
        using var fabrica = new FabricaApi(llm: ClienteLlmDoble.Texto(texto), conocimiento: new IndiceAprobacionYHabilitacion());

        var respuesta = await fabrica.PreguntarEstudianteAsync(id, pregunta);

        Assert.True(respuesta.Estado == 200, caso);
        Afirmaciones.SalidaDelContrato(respuesta);
        Assert.Equal(Textos.NoVerificable, respuesta.Respuesta);
        Assert.Empty(respuesta.Citas);
        Assert.True(respuesta.UsoDatosEstudiante, caso);
        Assert.Equal(ResultadosNuevosDeAuditoria.EstadoNoVerificado, Assert.Single(fabrica.Auditoria.Eventos).Resultado);
    }

    [Fact]
    [Trait("CA", "CA-06")]
    [Trait("ESC", "ESC-179")]
    [Trait("RF", "SEG-10c")]
    public async Task ESC_179r_Una_cifra_comparativa_que_no_esta_en_las_fuentes_da_numero_no_verificado()
    {
        using var fabrica = new FabricaApi(
            llm: ClienteLlmDoble.Texto("Microeconomía no es habilitable porque tu definitiva es menor a 2.5."),
            conocimiento: new IndiceAprobacionYHabilitacion());

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-005", PreguntaHabilitar);

        Assert.Equal(200, respuesta.Estado);
        Afirmaciones.SalidaDelContrato(respuesta);
        Assert.Equal(Textos.NoVerificable, respuesta.Respuesta);
        Assert.Empty(respuesta.Citas);
        Assert.True(respuesta.UsoDatosEstudiante);
        Assert.Equal(ResultadoAuditoria.NumeroNoVerificado, Assert.Single(fabrica.Auditoria.Eventos).Resultado);
    }
}
