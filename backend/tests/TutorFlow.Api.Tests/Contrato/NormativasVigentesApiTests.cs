using TutorFlow.Api.Tests.Dobles;
using TutorFlow.Core.Asistente;

namespace TutorFlow.Api.Tests.Contrato;

public sealed class NormativasVigentesApiTests
{
    private const string Articulo20 = "reglamento_academico_2025.md · Artículo 20";
    private const string Articulo22 = "reglamento_academico_2025.md · Artículo 22";
    private const string Tutorias = "bienestar_y_becas.md · Sección 2";

    public static TheoryData<string, string, string, string, string, string[]> RespuestasNormativas => new()
    {
        { "ESC-017 sin cita escrita", "EST-004", "¿Cuándo queda un estudiante en prueba académica?", "Queda en prueba académica el estudiante cuyo promedio ponderado del periodo sea inferior a 3.0.", Articulo20, ["3.0", "promedio"] },
        { "ESC-017 con cita escrita", "EST-004", "¿Cuándo queda un estudiante en prueba académica?", $"Queda en prueba académica el estudiante cuyo promedio ponderado del periodo sea inferior a 3.0 ({Articulo20}).", Articulo20, ["3.0", "promedio"] },
        { "ESC-019 sin cita escrita", "EST-005", "¿Cuándo hay tutorías académicas?", "Las tutorías son martes y jueves de 14:00 a 18:00 en la biblioteca.", Tutorias, ["martes", "jueves", "14:00", "18:00", "biblioteca"] },
        { "ESC-019 con cita escrita", "EST-005", "¿Cuándo hay tutorías académicas?", $"Las tutorías son martes y jueves de 14:00 a 18:00 en la biblioteca ({Tutorias}).", Tutorias, ["martes", "jueves", "14:00", "18:00", "biblioteca"] }
    };

    [Theory]
    [MemberData(nameof(RespuestasNormativas))]
    [Trait("CA", "CA-14")]
    [Trait("ESC", "ESC-017")]
    [Trait("ESC", "ESC-019")]
    [Trait("RN", "RN-11")]
    public async Task ESC_017_019_La_respuesta_normativa_conserva_su_contenido_y_cita_la_seccion_vigente(string caso, string usuario, string pregunta, string texto, string cita, string[] contiene)
    {
        Assert.NotEmpty(caso);
        using var fabrica = new FabricaApi(llm: GuionesLlm.SinHerramientas(_ => texto), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarEstudianteAsync(usuario, pregunta);

        Assert.Equal(200, respuesta.Estado);
        Assert.All(contiene, c => Assert.Contains(c, respuesta.Respuesta, StringComparison.OrdinalIgnoreCase));
        Assert.Contains(cita, respuesta.Citas);
        Assert.DoesNotContain(respuesta.Citas, c => c.StartsWith("reglamento_academico_2023.md", StringComparison.Ordinal));
        Assert.False(respuesta.UsoDatosEstudiante);
        Assert.Equal(ResultadoAuditoria.Respondida, Assert.Single(fabrica.Auditoria.Eventos).Resultado);
    }

    [Fact]
    [Trait("CA", "CA-14")]
    [Trait("ESC", "ESC-018")]
    [Trait("SEG", "SEG-01")]
    public async Task ESC_018_Quien_puede_consultar_la_informacion_de_un_estudiante_cita_el_Articulo_22_vigente()
    {
        const string texto = $"Solo pueden consultarla el propio estudiante y el coordinador de su programa ({Articulo22}).";
        using var fabrica = new FabricaApi(llm: GuionesLlm.SinHerramientas(_ => texto), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarCoordinadorAsync("COORD-01", "¿Quién puede consultar la información académica de un estudiante?");

        Assert.Equal(200, respuesta.Estado);
        Assert.Contains("coordinador", respuesta.Respuesta, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("estudiante", respuesta.Respuesta, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(Articulo22, respuesta.Citas);
        Assert.DoesNotContain(respuesta.Citas, c => c.StartsWith("reglamento_academico_2023.md", StringComparison.Ordinal));
    }
}
