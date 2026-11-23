using System.Text.RegularExpressions;
using TutorFlow.Core.Asistente;
using TutorFlow.Core.Autorizacion;
using TutorFlow.Core.Conocimiento;
using static TutorFlow.Core.Tests.Asistente.EscenarioAsistente;
using static TutorFlow.Core.Tests.Asistente.SeccionesDeReferencia;

namespace TutorFlow.Core.Tests.Asistente;

public sealed class ActivadoresResumenPropioTests
{
    private const string MarcaEst001 = "Cálculo Integral";
    private const string RespuestaGenerica = "Revisa el detalle de tus asignaturas.";
    private static readonly Usuario Est001 = new(Rol.Estudiante, "EST-001");
    private static readonly Usuario Est002 = new(Rol.Estudiante, "EST-002");
    private static readonly Usuario Est004 = new(Rol.Estudiante, "EST-004");
    private static readonly Usuario Est005 = new(Rol.Estudiante, "EST-005");
    private static readonly Usuario Coord01 = new(Rol.Coordinador, "COORD-01");

    public static TheoryData<string> PreguntasConPuedoYTerminoEspecifico => new()
    {
        "¿Qué materias puedo habilitar?",
        "¿Puedo habilitar alguna?",
        "¿Cuántas faltas más puedo tener en Bases de Datos sin perderla?",
        "¿PUEDO HABILITAR?",
        "¿Puedo aplicar a la beca por rendimiento?"
    };

    public static TheoryData<string> PreguntasConAsignaturaMatriculada => new()
    {
        "¿Cómo voy en Bases de Datos?",
        "¿Cómo voy en BD-203?",
        "¿Cómo voy en bd-203?",
        "¿Cómo voy en Inglés III?",
        "¿Cómo voy en INGLES III?",
        "¿Cómo voy en Programación Orientada a Objetos?",
        "¿Cómo voy en 𝐁𝐚𝐬𝐞𝐬 𝐝𝐞 𝐃𝐚𝐭𝐨𝐬?"
    };

    public static TheoryData<string> PreguntasQueNoActivan => new()
    {
        "¿Con qué nota puedo habilitar una materia?",
        "¿Con qué nota puedo habilitar una asignatura?",
        "¿Hasta qué semana puedo cancelar una asignatura?",
        "¿Qué materias puedo cursar?",
        "¿Qué asignaturas puedo inscribir?",
        "¿Qué dice el reglamento sobre el cálculo de la definitiva?",
        "¿Cómo voy en Cálculo?",
        "¿Cómo voy en Bases y Datos?"
    };

    public static TheoryData<string> PreguntasDeCoordinador => new()
    {
        "¿Qué materias puedo habilitar?",
        "¿Puedo aplicar a la beca por rendimiento?"
    };

    [Theory]
    [MemberData(nameof(PreguntasConPuedoYTerminoEspecifico))]
    [Trait("ESC", "ESC-172")]
    [Trait("CA", "CA-14")]
    [Trait("COMP", "COMP-07")]
    public async Task Puedo_con_un_termino_de_datos_especifico_entrega_el_resumen_propio(string pregunta)
    {
        var escenario = Con(Fijo(RespuestaGenerica));

        var resultado = await escenario.PreguntarAsync(Est001, pregunta);

        Assert.Contains(MarcaEst001, escenario.Llm.TextoDeLlamada(0), StringComparison.Ordinal);
        Assert.True(resultado.Respuesta!.UsoDatosEstudiante);
        Assert.Contains(new Cita(Textos.DocumentoDatosEstudiante, "Notas"), resultado.Respuesta.Citas);
    }

    [Theory]
    [MemberData(nameof(PreguntasConAsignaturaMatriculada))]
    [Trait("ESC", "ESC-172")]
    [Trait("CA", "CA-14")]
    [Trait("COMP", "COMP-07")]
    public async Task Nombrar_una_asignatura_matriculada_por_nombre_o_codigo_entrega_el_resumen_propio(string pregunta)
    {
        var escenario = Con(Fijo(RespuestaGenerica));

        var resultado = await escenario.PreguntarAsync(Est001, pregunta);

        var primera = escenario.Llm.TextoDeLlamada(0);
        Assert.Contains(MarcaEst001, primera, StringComparison.Ordinal);
        Assert.Contains("Bases de Datos", primera, StringComparison.Ordinal);
        Assert.True(resultado.Respuesta!.UsoDatosEstudiante);
        Assert.Contains(new Cita(Textos.DocumentoDatosEstudiante, "Notas"), resultado.Respuesta.Citas);
    }

