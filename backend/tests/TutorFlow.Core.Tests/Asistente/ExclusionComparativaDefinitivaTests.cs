using TutorFlow.Core.Asistente;
using TutorFlow.Core.Autorizacion;
using TutorFlow.Core.Conocimiento;
using static TutorFlow.Core.Tests.Asistente.EscenarioAsistente;
using static TutorFlow.Core.Tests.Asistente.SalidasVerificadas;
using static TutorFlow.Core.Tests.Asistente.SeccionesDeReferencia;

namespace TutorFlow.Core.Tests.Asistente;

public sealed class ExclusionComparativaDefinitivaTests
{
    private const string PreguntaHabilitar = "¿Puedo habilitar Microeconomía?";
    private const string PreguntaAprobar = "¿Qué necesito para aprobar Estadística?";
    private const string PreguntaContabilidad = "¿Qué nota definitiva tengo en Contabilidad?";
    private const string PreguntaEstadistica = "¿Cuál es mi nota definitiva en Estadística?";
    private static readonly Cita DatosNotas = new(Textos.DocumentoDatosEstudiante, "Notas");

    public static TheoryData<string, string, string, string> ComparativasQueNoSeVerifican => new()
    {
        { "m", "EST-005", PreguntaHabilitar, "Microeconomía no es habilitable porque tu definitiva es menor a 2.0." },
        { "n", "EST-005", PreguntaAprobar, "Para aprobar Estadística necesitas una definitiva de al menos 3.0; hoy tienes 2.3." },
        { "o", "EST-005", PreguntaAprobar, "Para aprobar Estadística necesitas una definitiva de 3.0." },
        { "p", "EST-005", PreguntaAprobar, "Estadística se aprueba con definitiva ≥ 3.0 y la tuya es 2.3." },
        { "o-igual", "EST-005", PreguntaAprobar, "Estadística se aprueba con definitiva mayor o igual a 3.0 y la tuya es 2.3." },
        { "simbolo-doble", "EST-005", PreguntaAprobar, "Estadística se aprueba con definitiva >= 3.0 y la tuya es 2.3." },
        { "requiere", "EST-005", PreguntaAprobar, "Estadística requiere una definitiva de 3.0." },
        { "verbo-pegado-a-la-cifra", "EST-005", PreguntaAprobar, "Para aprobar Estadística necesitas 3.0 de definitiva." }
    };

    public static TheoryData<string, string, string, string> ComparativasQueSiSeVerifican => new()
    {
        { "q", "EST-006", PreguntaEstadistica, "Tu definitiva en Estadística es 4.0, mayor a 3.0." },
        { "s", "EST-004", PreguntaContabilidad, "Tu definitiva en Contabilidad, que no necesita habilitación, es 3.3." },
        { "para", "EST-004", PreguntaContabilidad, "La definitiva para Contabilidad es 3.3." }
    };

    [Theory]
    [MemberData(nameof(ComparativasQueNoSeVerifican))]
    [Trait("CA", "CA-06")]
    [Trait("ESC", "ESC-179")]
    [Trait("SEG", "SEG-10g")]
    [Trait("D", "D-38")]
    public async Task ESC_179_La_cifra_de_una_comparacion_o_de_un_requisito_no_se_verifica_como_definitiva(string caso, string id, string pregunta, string texto)
    {
        Assert.NotEmpty(caso);
        var escenario = Con(Fijo(texto), Indice(Articulo5, Articulo6));

        var resultado = await escenario.PreguntarAsync(new Usuario(Rol.Estudiante, id), pregunta);

        AfirmarRespondida(resultado, escenario, texto);
        Assert.Contains(DatosNotas, resultado.Respuesta!.Citas);
    }

    [Theory]
    [MemberData(nameof(ComparativasQueSiSeVerifican))]
    [Trait("CA", "CA-06")]
    [Trait("ESC", "ESC-179")]
    [Trait("SEG", "SEG-10g")]
    [Trait("D", "D-38")]
    public async Task ESC_179_Un_comparador_despues_o_lejos_de_la_cifra_asociada_no_excluye_la_verificacion(string caso, string id, string pregunta, string texto)
    {
        Assert.NotEmpty(caso);
        var escenario = Con(Fijo(texto), Indice(Articulo5, Articulo6));

        var resultado = await escenario.PreguntarAsync(new Usuario(Rol.Estudiante, id), pregunta);

        AfirmarEstadoNoVerificado(resultado, escenario, usoDatos: true);
    }

    [Fact]
    [Trait("CA", "CA-06")]
    [Trait("ESC", "ESC-179")]
    [Trait("SEG", "SEG-10c")]
    [Trait("D", "D-38")]
    public async Task ESC_179r_Una_cifra_comparativa_que_no_esta_en_las_fuentes_da_numero_no_verificado_antes_de_SEG_10g()
    {
        var escenario = Con(Fijo("Microeconomía no es habilitable porque tu definitiva es menor a 2.5."), Indice(Articulo5, Articulo6));

        var resultado = await escenario.PreguntarAsync(new Usuario(Rol.Estudiante, "EST-005"), PreguntaHabilitar);

        var respuesta = resultado.Respuesta!;
        Assert.Equal(Textos.NoVerificable, respuesta.Respuesta);
        Assert.Empty(respuesta.Citas);
        Assert.True(respuesta.UsoDatosEstudiante);
        Assert.Equal(ResultadoAuditoria.NumeroNoVerificado, resultado.Auditoria);
        Assert.Equal(ResultadoAuditoria.NumeroNoVerificado, Assert.Single(escenario.Auditoria.Eventos).Resultado);
    }
}
