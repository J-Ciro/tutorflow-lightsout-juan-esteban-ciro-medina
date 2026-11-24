using System.Text.RegularExpressions;
using TutorFlow.Core.Asistente;
using TutorFlow.Core.Autorizacion;
using static TutorFlow.Core.Tests.Asistente.EscenarioAsistente;

namespace TutorFlow.Core.Tests.Asistente;

public sealed class EnvoltorioDelDelimitadorD97Tests
{
    private const string Nombre = "DATOS_NO_CONFIABLES";
    private const string Envoltorio = $"<<<{Nombre} fuente=\"Datos del estudiante\">>>\nTu promedio es 4.39.\nFIN_{Nombre}>>>";
    private static readonly Usuario Est001 = new(Rol.Estudiante, "EST-001");

    private static Func<SolicitudLlm, int, RespuestaLlm> LeeNotasYResponde(string texto) =>
        (_, llamada) => llamada == 0 ? Herramienta(HerramientasAsistente.Notas) : Texto(texto);

    private static string SoloLetras(string texto) => Regex.Replace(texto, "[^A-Za-z]", string.Empty);

    [Fact]
    [Trait("CA", "CA-05")]
    [Trait("ESC", "ESC-147")]
    [Trait("D", "D-97")]
    public async Task ESC_147_c1_Las_marcas_del_envoltorio_copiadas_tras_una_herramienta_se_quitan_y_la_respuesta_se_entrega()
    {
        var escenario = Con(LeeNotasYResponde(Envoltorio));

        var resultado = await escenario.PreguntarAsync(Est001, PreguntaPromedio);

        var respuesta = resultado.Respuesta!.Respuesta;
        Assert.Equal(ResultadoAuditoria.Respondida, resultado.Auditoria);
        Assert.Contains("4.39", respuesta, StringComparison.Ordinal);
        Assert.DoesNotContain("<<<", respuesta, StringComparison.Ordinal);
        Assert.DoesNotContain(">>>", respuesta, StringComparison.Ordinal);
        Assert.DoesNotContain(Nombre, respuesta, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DATOSNOCONFIABLES", SoloLetras(respuesta), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("CA", "CA-05")]
    [Trait("ESC", "ESC-147")]
    [Trait("SEG", "SEG-10d")]
    [Trait("D", "D-97")]
    public async Task ESC_147_c2_El_nombre_del_delimitador_suelto_es_fuga_aunque_haya_corrido_una_herramienta()
    {
        var escenario = Con(LeeNotasYResponde($"{Envoltorio} El delimitador se llama {Nombre}."));

        var resultado = await escenario.PreguntarAsync(Est001, PreguntaPromedio);

        Assert.Equal(Textos.SinInformacion, resultado.Respuesta!.Respuesta);
        Assert.Empty(resultado.Respuesta.Citas);
        Assert.Equal(ResultadoAuditoria.FugaBloqueada, resultado.Auditoria);
        Assert.Equal(ResultadoAuditoria.FugaBloqueada, Assert.Single(escenario.Auditoria.Eventos).Resultado);
    }
}