    [Fact]
    [Trait("ESC", "ESC-172")]
    [Trait("COMP", "COMP-07")]
    public async Task Un_codigo_sin_otro_termino_con_cero_raices_llega_al_LLM_porque_la_activacion_exime_del_umbral()
    {
        var escenario = Con(Fijo(RespuestaGenerica), Indice());

        var resultado = await escenario.PreguntarAsync(Est001, "¿Cómo voy en BD-203?");

        Assert.Single(escenario.Llm.Solicitudes);
        Assert.Contains(MarcaEst001, escenario.Llm.TextoDeLlamada(0), StringComparison.Ordinal);
        Assert.True(resultado.Respuesta!.UsoDatosEstudiante);
    }

    [Fact]
    [Trait("ESC", "ESC-172")]
    [Trait("COMP", "COMP-07")]
    public async Task EST_103_nombrado_por_una_estudiante_matriculada_es_la_asignatura_Estadistica_y_no_una_persona()
    {
        var escenario = Con(Fijo(RespuestaGenerica));

        var resultado = await escenario.PreguntarAsync(Est005, "¿Cómo voy en EST-103?");

        Assert.Equal(TipoResultadoPregunta.Respondida, resultado.Tipo);
        Assert.Contains("Estadística", escenario.Llm.TextoDeLlamada(0), StringComparison.Ordinal);
        Assert.True(resultado.Respuesta!.UsoDatosEstudiante);
    }

    [Theory]
    [MemberData(nameof(PreguntasQueNoActivan))]
    [Trait("ESC", "ESC-155")]
    [Trait("ESC", "ESC-172")]
    [Trait("COMP", "COMP-07")]
    public async Task Los_genericos_los_nombres_parciales_y_los_genericos_indefinidos_no_entregan_el_resumen(string pregunta)
    {
        var escenario = Con(Fijo(Textos.SinInformacion));

        var resultado = await escenario.PreguntarAsync(Est001, pregunta);

        Assert.DoesNotContain(MarcaEst001, escenario.Llm.TextoDeLlamada(0), StringComparison.Ordinal);
        Assert.Empty(escenario.Api.LecturasNotas);
        Assert.Empty(escenario.Api.LecturasAsistencia);
        Assert.False(resultado.Respuesta!.UsoDatosEstudiante);
    }

    [Theory]
    [MemberData(nameof(PreguntasDeCoordinador))]
    [Trait("ESC", "ESC-155")]
    [Trait("COMP", "COMP-07")]
    public async Task El_coordinador_nunca_recibe_resumen_aunque_nombre_una_asignatura_o_use_puedo(string pregunta)
    {
        var escenario = Con(Fijo(Textos.SinInformacion));

        var resultado = await escenario.PreguntarAsync(Coord01, pregunta);

        Assert.DoesNotContain(MarcaEst001, escenario.Llm.TextoDeLlamada(0), StringComparison.Ordinal);
        Assert.Empty(escenario.Api.LecturasNotas);
        Assert.Empty(escenario.Api.LecturasAsistencia);
        Assert.False(resultado.Respuesta!.UsoDatosEstudiante);
    }

    [Fact]
    [Trait("ESC", "ESC-172")]
    [Trait("ESC", "ESC-032")]
    [Trait("CA", "CA-14")]
    [Trait("COMP", "COMP-07")]
    [Trait("RN", "RN-10")]
    public async Task EST_002_pregunta_si_puede_habilitar_Bases_de_Datos_y_recibe_su_resumen_con_notas_y_asistencia()
    {
        var escenario = Con(Fijo(RespuestaGenerica), Indice());

        var resultado = await escenario.PreguntarAsync(Est002, "¿Puedo habilitar Bases de Datos?");

        var primera = escenario.Llm.TextoDeLlamada(0);
        Assert.Matches(new Regex(@"Bases de Datos.*(?<![\d.])9(?![\d.])", RegexOptions.Singleline), primera);
        Assert.Matches(@"Cálculo Integral.*(?<![\d.])2\.6", primera);
        Assert.DoesNotContain("Laura Gómez", primera, StringComparison.Ordinal);
        Assert.DoesNotContain("EST-001", primera, StringComparison.Ordinal);
        Assert.True(resultado.Respuesta!.UsoDatosEstudiante);
        Assert.Contains(new Cita(Textos.DocumentoDatosEstudiante, "Notas"), resultado.Respuesta.Citas);
        Assert.Contains(new Cita(Textos.DocumentoDatosEstudiante, "Asistencia"), resultado.Respuesta.Citas);
    }

