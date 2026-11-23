using TutorFlow.Api.Tests.Dobles;
using TutorFlow.Core.Asistente;
using TutorFlow.Pruebas.Soporte;

namespace TutorFlow.Api.Tests.Contrato;

public sealed class NormativaDelCoordinadorApiTests
{
    private const string Pregunta = "¿Quién puede consultar la información académica de un estudiante?";
    private const string FuenteDelPrograma = "fuente=\"Datos del programa";
    private const string TextoSinReferencia = "Pueden consultarla el propio estudiante y el coordinador de su programa.";

    public static TheoryData<string, string> RespuestasDelDoble => new()
    {
        { "a", $"{TextoSinReferencia[..^1]} (reglamento_academico_2025.md · Artículo 22)." },
        { "b", TextoSinReferencia }
    };

    [Theory]
    [MemberData(nameof(RespuestasDelDoble))]
    [Trait("CA", "CA-14")]
    [Trait("CA", "CA-19")]
    [Trait("ESC", "ESC-018")]
    [Trait("D", "D-66")]
    public async Task ESC_018ab_La_pregunta_normativa_del_coordinador_cita_exactamente_el_articulo_22_sin_prelectura_y_con_uso_false(string caso, string texto)
    {
        Assert.NotEmpty(caso);
        using var fabrica = new FabricaApi(llm: GuionesLlm.SinHerramientas(_ => texto), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarCoordinadorAsync("COORD-01", Pregunta);

        Assert.Equal(200, respuesta.Estado);
        Assert.Contains("coordinador", respuesta.Respuesta, StringComparison.Ordinal);
        Assert.Equal(["reglamento_academico_2025.md · Artículo 22"], respuesta.Citas);
        Assert.False(respuesta.UsoDatosEstudiante);
        Assert.All(fabrica.Llm.Solicitudes.SelectMany(s => s.Mensajes), m => Assert.DoesNotContain(FuenteDelPrograma, m.Contenido, StringComparison.Ordinal));
        Assert.DoesNotContain(fabrica.Api.Llamadas, l => l.StartsWith("ObtenerNotas", StringComparison.Ordinal) || l.StartsWith("ObtenerAsistencia", StringComparison.Ordinal));
    }
}
