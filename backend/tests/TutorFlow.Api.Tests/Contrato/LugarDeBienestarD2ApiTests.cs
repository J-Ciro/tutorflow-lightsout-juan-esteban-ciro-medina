using TutorFlow.Api.Tests.Dobles;

namespace TutorFlow.Api.Tests.Contrato;

public sealed class LugarDeBienestarD2ApiTests
{
    private const string Seccion1 = "bienestar_y_becas.md · Sección 1";
    private const string TextoSinElLugar = "El horario de atención de Bienestar es: lunes a viernes de 8:00 a 17:00; sábados de 8:00 a 12:00 (bienestar_y_becas.md · Sección 1).";

    [Fact]
    [Trait("Hallazgo", "D2")]
    [Trait("CA", "CA-01")]
    [Trait("ESC", "ESC-007")]
    public async Task D99_D2_ESC_007_el_horario_de_Bienestar_sin_el_lugar_en_el_texto_del_llm_incluye_edificio_B_piso_2()
    {
        using var fabrica = new FabricaApi(llm: GuionesLlm.SinHerramientas(_ => TextoSinElLugar), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", "¿Cuál es el horario de atención de Bienestar?");

        Assert.Equal(200, respuesta.Estado);
        Assert.Contains("edificio B, piso 2", respuesta.Respuesta, StringComparison.Ordinal);
        Assert.Contains("8:00", respuesta.Respuesta, StringComparison.Ordinal);
        Assert.Contains(Seccion1, respuesta.Citas);
    }
}