    [Fact]
    [Trait("ESC", "ESC-172")]
    [Trait("ESC", "ESC-031")]
    [Trait("CA", "CA-14")]
    [Trait("COMP", "COMP-07")]
    [Trait("RN", "RN-10")]
    public async Task EST_005_pregunta_que_materias_puede_habilitar_y_recibe_Estadistica_y_Microeconomia_en_el_resumen()
    {
        var escenario = Con(Fijo(RespuestaGenerica));

        var resultado = await escenario.PreguntarAsync(Est005, "¿Qué materias puedo habilitar?");

        var primera = escenario.Llm.TextoDeLlamada(0);
        Assert.Matches(@"Estadística.*(?<![\d.])2\.3", primera);
        Assert.Matches(@"Microeconomía.*(?<![\d.])1\.8", primera);
        Assert.DoesNotContain("Camilo Rojas", primera, StringComparison.Ordinal);
        Assert.True(resultado.Respuesta!.UsoDatosEstudiante);
        Assert.Contains(new Cita(Textos.DocumentoDatosEstudiante, "Notas"), resultado.Respuesta.Citas);
    }

    [Fact]
    [Trait("ESC", "ESC-027")]
    [Trait("CA", "CA-14")]
    [Trait("COMP", "COMP-07")]
    public async Task Las_faltas_restantes_en_Bases_de_Datos_activan_el_resumen_por_puedo_y_por_la_asignatura()
    {
        var escenario = Con(Fijo("Puedes faltar 5 clases más en Bases de Datos."));

        var resultado = await escenario.PreguntarAsync(Est001, "¿Cuántas faltas más puedo tener en Bases de Datos sin perderla?");

        Assert.Single(escenario.Llm.Solicitudes);
        Assert.Contains(MarcaEst001, escenario.Llm.TextoDeLlamada(0), StringComparison.Ordinal);
        Assert.True(resultado.Respuesta!.UsoDatosEstudiante);
        Assert.Contains(new Cita(Textos.DocumentoDatosEstudiante, "Asistencia"), resultado.Respuesta.Citas);
    }

    [Fact]
    [Trait("ESC", "ESC-029")]
    [Trait("CA", "CA-14")]
    [Trait("COMP", "COMP-07")]
    public async Task La_pregunta_de_la_beca_por_rendimiento_de_EST_004_entrega_su_resumen_con_el_promedio_3_88()
    {
        var escenario = Con(Fijo("No cumples: tu promedio 3.88 es menor que 4.3."));

        var resultado = await escenario.PreguntarAsync(Est004, "¿Puedo aplicar a la beca por rendimiento?");

        Assert.Matches(@"(?<![\d.])3\.88", escenario.Llm.TextoDeLlamada(0));
        Assert.DoesNotContain("Valentina Castro", escenario.Llm.TextoDeLlamada(0), StringComparison.Ordinal);
        Assert.True(resultado.Respuesta!.UsoDatosEstudiante);
    }

    [Fact]
    [Trait("ESC", "ESC-172")]
    [Trait("CA", "CA-14")]
    [Trait("COMP", "COMP-04")]
    [Trait("COMP", "COMP-07")]
    public async Task Una_asignatura_del_catalogo_en_la_que_no_esta_matriculado_se_verifica_con_una_lectura_y_no_entrega_resumen()
    {
        var escenario = Con(Fijo(Textos.SinInformacion));

        var resultado = await escenario.PreguntarAsync(Est001, "¿Cómo voy en Contabilidad?");

        Assert.Equal(["EST-001"], escenario.Api.LecturasNotas.ToArray());
        Assert.Empty(escenario.Api.LecturasAsistencia);
        Assert.All(Enumerable.Range(0, escenario.Llm.Solicitudes.Count), i => Assert.DoesNotContain(MarcaEst001, escenario.Llm.TextoDeLlamada(i), StringComparison.Ordinal));
        Assert.False(resultado.Respuesta!.UsoDatosEstudiante);
        Assert.DoesNotContain(resultado.Respuesta.Citas, c => c.Documento == Textos.DocumentoDatosEstudiante);
        var evento = Assert.Single(escenario.Auditoria.Eventos);
        Assert.Contains(RecursoConsultado.Notas, evento.RecursosConsultados);
        Assert.Contains(evento.DatosConsultados, d => d.EstudianteId == "EST-001" && d.Recurso == RecursoConsultado.Notas);
    }

