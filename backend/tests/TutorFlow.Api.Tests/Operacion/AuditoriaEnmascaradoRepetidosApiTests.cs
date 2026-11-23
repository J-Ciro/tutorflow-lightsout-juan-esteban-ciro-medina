using TutorFlow.Api.Tests.Dobles;
using TutorFlow.Core.Asistente;

namespace TutorFlow.Api.Tests.Operacion;

public sealed class AuditoriaEnmascaradoRepetidosApiTests
{
    public static TheoryData<string, string, string, string> PreguntasConTercerosRepetidos => new()
    {
        { "estudiante", "EST-001", "¿Qué nota tiene Andrés Martínez? Insisto: Andrés Martínez, la de Andrés Martínez.", "¿Qué nota tiene [TERCERO]? Insisto: [TERCERO], la de [TERCERO]." },
        { "estudiante", "EST-001", "Notas de EST-002 y de EST-002", "Notas de [TERCERO] y de [TERCERO]" },
        { "estudiante", "EST-001", "Andrés Martínez (EST-002) y otra vez Andrés Martínez", "[TERCERO] ([TERCERO]) y otra vez [TERCERO]" },
        { "coordinador", "COORD-01", "Camilo Rojas y Camilo Rojas", "[TERCERO] y [TERCERO]" }
    };

    [Theory]
    [MemberData(nameof(PreguntasConTercerosRepetidos))]
    [Trait("CA", "CA-08")]
    [Trait("ESC", "ESC-118")]
    [Trait("HALLAZGO", "H-01")]
    public async Task Un_tercero_repetido_se_enmascara_en_todas_sus_ocurrencias_en_el_evento_de_un_403(string rol, string id, string pregunta, string esperada)
    {
        using var fabrica = new FabricaApi();

        var respuesta = await fabrica.PreguntarAsync(rol, id, pregunta);

        Assert.Equal(403, respuesta.Estado);
        var evento = Assert.Single(fabrica.Auditoria.Eventos);
        Assert.Equal(ResultadoAuditoria.Denegada, evento.Resultado);
        Assert.Equal(esperada, evento.Pregunta);
    }

    [Fact]
    [Trait("CA", "CA-08")]
    [Trait("ESC", "ESC-118")]
    [Trait("HALLAZGO", "H-01")]
    public async Task Un_tercero_repetido_se_enmascara_en_todas_sus_ocurrencias_aunque_la_identidad_sea_invalida()
    {
        using var fabrica = new FabricaApi();

        await fabrica.PreguntarEstudianteAsync("EST-001", "¿Cuál es la nota mínima para aprobar?");

        var respuesta = await fabrica.PreguntarAsync("estudiante", "EST-999", "Notas de Andrés Martínez y de Andrés Martínez");

        Assert.Equal(403, respuesta.Estado);
        Assert.Equal("Notas de [TERCERO] y de [TERCERO]", fabrica.Auditoria.Eventos[^1].Pregunta);
    }
}
