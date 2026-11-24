using TutorFlow.Core.Asistente;
using TutorFlow.Core.Autorizacion;
using TutorFlow.Core.Conocimiento;
using static TutorFlow.Core.Tests.Asistente.EscenarioAsistente;
using static TutorFlow.Core.Tests.Asistente.SeccionesDeReferencia;

namespace TutorFlow.Core.Tests.Asistente;

public sealed class DesgloseDelCoordinadorD96Tests
{
    private static readonly Usuario Coord01 = new(Rol.Coordinador, "COORD-01");
    private static readonly string[] EstudiantesDeAdm = ["EST-004", "EST-005", "EST-006"];

    private static IndiceFijo IndiceDeFaltas() => Indice(Articulo11, Articulo12);

    [Fact]
    [Trait("CA", "CA-27")]
    [Trait("ESC", "ESC-189")]
    [Trait("SEG", "SEG-02")]
    [Trait("D", "D-96")]
    public async Task ESC_189_n1_Con_un_solo_estudiante_nombrado_se_lee_solo_a_ese_estudiante_sin_prelectura()
    {
        var escenario = Con(Fijo("Este es el detalle de Andrés Martínez."), IndiceDeFaltas());

        var resultado = await escenario.PreguntarAsync(Coord01, "¿Cuántas faltas tiene Andrés Martínez?");

        Assert.Contains("Total: 15 faltas", resultado.Respuesta!.Respuesta, StringComparison.Ordinal);
        Assert.NotEmpty(escenario.Api.LecturasNotas);
        Assert.NotEmpty(escenario.Api.LecturasAsistencia);
        Assert.All(escenario.Api.LecturasNotas, id => Assert.Equal("EST-002", id));
        Assert.All(escenario.Api.LecturasAsistencia, id => Assert.Equal("EST-002", id));
        Assert.DoesNotContain(resultado.Respuesta.Citas, c => c.Documento == Textos.DocumentoDatosPrograma);
        Assert.Contains(new Cita("Datos del estudiante", "Asistencia"), resultado.Respuesta.Citas);
    }

    [Fact]
    [Trait("CA", "CA-27")]
    [Trait("ESC", "ESC-189")]
    [Trait("SEG", "SEG-02")]
    [Trait("D", "D-96")]
    public async Task ESC_189_p_Con_dos_estudiantes_nombrados_no_hay_desglose_y_se_sigue_la_ruta_normal_con_prelectura_de_su_programa()
    {
        var escenario = Con(Fijo("Ambos tienen faltas registradas."), IndiceDeFaltas());

        var resultado = await escenario.PreguntarAsync(Coord01, "¿Cuántas faltas tienen Andrés Martínez y Sofía Herrera?");

        var lecturas = escenario.Api.LecturasNotas.Concat(escenario.Api.LecturasAsistencia).ToList();
        Assert.DoesNotContain("Total:", resultado.Respuesta!.Respuesta, StringComparison.Ordinal);
        Assert.DoesNotContain("faltas restantes", resultado.Respuesta.Respuesta, StringComparison.Ordinal);
        Assert.Contains("EST-001", lecturas);
        Assert.Empty(lecturas.Intersect(EstudiantesDeAdm));
    }
}
