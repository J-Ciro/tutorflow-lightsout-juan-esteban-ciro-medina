using TutorFlow.Core.Asistente;
using TutorFlow.Core.Autorizacion;
using TutorFlow.Core.Conocimiento;
using static TutorFlow.Core.Tests.Asistente.EscenarioAsistente;
using static TutorFlow.Core.Tests.Asistente.SeccionesDeReferencia;

namespace TutorFlow.Core.Tests.Asistente;

public sealed class LimpiezaAmpliadaTests
{
    private const string PreguntaPromedio4 = "¿Cuál es mi promedio ponderado del periodo?";
    private static readonly Usuario Est001 = new(Rol.Estudiante, "EST-001");
    private static readonly Cita Art5 = new(Reglamento, "Artículo 5");
    private static readonly Cita Art11 = new(Reglamento, "Artículo 11");
    private static readonly Cita Art20 = new(Reglamento, "Artículo 20");
    private static readonly Cita Beca31 = new(Bienestar, "Sección 3.1");
    private static readonly Cita Bienestar1 = new(Bienestar, "Sección 1");

    public static TheoryData<string[], string, string> TextosReales => new()
    {
        {
            [Texto(Beca31)],
            "Para obtener la beca por rendimiento académico, debes cumplir los siguientes requisitos:\n\n- Promedio ponderado del periodo ≥ 4.3\n- Ninguna asignatura perdida, incluidas las por inasistencia\n- Matrícula de al menos 12 créditos\n\nEstos requisitos están respaldados por el Artículo 20 de reglamento_academico_2025.md · Artículo 20.",
            "Para obtener la beca por rendimiento académico, debes cumplir los siguientes requisitos:\n\n- Promedio ponderado del periodo ≥ 4.3\n- Ninguna asignatura perdida, incluidas las por inasistencia\n- Matrícula de al menos 12 créditos"
        },
        { [], "El horario es de 8:00 a 17:00. Estos horarios se encuentran en Documento.md · Sección 1.", "El horario es de 8:00 a 17:00." },
        { [Texto(Beca31)], "Requisitos: promedio 4.3 o más. Están respaldados por reglamento_academico_2025.md · Artículo 20 · Sección 3.1.", "Requisitos: promedio 4.3 o más." },
        { [Texto(Art20)], "Según reglamento_academico_2023.md · Artículo 20: Queda en prueba académica quien tenga promedio inferior a 3.0.", "Queda en prueba académica quien tenga promedio inferior a 3.0." },
        { [Texto(Beca31)], "Requisitos: promedio 4.3 o más. Esta información proviene del reglamento académico.", "Requisitos: promedio 4.3 o más." },
        { [], "Tu promedio es 4.39. No encuentro esa información en los documentos disponibles.", "Tu promedio es 4.39." }
    };

    public static TheoryData<string[], string> TextosSinCambios => new()
    {
        { [Texto(Art5)], "Se aprueba con 3.0 según el reglamento." },
        { [Texto(Bienestar1)], "La oficina de Bienestar atiende de lunes a viernes." },
        { [Texto(Art11)], "La regla se encuentra en reglamento_academico_2025.md · Artículo 11." },
        { [Texto(Art5)], "Ese es el motivo por el que preguntas por." }
    };

    public static TheoryData<string[], string, string> OracionesHuerfanas => new()
    {
        { [], "El horario es de 8:00 a 17:00. La tasa se calcula con Documento.md.", "El horario es de 8:00 a 17:00." },
        { [], "El horario es de 8:00 a 17:00. Estos datos se basan en Documento.md · Sección 2.", "El horario es de 8:00 a 17:00." },
        { [Texto(Beca31)], "Requisitos: promedio 4.3 o más. Esta información se establece en reglamento_academico_2025.md · Artículo 20.", "Requisitos: promedio 4.3 o más." }
    };

    public static TheoryData<string[], string, string> AtribucionesGenericas => new()
    {
        { [Texto(Beca31)], "Requisitos: promedio 4.3 o más, según el reglamento académico.", "Requisitos: promedio 4.3 o más." },
        { [], "Se aprueba con 3.0. Esta información proviene del documento de evaluación y calificaciones.", "Se aprueba con 3.0." },
        { [Texto(Art5)], "Se aprueba con 3.0 según el reglamento académico.", "Se aprueba con 3.0 según el reglamento académico." },
        { [Texto(Bienestar1)], "Bienestar atiende de lunes a viernes. La oficina de Bienestar queda en el edificio B.", "Bienestar atiende de lunes a viernes. La oficina de Bienestar queda en el edificio B." }
    };

