using TutorFlow.Core.Asistente;
using TutorFlow.Core.Autorizacion;
using TutorFlow.Core.Conocimiento;
using static TutorFlow.Core.Tests.Asistente.EscenarioAsistente;
using static TutorFlow.Core.Tests.Asistente.SeccionesDeReferencia;

namespace TutorFlow.Core.Tests.Asistente;

public sealed class CifrasComoApoyoTests
{
    private const string PreguntaDefinitiva = "¿Cómo se calcula la nota definitiva de una asignatura?";
    private const string RespuestaPesos = "La definitiva pondera 30% el corte 1, 30% el corte 2 y 40% el corte 3, y se redondea a una cifra decimal.";
    private const string CitasEscritas = $"({Evaluacion} · Sección 2) ({Evaluacion} · Sección 3)";
    private static readonly Usuario Est001 = new(Rol.Estudiante, "EST-001");
    private static readonly Cita CitaCortes = new(Evaluacion, "Sección 2");
    private static readonly Cita CitaDefinitiva = new(Evaluacion, "Sección 3");
    private static readonly SeccionDocumento DescuentoDecimal = new(Bienestar, "Sección 9", "Beca de prueba", "Descuento del 12.5% para estudiantes de prueba.");

    public static TheoryData<string, string[]> RespuestasConApoyoNumerico => new()
    {
        { "La definitiva pondera 30% el corte 1, 30% el corte 2 y 40% el corte 3, y se redondea a una cifra decimal", ["Sección 2", "Sección 3"] },
        { "El corte 1 pesa 30%", ["Sección 2"] },
        { "El corte 1 pesa 30 %", ["Sección 2"] },
        { "La definitiva se redondea a una cifra decimal", ["Sección 3"] }
    };

    private static string ReferenciaEscrita(string seccion) => $"({Evaluacion} · {seccion})";

    public static TheoryData<string> RespuestasSinApoyoNumerico => new()
    {
        "El corte 1 pesa 30 puntos.",
        "El corte 1 pesa 30.",
        "El corte 1 pesa 3 puntos.",
        "El corte 1 pesa 30 por ciento."
    };

    [Theory]
    [MemberData(nameof(RespuestasConApoyoNumerico))]
    [Trait("ESC", "ESC-171")]
    [Trait("CA", "CA-14")]
    [Trait("COMP", "COMP-02")]
    [Trait("SEG", "SEG-10a")]
    [Trait("D", "D-37")]
    public async Task La_cita_escrita_con_apoyo_se_conserva_y_la_escrita_sin_apoyo_se_elimina_del_texto(string cuerpo, string[] seccionesEsperadas)
    {
        var escenario = Con(Fijo($"{cuerpo} {CitasEscritas}."), Indice(Cortes, NotaDefinitiva));
        var conservadas = string.Join(" ", seccionesEsperadas.Select(ReferenciaEscrita));

        var resultado = await escenario.PreguntarAsync(Est001, PreguntaDefinitiva);

        Assert.Equal(ResultadoAuditoria.Respondida, resultado.Auditoria);
        Assert.Equal($"{cuerpo} {conservadas}.", resultado.Respuesta!.Respuesta);
        Assert.Equal(seccionesEsperadas.Select(s => new Cita(Evaluacion, s)), resultado.Respuesta.Citas);
        Assert.False(resultado.Respuesta.UsoDatosEstudiante);
    }

    [Theory]
    [MemberData(nameof(RespuestasSinApoyoNumerico))]
    [Trait("ESC", "ESC-171")]
    [Trait("CA", "CA-14")]
    [Trait("COMP", "COMP-02")]
    [Trait("SEG", "SEG-10e")]
    public async Task Un_entero_sin_porcentaje_no_aporta_apoyo_y_las_citas_escritas_se_descartan(string respuesta)
    {
        var escenario = Con(Fijo($"{respuesta} {CitasEscritas}"), Indice(Cortes, NotaDefinitiva));

        var resultado = await escenario.PreguntarAsync(Est001, PreguntaDefinitiva);

        AfirmarSinInformacionPorConsumoReal(resultado, escenario);
    }

    [Fact]
    [Trait("ESC", "ESC-171")]
    [Trait("CA", "CA-01")]
    [Trait("COMP", "COMP-02")]
    public async Task Sin_citas_escritas_el_respaldo_de_los_pesos_son_la_Seccion_2_y_la_Seccion_3_en_orden_de_ranking()
    {
        var escenario = Con(Fijo(RespuestaPesos), Indice(Cortes, NotaDefinitiva));

        var resultado = await escenario.PreguntarAsync(Est001, PreguntaDefinitiva);

        Assert.Equal(ResultadoAuditoria.Respondida, resultado.Auditoria);
        Assert.Equal([CitaCortes, CitaDefinitiva], resultado.Respuesta!.Citas.ToArray());
        Assert.False(resultado.Respuesta.UsoDatosEstudiante);
    }

    [Fact]
    [Trait("ESC", "ESC-171")]
    [Trait("SEG", "SEG-10c")]
    public async Task Un_porcentaje_inventado_se_bloquea_antes_de_contar_como_apoyo()
    {
        var escenario = Con(Fijo($"El corte 1 pesa 35%. {CitasEscritas}"), Indice(Cortes, NotaDefinitiva));

        var resultado = await escenario.PreguntarAsync(Est001, PreguntaDefinitiva);

        Assert.Equal(Textos.NoVerificable, resultado.Respuesta!.Respuesta);
        Assert.Empty(resultado.Respuesta.Citas);
        Assert.Equal(ResultadoAuditoria.NumeroNoVerificado, resultado.Auditoria);
    }

    [Fact]
    [Trait("ESC", "ESC-171")]
    [Trait("COMP", "COMP-02")]
    public async Task Un_porcentaje_con_decimales_se_compara_como_valor_literal_y_completa_el_limite()
    {
        var escenario = Con(Fijo("El descuento es 12.5%."), Indice(DescuentoDecimal));

        var resultado = await escenario.PreguntarAsync(Est001, "¿Cuál es el descuento?");

        Assert.Equal(ResultadoAuditoria.Respondida, resultado.Auditoria);
        Assert.Equal([new Cita(Bienestar, "Sección 9")], resultado.Respuesta!.Citas.ToArray());
    }

    [Fact]
    [Trait("ESC", "ESC-171")]
    [Trait("COMP", "COMP-02")]
    public async Task Un_entero_de_la_respuesta_y_de_la_seccion_no_cuenta_como_raiz_compartida()
    {
        var escenario = Con(Fijo("Las tutorías son los lunes de 8 a 12."), Indice(Atencion));

        var resultado = await escenario.PreguntarAsync(Est001, "¿Cuándo atiende Bienestar los lunes?");

        AfirmarSinInformacionPorConsumoReal(resultado, escenario);
    }

    private static void AfirmarSinInformacionPorConsumoReal(ResultadoPregunta resultado, EscenarioAsistente escenario)
    {
        var respuesta = resultado.Respuesta!;
        Assert.Equal(Textos.SinInformacion, respuesta.Respuesta);
        Assert.Empty(respuesta.Citas);
        Assert.False(respuesta.UsoDatosEstudiante);
        Assert.True(respuesta.Tokens > 0);
        Assert.Equal(ResultadoAuditoria.SinInformacion, resultado.Auditoria);
        Assert.Equal(ResultadoAuditoria.SinInformacion, Assert.Single(escenario.Auditoria.Eventos).Resultado);
    }
}
