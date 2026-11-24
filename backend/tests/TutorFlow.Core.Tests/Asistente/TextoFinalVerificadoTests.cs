using TutorFlow.Core.Asistente;
using TutorFlow.Core.Autorizacion;
using TutorFlow.Core.Conocimiento;
using TutorFlow.Pruebas.Soporte;
using static TutorFlow.Core.Tests.Asistente.EscenarioAsistente;
using static TutorFlow.Core.Tests.Asistente.SeccionesDeReferencia;

namespace TutorFlow.Core.Tests.Asistente;

public sealed class TextoFinalVerificadoTests
{
    private const string PreguntaNotaMinima = "¿Cuál es la nota mínima para aprobar una asignatura?";
    private const string PreguntaPromedioPonderado = "¿Cuál es mi promedio ponderado del periodo?";
    private const string PreguntaPerdieron = "¿Qué estudiantes de mi programa perdieron materias?";
    private const string PreguntaDefinitiva = "¿Cuál es mi nota definitiva en Estadística?";
    private const string PreguntaHorario = "¿Cuál es el horario de atención de Bienestar?";
    private static readonly Usuario Est001 = new(Rol.Estudiante, "EST-001");
    private static readonly Usuario Est006 = new(Rol.Estudiante, "EST-006");
    private static readonly Usuario Coord01 = new(Rol.Coordinador, "COORD-01");
    private static readonly Cita CitaNegativa = new(Reglamento, "Artículo 22");

    public static TheoryData<string, string> LimpiezaQueNoUneTokens => new()
    {
        { "Tu promedio es 1 x.md .2", "Tu promedio es 1 .2" },
        { "Tu promedio es 4.39, igual que EST a.md 002.", "Tu promedio es 4.39, igual que EST 002." },
        { "Valentina b.md Castro (EST a.md 005) tiene Microeconomía perdida.", "Valentina Castro (EST 005) tiene Microeconomía perdida." },
        { "Tu definitiva x1.md x2.md x3.md x4.md es 4.0 en Estadística.", "Tu definitiva es 4.0 en Estadística." },
        { "abc x.md def", "abc def" },
        { "Promedio 4 x.md 39", "Promedio 4 39" },
        { "EST a.md 002", "EST 002" }
    };

    public static TheoryData<string, bool> CifrasPartidas => new()
    {
        { "Se aprueba con 1 .2", false },
        { "Se aprueba con 1 ,2", false },
        { "Se aprueba con 4 .40.", false },
        { "Se aprueba con 3 .0 (reglamento_academico_2025.md · Artículo 5).", true }
    };

    [Theory]
    [MemberData(nameof(LimpiezaQueNoUneTokens))]
    [Trait("CA", "CA-16")]
    [Trait("ESC", "ESC-183")]
    [Trait("RF", "COMP-02")]
    public void ESC_183_El_borrado_de_una_referencia_reemplaza_por_un_espacio_y_nunca_fusiona_tokens(string respuesta, string esperada)
    {
        var limpia = ReferenciasEscritas.Limpiar(respuesta, []);

        Assert.Equal(esperada, limpia);
    }

    [Fact]
    [Trait("CA", "CA-16")]
    [Trait("ESC", "ESC-183")]
    [Trait("RF", "SEG-10c")]
    public async Task ESC_183a_La_cifra_que_la_limpieza_deja_partida_se_verifica_como_1_2_y_no_se_entrega()
    {
        var escenario = Con(Fijo("Se aprueba con 1 x.md .2"), Indice(Articulo5));

        var resultado = await escenario.PreguntarAsync(Est001, PreguntaNotaMinima);

        Assert.Equal(Textos.NoVerificable, resultado.Respuesta!.Respuesta);
        Assert.Empty(resultado.Respuesta.Citas);
        Assert.Equal(ResultadoAuditoria.NumeroNoVerificado, resultado.Auditoria);
        Assert.Equal(ResultadoAuditoria.NumeroNoVerificado, Assert.Single(escenario.Auditoria.Eventos).Resultado);
    }

    [Theory]
    [MemberData(nameof(CifrasPartidas))]
    [Trait("CA", "CA-16")]
    [Trait("ESC", "ESC-183")]
    [Trait("RF", "SEG-10c")]
    public async Task ESC_183_Una_cifra_con_espacios_antes_del_separador_decimal_se_verifica_sin_los_espacios(string texto, bool seEntrega)
    {
        var escenario = Con(Fijo(texto), Indice(Articulo5));

        var resultado = await escenario.PreguntarAsync(Est001, PreguntaNotaMinima);

        if (seEntrega)
        {
            Assert.Equal(texto, resultado.Respuesta!.Respuesta);
            Assert.Equal(ResultadoAuditoria.Respondida, resultado.Auditoria);
            return;
        }

        Assert.Equal(Textos.NoVerificable, resultado.Respuesta!.Respuesta);
        Assert.Equal(ResultadoAuditoria.NumeroNoVerificado, resultado.Auditoria);
    }

