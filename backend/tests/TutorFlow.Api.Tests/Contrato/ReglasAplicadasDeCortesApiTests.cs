using TutorFlow.Api.Tests.Dobles;
using TutorFlow.Core.Asistente;

namespace TutorFlow.Api.Tests.Contrato;

public sealed class ReglasAplicadasDeCortesApiTests
{
    private const string EncabezadoDeReglas = "Reglas aplicadas a estos datos: ";
    private const string PreguntaDeCorte = "¿Cómo se califican los cortes de una asignatura?";

    private static ClienteLlmDoble ConsultaCortes() =>
        new((solicitud, numero) => numero == 0
            ? new RespuestaLlm(null, [new LlamadaHerramienta("cortes-1", HerramientasAsistente.Cortes, """{"asignatura":"Estadística"}""")], ClienteLlmDoble.UsoPorDefecto, false)
            : new RespuestaLlm("Tu nota del corte 1 de Estadística es 3.9.", [], ClienteLlmDoble.UsoPorDefecto, false));

    [Fact]
    [Trait("CA", "CA-14")]
    [Trait("ESC", "ESC-198")]
    [Trait("D", "D-66")]
    public async Task El_resultado_de_consultar_cortes_termina_con_reglas_aplicadas()
    {
        var llm = ConsultaCortes();
        using var fabrica = new FabricaApi(llm: llm, conocimientoReal: true);

        await fabrica.PreguntarEstudianteAsync("EST-006", PreguntaDeCorte);

        var resultado = llm.Solicitudes[1].Mensajes.Last(m => m.Rol == RolMensaje.Herramienta).Contenido;
        var ultimaLinea = resultado.Split('\n').Select(l => l.TrimEnd()).Where(l => l.Length > 0 && !l.Contains("DATOS_NO_CONFIABLES", StringComparison.Ordinal)).Last();
        Assert.StartsWith(EncabezadoDeReglas, ultimaLinea, StringComparison.Ordinal);
        Assert.EndsWith(".", ultimaLinea, StringComparison.Ordinal);
        Assert.Contains("evaluacion_y_calificaciones.md · Sección 3", ultimaLinea, StringComparison.Ordinal);
    }
}