    private static string Texto(Cita cita) => $"{cita.Documento} · {cita.Seccion}";

    private static Cita DesdeTexto(string texto)
    {
        var partes = texto.Split(" · ");
        return new Cita(partes[0], partes[1]);
    }

    private static string Limpiar(string respuesta, string[] citas) =>
        ReferenciasEscritas.Limpiar(respuesta, [.. citas.Select(DesdeTexto)]);

    [Theory]
    [MemberData(nameof(TextosReales))]
    [Trait("CA", "CA-14")]
    [Trait("ESC", "ESC-195")]
    [Trait("RF", "COMP-02")]
    public void ESC_195_La_limpieza_ampliada_corrige_los_textos_reales_de_la_ronda_4(string[] citas, string respuesta, string esperada)
    {
        Assert.Equal(esperada, Limpiar(respuesta, citas));
    }

    [Theory]
    [MemberData(nameof(TextosSinCambios))]
    [Trait("CA", "CA-14")]
    [Trait("ESC", "ESC-195")]
    [Trait("RF", "COMP-02")]
    public void ESC_195g_Un_texto_sin_referencia_invalida_ni_atribucion_sin_cita_no_cambia(string[] citas, string respuesta)
    {
        Assert.Equal(respuesta, Limpiar(respuesta, citas));
    }

    [Theory]
    [MemberData(nameof(OracionesHuerfanas))]
    [Trait("CA", "CA-14")]
    [Trait("ESC", "ESC-195")]
    [Trait("RF", "COMP-02")]
    public void ESC_195_Una_oracion_a_la_que_se_le_borro_algo_y_termina_en_preposicion_o_atribucion_se_elimina_entera(string[] citas, string respuesta, string esperada)
    {
        Assert.Equal(esperada, Limpiar(respuesta, citas).Trim());
    }

    [Theory]
    [MemberData(nameof(AtribucionesGenericas))]
    [Trait("CA", "CA-14")]
    [Trait("ESC", "ESC-195")]
    [Trait("RF", "COMP-02")]
    public void ESC_195_La_atribucion_generica_a_un_documento_sin_cita_se_elimina_y_con_cita_se_conserva(string[] citas, string respuesta, string esperada)
    {
        Assert.Equal(esperada, Limpiar(respuesta, citas));
    }

    [Fact]
    [Trait("CA", "CA-14")]
    [Trait("ESC", "ESC-195")]
    [Trait("RF", "COMP-02")]
    public async Task ESC_195f_La_frase_fija_de_COMP_03_anexada_a_una_respuesta_con_datos_se_elimina_y_se_entrega_el_resto()
    {
        var escenario = Con(Fijo("Tu promedio es 4.39. No encuentro esa información en los documentos disponibles."), Indice(PromedioPonderado, NotaDefinitiva));

        var resultado = await escenario.PreguntarAsync(Est001, PreguntaPromedio4);

        Assert.Equal("Tu promedio es 4.39.", resultado.Respuesta!.Respuesta);
        Assert.Equal(ResultadoAuditoria.Respondida, resultado.Auditoria);
        Assert.True(resultado.Respuesta.UsoDatosEstudiante);
    }

    [Fact]
    [Trait("CA", "CA-14")]
    [Trait("ESC", "ESC-195")]
    [Trait("RF", "COMP-03")]
    public async Task ESC_195h_Solo_la_frase_fija_es_la_respuesta_de_COMP_03_sin_citas_y_con_auditoria_sin_informacion()
    {
        var escenario = Con(Fijo(Textos.SinInformacion));

        var resultado = await escenario.PreguntarAsync(Est001, PreguntaPromedio);

        Assert.Equal(Textos.SinInformacion, resultado.Respuesta!.Respuesta);
        Assert.Empty(resultado.Respuesta.Citas);
        Assert.Equal(ResultadoAuditoria.SinInformacion, resultado.Auditoria);
    }

    [Fact]
    [Trait("CA", "CA-14")]
    [Trait("ESC", "ESC-195")]
    [Trait("RF", "COMP-02")]
    public async Task ESC_195b_La_oracion_huerfana_tampoco_llega_a_la_respuesta_entregada()
    {
        var escenario = Con(Fijo("Bienestar atiende de lunes a viernes de 8:00 a 17:00. Estos horarios se encuentran en Documento.md · Sección 1."), Indice(Atencion));

        var resultado = await escenario.PreguntarAsync(Est001, "¿Cuál es el horario de atención de Bienestar?");

        Assert.Equal("Bienestar atiende de lunes a viernes de 8:00 a 17:00.", resultado.Respuesta!.Respuesta);
    }
}