    [Fact]
    [Trait("CA", "CA-16")]
    [Trait("ESC", "ESC-183")]
    [Trait("RF", "SEG-10b")]
    public async Task ESC_183b_El_id_que_la_limpieza_deja_como_EST_002_se_bloquea_con_la_negativa_y_sin_reintento()
    {
        var escenario = Con(Fijo("Tu promedio es 4.39, igual que EST a.md 002."));

        var resultado = await escenario.PreguntarAsync(Est001, PreguntaPromedio);

        Assert.Equal(Textos.NegativaEstudiante, resultado.Respuesta!.Respuesta);
        Assert.Equal([CitaNegativa], resultado.Respuesta.Citas);
        Assert.Equal(ResultadoAuditoria.FugaBloqueada, resultado.Auditoria);
        Assert.Single(escenario.Llm.Solicitudes);
        Assert.False(ReintentoDeLlm.ReintentoRegistrado(Assert.Single(escenario.Auditoria.Eventos)));
    }

    [Fact]
    [Trait("CA", "CA-16")]
    [Trait("ESC", "ESC-183")]
    [Trait("RF", "SEG-10b")]
    public async Task ESC_183c_El_coordinador_que_nombra_a_un_estudiante_ajeno_con_el_id_partido_recibe_la_negativa()
    {
        var escenario = Con(SalidasVerificadas.LeeElProgramaYResponde("Valentina b.md Castro (EST a.md 005) tiene Microeconomía perdida."), Indice(Articulo5, Articulo6, Articulo20));

        var resultado = await escenario.PreguntarAsync(Coord01, PreguntaPerdieron);

        Assert.Equal(Textos.NegativaCoordinador, resultado.Respuesta!.Respuesta);
        Assert.Equal([CitaNegativa], resultado.Respuesta.Citas);
        Assert.Equal(ResultadoAuditoria.FugaBloqueada, resultado.Auditoria);
        Assert.Equal(ResultadoAuditoria.FugaBloqueada, Assert.Single(escenario.Auditoria.Eventos).Resultado);
    }

    [Fact]
    [Trait("CA", "CA-16")]
    [Trait("ESC", "ESC-183")]
    [Trait("RF", "SEG-10")]
    public async Task ESC_183e_Una_referencia_valida_se_conserva_y_la_respuesta_se_entrega_sin_cambios()
    {
        var texto = "Se aprueba con 3.0 (reglamento_academico_2025.md · Artículo 5).";
        var escenario = Con(Fijo(texto), Indice(Articulo5));

        var resultado = await escenario.PreguntarAsync(Est001, PreguntaNotaMinima);

        Assert.Equal(texto, resultado.Respuesta!.Respuesta);
        Assert.Contains(new Cita(Reglamento, "Artículo 5"), resultado.Respuesta.Citas);
        Assert.Equal(ResultadoAuditoria.Respondida, resultado.Auditoria);
    }

    [Fact]
    [Trait("CA", "CA-16")]
    [Trait("ESC", "ESC-183")]
    [Trait("RF", "COMP-02")]
    public async Task ESC_183f_La_referencia_invalida_se_elimina_con_su_parentesis_y_la_cita_escrita_se_descarta()
    {
        var escenario = Con(Fijo("Se aprueba con 3.0 (x.md · Sección 9)."), Indice(Articulo5));

        var resultado = await escenario.PreguntarAsync(Est001, PreguntaNotaMinima);

        Assert.Equal("Se aprueba con 3.0.", resultado.Respuesta!.Respuesta);
        Assert.DoesNotContain(resultado.Respuesta.Citas, c => c.Documento.StartsWith("x.md", StringComparison.Ordinal));
        Assert.Contains(new Cita(Reglamento, "Artículo 5"), resultado.Respuesta.Citas);
        Assert.Equal(ResultadoAuditoria.Respondida, resultado.Auditoria);
    }

    [Fact]
    [Trait("CA", "CA-16")]
    [Trait("ESC", "ESC-183")]
    [Trait("RF", "SEG-10")]
    public async Task ESC_183g_Lo_que_se_entrega_es_exactamente_el_texto_ya_limpio()
    {
        var escenario = Con(Fijo("Bienestar atiende de lunes a viernes de 8:00 a 17:00 (Documento.md · Sección 11)."), Indice(Atencion));

        var resultado = await escenario.PreguntarAsync(Est001, PreguntaHorario);

        Assert.Equal("Bienestar atiende de lunes a viernes de 8:00 a 17:00.", resultado.Respuesta!.Respuesta);
    }
}
