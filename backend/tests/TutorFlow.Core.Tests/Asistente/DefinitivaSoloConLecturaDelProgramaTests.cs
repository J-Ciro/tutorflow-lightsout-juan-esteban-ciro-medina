using TutorFlow.Core.Asistente;
using TutorFlow.Core.Autorizacion;
using TutorFlow.Core.Conocimiento;
using static TutorFlow.Core.Tests.Asistente.EscenarioAsistente;
using static TutorFlow.Core.Tests.Asistente.SalidasVerificadas;
using static TutorFlow.Core.Tests.Asistente.SeccionesDeReferencia;

namespace TutorFlow.Core.Tests.Asistente;

public sealed class DefinitivaSoloConLecturaDelProgramaTests
{
    private const string PreguntaDefinitiva = "¿Qué definitiva tiene Valentina Castro en Estadística?";
    private static readonly Cita DatosProgramaAdm = new(Textos.DocumentoDatosPrograma, "ADM");
    private static readonly Usuario Coord02 = new(Rol.Coordinador, "COORD-02");

    private static EscenarioAsistente ConSoloElPrograma(string texto) =>
        Con(
            (_, llamada) => llamada == 0 ? Herramienta(HerramientasAsistente.Programa) : Texto(texto),
            Indice(Articulo5, Articulo6, Articulo20));

    [Fact]
    [Trait("CA", "CA-06")]
    [Trait("ESC", "ESC-179")]
    [Trait("SEG", "SEG-10g")]
    public async Task ESC_179k_La_definitiva_que_no_esta_en_el_resultado_del_programa_da_numero_no_verificado()
    {
        var escenario = ConSoloElPrograma("La definitiva de Valentina Castro en Estadística es 3.2.");

        var resultado = await escenario.PreguntarAsync(Coord02, PreguntaDefinitiva);

        var respuesta = resultado.Respuesta!;
        Assert.Equal(Textos.NoVerificable, respuesta.Respuesta);
        Assert.Empty(respuesta.Citas);
        Assert.True(respuesta.UsoDatosEstudiante);
        Assert.Equal(ResultadoAuditoria.NumeroNoVerificado, resultado.Auditoria);
        Assert.Equal(ResultadoAuditoria.NumeroNoVerificado, Assert.Single(escenario.Auditoria.Eventos).Resultado);
    }

    [Fact]
    [Trait("CA", "CA-06")]
    [Trait("ESC", "ESC-179")]
    [Trait("SEG", "SEG-10g")]
    public async Task ESC_179l_La_definitiva_que_lista_el_resultado_del_programa_se_entrega_con_la_cita_del_programa()
    {
        const string texto = "La definitiva de Valentina Castro en Estadística es 2.3.";
        var escenario = ConSoloElPrograma(texto);

        var resultado = await escenario.PreguntarAsync(Coord02, PreguntaDefinitiva);

        AfirmarRespondida(resultado, escenario, texto);
        Assert.Contains(DatosProgramaAdm, resultado.Respuesta!.Citas);
    }

    [Fact]
    [Trait("CA", "CA-06")]
    [Trait("ESC", "ESC-179")]
    [Trait("SEG", "SEG-10g")]
    public async Task ESC_179_El_resultado_de_la_herramienta_de_programa_lista_las_perdidas_de_la_estudiante_con_su_definitiva()
    {
        var escenario = ConSoloElPrograma("La definitiva de Valentina Castro en Estadística es 2.3.");

        await escenario.PreguntarAsync(Coord02, PreguntaDefinitiva);

        var resultadoDeLaHerramienta = escenario.Llm.TextoDeLlamada(1);
        Assert.Contains("Valentina Castro (ID EST-005)", resultadoDeLaHerramienta, StringComparison.Ordinal);
        Assert.Contains("Microeconomía (1.8)", resultadoDeLaHerramienta, StringComparison.Ordinal);
        Assert.Contains("Estadística (2.3)", resultadoDeLaHerramienta, StringComparison.Ordinal);
        Assert.DoesNotContain("Contabilidad (3.2)", resultadoDeLaHerramienta, StringComparison.Ordinal);
    }
}