    [Fact]
    [Trait("ESC", "ESC-172")]
    [Trait("COMP", "COMP-03")]
    [Trait("COMP", "COMP-04")]
    public async Task Si_la_asignatura_no_matriculada_deja_la_pregunta_bajo_el_umbral_responde_COMP_03_con_la_lectura_auditada()
    {
        var escenario = Con(Fijo("Respuesta del modelo."), Indice());

        var resultado = await escenario.PreguntarAsync(Est001, "¿Cómo voy en Contabilidad?");

        Assert.Empty(escenario.Llm.Solicitudes);
        Assert.Equal(Textos.SinInformacion, resultado.Respuesta!.Respuesta);
        Assert.Empty(resultado.Respuesta.Citas);
        Assert.False(resultado.Respuesta.UsoDatosEstudiante);
        Assert.Equal(0, resultado.Respuesta.Tokens);
        Assert.Equal(["EST-001"], escenario.Api.LecturasNotas.ToArray());
        Assert.Empty(escenario.Api.LecturasAsistencia);
        Assert.Contains(RecursoConsultado.Notas, Assert.Single(escenario.Auditoria.Eventos).RecursosConsultados);
    }

    [Fact]
    [Trait("ESC", "ESC-172")]
    [Trait("SEG", "SEG-04")]
    public async Task Un_tercero_junto_a_una_asignatura_se_deniega_antes_de_verificar_la_matricula()
    {
        var escenario = Con(Fijo("Respuesta del modelo."));

        var resultado = await escenario.PreguntarAsync(Est001, "¿Puedo habilitar Bases de Datos de Andrés Martínez?");

        Assert.Equal(TipoResultadoPregunta.Denegada, resultado.Tipo);
        Assert.Empty(escenario.Llm.Solicitudes);
        Assert.Empty(escenario.Api.LecturasNotas);
        Assert.Empty(escenario.Api.LecturasAsistencia);
    }

    [Theory]
    [InlineData("¿Cómo voy en Bases de Datos en el periodo 2025-1?")]
    [InlineData("¿Cómo voy en BD-203 en 2025-2?")]
    [InlineData("¿Puedo habilitar Bases de Datos en 2026-1?")]
    [Trait("ESC", "ESC-172")]
    [Trait("COMP", "COMP-03")]
    public async Task Una_asignatura_nombrada_con_otro_periodo_responde_COMP_03_sin_LLM_ni_lecturas(string pregunta)
    {
        var escenario = Con(Fijo("Respuesta del modelo."));

        var resultado = await escenario.PreguntarAsync(Est001, pregunta);

        Assert.Empty(escenario.Llm.Solicitudes);
        Assert.Equal(Textos.SinInformacion, resultado.Respuesta!.Respuesta);
        Assert.Empty(resultado.Respuesta.Citas);
        Assert.False(resultado.Respuesta.UsoDatosEstudiante);
        Assert.Equal(0, resultado.Respuesta.Tokens);
        Assert.Equal(0m, resultado.Respuesta.CostoUsd);
        Assert.Empty(escenario.Api.LecturasNotas);
        Assert.Empty(escenario.Api.LecturasAsistencia);
        Assert.Equal(ResultadoAuditoria.SinInformacion, Assert.Single(escenario.Auditoria.Eventos).Resultado);
    }

    [Fact]
    [Trait("ESC", "ESC-169")]
    [Trait("CA", "CA-06")]
    [Trait("COMP", "COMP-03")]
    public async Task Exenta_por_el_resumen_la_pregunta_de_habilitar_Bases_de_Datos_llega_al_LLM_aunque_ninguna_seccion_supere_el_umbral()
    {
        var escenario = Con(Fijo(RespuestaGenerica), Indice(Articulo6, Articulo22));

        var resultado = await escenario.PreguntarAsync(Est002, "¿Puedo habilitar Bases de Datos?");

        Assert.Single(escenario.Llm.Solicitudes);
        Assert.True(resultado.Respuesta!.UsoDatosEstudiante);
    }
}
