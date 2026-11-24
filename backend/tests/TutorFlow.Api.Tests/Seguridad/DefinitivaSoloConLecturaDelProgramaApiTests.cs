using TutorFlow.Api.Tests.Dobles;

namespace TutorFlow.Api.Tests.Seguridad;

public sealed class DefinitivaSoloConLecturaDelProgramaApiTests
{
    private const string PreguntaDefinitiva = "¿Qué dice el reglamento sobre la habilitación?";
    private const int LlamadasDeLecturaYRespuesta = 2;

    [Theory]
    [InlineData("k", "La definitiva de Valentina Castro en Estadística es 3.2.")]
    [InlineData("l", "La definitiva de Valentina Castro en Estadística es 2.3.")]
    [Trait("CA", "CA-17")]
    [Trait("ESC", "ESC-179")]
    [Trait("D", "D-98")]
    public async Task ESC_179kl_La_definitiva_de_una_persona_escrita_por_el_modelo_se_elimina_sin_reintento(string caso, string texto)
    {
        Assert.NotEmpty(caso);
        using var fabrica = new FabricaApi(llm: ClienteLlmDoble.ConsultaDatos(redactar: _ => texto), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarCoordinadorAsync("COORD-02", PreguntaDefinitiva);

        Afirmaciones.OracionConPersonaPodada(respuesta, fabrica, usoDatos: true);
        Assert.Equal(LlamadasDeLecturaYRespuesta, fabrica.Llm.Llamadas);
    }
}
