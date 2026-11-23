using TutorFlow.Api.Tests.Dobles;
using TutorFlow.Core.Asistente;

namespace TutorFlow.Api.Tests.Seguridad;

public sealed class DefinitivaSoloConLecturaDelProgramaApiTests
{
    private const string PreguntaDefinitiva = "¿Qué definitiva tiene Valentina Castro en Estadística?";
    private const string DatosProgramaAdm = "Datos del programa · ADM";

    [Fact]
    [Trait("CA", "CA-06")]
    [Trait("ESC", "ESC-179")]
    [Trait("RF", "SEG-10g")]
    public async Task ESC_179k_Una_definitiva_ajena_a_la_lectura_del_programa_da_numero_no_verificado()
    {
        using var fabrica = new FabricaApi(llm: ClienteLlmDoble.ConsultaDatos(redactar: _ => "La definitiva de Valentina Castro en Estadística es 3.2."), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarCoordinadorAsync("COORD-02", PreguntaDefinitiva);

        Assert.Equal(200, respuesta.Estado);
        Afirmaciones.SalidaDelContrato(respuesta);
        Assert.Equal(Textos.NoVerificable, respuesta.Respuesta);
        Assert.Empty(respuesta.Citas);
        Assert.True(respuesta.UsoDatosEstudiante);
        Assert.Equal(ResultadoAuditoria.NumeroNoVerificado, Assert.Single(fabrica.Auditoria.Eventos).Resultado);
        Assert.Equal(2, fabrica.Llm.Llamadas);
    }

    [Fact]
    [Trait("CA", "CA-06")]
    [Trait("ESC", "ESC-179")]
    [Trait("RF", "SEG-10g")]
    public async Task ESC_179l_La_definitiva_que_lista_el_resultado_del_programa_se_entrega_con_su_cita()
    {
        const string texto = "La definitiva de Valentina Castro en Estadística es 2.3.";
        using var fabrica = new FabricaApi(llm: ClienteLlmDoble.ConsultaDatos(redactar: _ => texto), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarCoordinadorAsync("COORD-02", PreguntaDefinitiva);

        Assert.Equal(200, respuesta.Estado);
        Assert.Equal(texto, respuesta.Respuesta);
        Assert.Contains(DatosProgramaAdm, respuesta.Citas);
        Assert.True(respuesta.UsoDatosEstudiante);
        Assert.Equal(ResultadoAuditoria.Respondida, Assert.Single(fabrica.Auditoria.Eventos).Resultado);
    }
}
