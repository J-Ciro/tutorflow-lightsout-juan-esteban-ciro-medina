using TutorFlow.Core.Asistente;
using TutorFlow.Core.Autorizacion;
using TutorFlow.Core.Conocimiento;
using static TutorFlow.Core.Tests.Asistente.EscenarioAsistente;
using static TutorFlow.Core.Tests.Asistente.SeccionesDeReferencia;

namespace TutorFlow.Core.Tests.Asistente;

public sealed class ApoyoYSinRespaldoTests
{
    private const string PreguntaBiblioteca = "¿Cuándo atiende la biblioteca?";
    private const string PreguntaNotaMinima = "¿Cuál es la nota mínima para aprobar una asignatura?";
    private const string PreguntaDefinitiva = "¿Con qué definitiva se aprueba la asignatura?";
    private const string RespuestaDefinitiva = "La asignatura exige una definitiva suficiente.";
    private const string PreguntaPrecio = "¿Cuánto cuesta la matrícula con la beca socioeconómica?";
    private static readonly Usuario Est001 = new(Rol.Estudiante, "EST-001");
    private static readonly Usuario Coord02 = new(Rol.Coordinador, "COORD-02");
    private static readonly Cita CitaBiblioteca = new(DocumentoDePrueba, "Artículo 1");
    private static readonly Cita DatosNotas = new(Textos.DocumentoDatosEstudiante, "Notas");
    private static readonly Cita DatosAsistencia = new(Textos.DocumentoDatosEstudiante, "Asistencia");
    private static readonly Cita DatosProgramaAdm = new(Textos.DocumentoDatosPrograma, "ADM");

    private static readonly SeccionDocumento DefinitivaA = new(Reglamento, "Artículo 30", "Definitiva", "La definitiva suficiente aprueba la asignatura.");
    private static readonly SeccionDocumento DefinitivaB = new(Reglamento, "Artículo 31", "Suficiente", "La asignatura queda aprobada con una definitiva suficiente.");
    private static readonly SeccionDocumento DefinitivaC = new(Reglamento, "Artículo 32", "Asignatura", "Una definitiva suficiente aprueba la asignatura del periodo.");
    private static readonly SeccionDocumento SoloComparteUnaRaiz = new(Reglamento, "Artículo 33", "Definitiva", "El cierre ocurre al terminar el periodo.");

    public static TheoryData<string, bool> RespuestasDeLaBiblioteca => new()
    {
        { "Los laboratorios cierran durante las vacaciones.", false },
        { "La biblioteca cierra durante las vacaciones.", false },
        { "La biblioteca atiende entre semana.", true }
    };

    public static TheoryData<string> FormasDeValorMonetario => new()
    {
        "La matrícula con la beca cuesta $2.500.000.",
        "La matrícula con la beca cuesta $ 100.",
        "La matrícula con la beca cuesta COP 100.",
        "La matrícula con la beca cuesta USD 100.",
        "La matrícula con la beca queda en 1.750.000 pesos.",
        "La matrícula con la beca queda en 100 pesos.",
        "La matrícula con la beca queda en 100 dólares.",
        "La matrícula con la beca queda en 100 dolares.",
        "La matrícula con la beca queda en 12.345.",
        "La matrícula con la beca queda en 1,500.",
        "La matrícula con la beca queda en 1.500.000."
    };

    [Theory]
    [MemberData(nameof(RespuestasDeLaBiblioteca))]
    [Trait("ESC", "ESC-168")]
    [Trait("CA", "CA-14")]
    [Trait("COMP", "COMP-02")]
    [Trait("SEG", "SEG-10e")]
    public async Task Sin_cita_escrita_el_respaldo_exige_dos_raices_compartidas_con_la_respuesta(string respuesta, bool apoyada)
    {
        var escenario = Con(Fijo(respuesta), Indice(Biblioteca));

        var resultado = await escenario.PreguntarAsync(Est001, PreguntaBiblioteca);

        if (apoyada)
        {
            AfirmarRespondida(resultado, respuesta, [CitaBiblioteca]);
        }
        else
        {
            AfirmarSinInformacionPorConsumoReal(resultado, escenario);
        }
    }

    [Theory]
    [MemberData(nameof(RespuestasDeLaBiblioteca))]
    [Trait("ESC", "ESC-170")]
    [Trait("CA", "CA-14")]
    [Trait("COMP", "COMP-02")]
    [Trait("SEG", "SEG-10a")]
    public async Task Una_cita_escrita_solo_se_conserva_si_su_seccion_comparte_dos_raices_con_la_respuesta(string respuesta, bool apoyada)
    {
        var texto = $"{respuesta} ({DocumentoDePrueba} · Artículo 1)";
        var escenario = Con(Fijo(texto), Indice(Biblioteca));

        var resultado = await escenario.PreguntarAsync(Est001, PreguntaBiblioteca);

        if (apoyada)
        {
            AfirmarRespondida(resultado, texto, [CitaBiblioteca]);
        }
        else
        {
            AfirmarSinInformacionPorConsumoReal(resultado, escenario);
        }
    }

    [Fact]
    [Trait("ESC", "ESC-170")]
    [Trait("COMP", "COMP-02")]
    [Trait("SEG", "SEG-10a")]
    public async Task El_texto_de_la_cita_escrita_no_cuenta_como_apoyo_de_la_respuesta()
    {
        var seccion = new SeccionDocumento("biblioteca_atencion.md", "Artículo 1", "Servicio", "La biblioteca ofrece atención de lunes a viernes.");
        var texto = "Los laboratorios cierran durante las vacaciones. (biblioteca_atencion.md · Artículo 1)";
        var escenario = Con(Fijo(texto), Indice(seccion));

        var resultado = await escenario.PreguntarAsync(Est001, PreguntaBiblioteca);

        AfirmarSinInformacionPorConsumoReal(resultado, escenario);
    }

    [Fact]
    [Trait("ESC", "ESC-167")]
    [Trait("CA", "CA-06")]
    [Trait("SEG", "SEG-10e")]
    public async Task Respuesta_inventada_sin_numeros_citas_ni_datos_se_reemplaza_por_COMP_03()
    {
        var escenario = Con(Fijo("Las tutorías se agendan escribiendo al buzón virtual de la decanatura."), Indice(Servicios));

        var resultado = await escenario.PreguntarAsync(Est001, "¿Cuándo hay tutorías académicas?");

        Assert.Single(escenario.Llm.Solicitudes);
        AfirmarSinInformacionPorConsumoReal(resultado, escenario);
    }

    [Fact]
    [Trait("ESC", "ESC-164")]
    [Trait("CA", "CA-14")]
    [Trait("COMP", "COMP-02")]
    public async Task Sin_citas_escritas_el_respaldo_son_las_secciones_que_comparten_dos_raices_incluidas_las_cifras()
    {
        var escenario = Con(Fijo("Una asignatura se aprueba con 3.0."), Indice(Articulo5, Articulo6, Articulo11, Articulo12, Articulo20, Articulo22));

        var resultado = await escenario.PreguntarAsync(Est001, PreguntaNotaMinima);

        AfirmarRespondida(resultado, "Una asignatura se aprueba con 3.0.", [new Cita(Reglamento, "Artículo 5"), new Cita(Reglamento, "Artículo 6")]);
        Assert.DoesNotContain(resultado.Respuesta!.Citas, c => c.Documento == Textos.DocumentoDatosEstudiante);
    }

    [Fact]
    [Trait("ESC", "ESC-164")]
    [Trait("CA", "CA-14")]
    [Trait("COMP", "COMP-02")]
    public async Task El_respaldo_de_la_nota_minima_sigue_el_orden_de_ranking_y_nunca_incluye_el_Articulo_20()
    {
        var escenario = Con(Fijo("Una asignatura se aprueba con 3.0."), Indice(Articulo20, Articulo6, Articulo5, Articulo11, Articulo12, Articulo22));

        var resultado = await escenario.PreguntarAsync(Est001, PreguntaNotaMinima);

        AfirmarRespondida(resultado, "Una asignatura se aprueba con 3.0.", [new Cita(Reglamento, "Artículo 6"), new Cita(Reglamento, "Artículo 5")]);
    }

    [Fact]
    [Trait("ESC", "ESC-164")]
    [Trait("CA", "CA-14")]
    [Trait("COMP", "COMP-02")]
    public async Task Un_entero_no_es_la_cifra_3_0_y_el_respaldo_queda_solo_en_el_Articulo_5()
    {
        var escenario = Con(Fijo("Una asignatura se aprueba con 3."), Indice(Articulo5, Articulo6, Articulo11, Articulo12, Articulo20, Articulo22));

        var resultado = await escenario.PreguntarAsync(Est001, PreguntaNotaMinima);

        AfirmarRespondida(resultado, "Una asignatura se aprueba con 3.", [new Cita(Reglamento, "Artículo 5")]);
    }

    [Fact]
    [Trait("ESC", "ESC-164")]
    [Trait("CA", "CA-14")]
    [Trait("SEG", "SEG-10a")]
    public async Task La_cita_al_reglamento_2023_se_descarta_y_el_respaldo_es_el_mismo_de_la_respuesta_sin_cita()
    {
        var texto = "Una asignatura se aprueba con 3.0. reglamento_academico_2023.md · Artículo 5";
        var escenario = Con(Fijo(texto), Indice(Articulo5, Articulo6, Articulo11, Articulo12, Articulo20, Articulo22));

        var resultado = await escenario.PreguntarAsync(Est001, PreguntaNotaMinima);

        AfirmarRespondida(resultado, texto, [new Cita(Reglamento, "Artículo 5"), new Cita(Reglamento, "Artículo 6")]);
    }

    [Fact]
    [Trait("ESC", "ESC-164")]
    [Trait("CA", "CA-14")]
    [Trait("COMP", "COMP-02")]
    public async Task Con_datos_leidos_las_citas_son_la_seccion_de_apoyo_mas_Notas_y_Asistencia()
    {
        var escenario = Con(Fijo("Bases de Datos: 2 faltas de 48 clases."), Indice(Articulo5, Articulo11, Articulo12, Articulo22));

        var resultado = await escenario.PreguntarAsync(Est001, "¿Cuántas faltas tengo en Bases de Datos?");

        var citas = resultado.Respuesta!.Citas.ToArray();
        Assert.Equal([new Cita(Reglamento, "Artículo 11")], citas.Where(c => c.Documento == Reglamento).ToArray());
        Assert.Contains(DatosNotas, citas);
        Assert.Contains(DatosAsistencia, citas);
        Assert.Equal(3, citas.Length);
        Assert.True(resultado.Respuesta.UsoDatosEstudiante);
    }

    [Fact]
    [Trait("ESC", "ESC-164")]
    [Trait("CA", "CA-14")]
    [Trait("SEG", "SEG-10e")]
    public async Task Con_datos_leidos_y_sin_seccion_de_apoyo_la_respuesta_se_entrega_solo_con_citas_de_datos()
    {
        var texto = "Bases de Datos: 2 faltas de 48 clases.";
        var escenario = Con(Fijo(texto), Indice(Servicios));

        var resultado = await escenario.PreguntarAsync(Est001, "¿Cuántas faltas tengo en Bases de Datos?");

        Assert.Equal(ResultadoAuditoria.Respondida, resultado.Auditoria);
        Assert.Equal(texto, resultado.Respuesta!.Respuesta);
        Assert.Equal(2, resultado.Respuesta.Citas.Count);
        Assert.Contains(DatosNotas, resultado.Respuesta.Citas);
        Assert.Contains(DatosAsistencia, resultado.Respuesta.Citas);
        Assert.True(resultado.Respuesta.UsoDatosEstudiante);
    }

    [Fact]
    [Trait("ESC", "ESC-164")]
    [Trait("COMP", "COMP-02")]
    public async Task El_respaldo_agrega_como_maximo_dos_secciones_en_orden_de_ranking()
    {
        var escenario = Con(Fijo(RespuestaDefinitiva), Indice(Indexadas(DefinitivaA, DefinitivaB, DefinitivaC), [DefinitivaC, DefinitivaA, DefinitivaB]));

        var resultado = await escenario.PreguntarAsync(Est001, PreguntaDefinitiva);

        Assert.Equal([CitaDe(DefinitivaC), CitaDe(DefinitivaA)], resultado.Respuesta!.Citas.ToArray());
    }

    [Fact]
    [Trait("ESC", "ESC-164")]
    [Trait("COMP", "COMP-02")]
    public async Task El_respaldo_salta_las_secciones_entregadas_que_no_cumplen_la_condicion_de_apoyo()
    {
        var escenario = Con(Fijo(RespuestaDefinitiva), Indice(Indexadas(DefinitivaA, DefinitivaB, SoloComparteUnaRaiz), [SoloComparteUnaRaiz, DefinitivaA, DefinitivaB]));

        var resultado = await escenario.PreguntarAsync(Est001, PreguntaDefinitiva);

        Assert.Equal([CitaDe(DefinitivaA), CitaDe(DefinitivaB)], resultado.Respuesta!.Citas.ToArray());
    }

    [Fact]
    [Trait("ESC", "ESC-164")]
    [Trait("COMP", "COMP-02")]
    public async Task El_respaldo_con_una_sola_seccion_que_cumple_es_esa_seccion()
    {
        var escenario = Con(Fijo(RespuestaDefinitiva), Indice(Indexadas(DefinitivaA, SoloComparteUnaRaiz), [SoloComparteUnaRaiz, DefinitivaA]));

        var resultado = await escenario.PreguntarAsync(Est001, PreguntaDefinitiva);

        Assert.Equal([CitaDe(DefinitivaA)], resultado.Respuesta!.Citas.ToArray());
    }

    [Fact]
    [Trait("ESC", "ESC-164")]
    [Trait("COMP", "COMP-02")]
    [Trait("SEG", "SEG-10a")]
    public async Task Una_cita_escrita_valida_se_conserva_y_el_respaldo_no_se_agrega()
    {
        var texto = $"{RespuestaDefinitiva} ({Reglamento} · Artículo 31)";
        var escenario = Con(Fijo(texto), Indice(DefinitivaA, DefinitivaB, DefinitivaC));

        var resultado = await escenario.PreguntarAsync(Est001, PreguntaDefinitiva);

        Assert.Equal([CitaDe(DefinitivaB)], resultado.Respuesta!.Citas.ToArray());
    }

    [Fact]
    [Trait("ESC", "ESC-164")]
    [Trait("COMP", "COMP-02")]
    [Trait("SEG", "SEG-10a")]
    public async Task Una_cita_escrita_sin_apoyo_se_descarta_y_el_respaldo_usa_las_secciones_que_si_cumplen()
    {
        var texto = $"{RespuestaDefinitiva} ({Reglamento} · Artículo 33)";
        var escenario = Con(Fijo(texto), Indice(Indexadas(DefinitivaA, DefinitivaB, SoloComparteUnaRaiz), [SoloComparteUnaRaiz, DefinitivaA, DefinitivaB]));

        var resultado = await escenario.PreguntarAsync(Est001, PreguntaDefinitiva);

        Assert.Equal([CitaDe(DefinitivaA), CitaDe(DefinitivaB)], resultado.Respuesta!.Citas.ToArray());
    }

    [Fact]
    [Trait("ESC", "ESC-170")]
    [Trait("CA", "CA-14")]
    [Trait("COMP", "COMP-02")]
    public async Task Sin_lectura_de_datos_una_cita_a_una_seccion_de_reglas_aplicadas_sin_apoyo_se_descarta()
    {
        var evaluacion = new SeccionDocumento(Evaluacion, "Sección 3", "Definitiva", "Texto sin relación alguna.");
        var texto = $"La biblioteca atiende entre semana. ({Evaluacion} · Sección 3)";
        var escenario = Con(Fijo(texto), Indice(Biblioteca, evaluacion));

        var resultado = await escenario.PreguntarAsync(Est001, PreguntaBiblioteca);

        Assert.Equal([CitaBiblioteca], resultado.Respuesta!.Citas.ToArray());
    }

    [Fact]
    [Trait("ESC", "ESC-170")]
    [Trait("CA", "CA-14")]
    [Trait("COMP", "COMP-02")]
    public async Task Con_lectura_de_datos_la_cita_a_una_seccion_de_reglas_aplicadas_se_conserva_aunque_no_comparta_raices()
    {
        var evaluacion = new SeccionDocumento(Evaluacion, "Sección 3", "Definitiva", "Texto sin relación alguna.");
        var texto = $"Tu promedio del periodo es 4.39 ({Evaluacion} · Sección 3).";
        var escenario = Con(Fijo(texto), Indice(Indexadas(evaluacion), []));

        var resultado = await escenario.PreguntarAsync(Est001, "¿Cuál es mi promedio?");

        var citas = resultado.Respuesta!.Citas.ToArray();
        Assert.Equal([new Cita(Evaluacion, "Sección 3")], citas.Where(c => c.Documento == Evaluacion).ToArray());
        Assert.Contains(DatosNotas, citas);
        Assert.Contains(DatosAsistencia, citas);
    }

    [Fact]
    [Trait("ESC", "ESC-170")]
    [Trait("CA", "CA-14")]
    [Trait("COMP", "COMP-02")]
    public async Task Con_lectura_de_datos_la_cita_a_una_seccion_fuera_de_reglas_aplicadas_y_sin_apoyo_se_descarta()
    {
        var texto = $"Tu promedio del periodo es 4.39 ({Reglamento} · Artículo 22).";
        var escenario = Con(Fijo(texto), Indice(Indexadas(Articulo22), []));

        var resultado = await escenario.PreguntarAsync(Est001, "¿Cuál es mi promedio?");

        Assert.DoesNotContain(resultado.Respuesta!.Citas, c => c.Seccion == "Artículo 22");
        Assert.Contains(DatosNotas, resultado.Respuesta.Citas);
    }

    [Fact]
    [Trait("ESC", "ESC-170")]
    [Trait("CA", "CA-14")]
    [Trait("COMP", "COMP-02")]
    public async Task Con_la_herramienta_de_programa_la_cita_del_bloque_de_reglas_aplicadas_se_conserva()
    {
        var texto = $"Valentina Castro: Microeconomía (1.8) y Estadística (2.3). ({Reglamento} · Artículo 5)";
        var escenario = Con(GuionDePrograma(texto), Indice(Indexadas(Articulo5, Articulo22), [Articulo22]));

        var resultado = await escenario.PreguntarAsync(Coord02, "¿Qué estudiantes de mi programa tienen alguna asignatura perdida?");

        AfirmarRespondida(resultado, texto, [new Cita(Reglamento, "Artículo 5"), DatosProgramaAdm]);
        Assert.True(resultado.Respuesta!.UsoDatosEstudiante);
    }

    [Fact]
    [Trait("ESC", "ESC-170")]
    [Trait("CA", "CA-14")]
    [Trait("COMP", "COMP-02")]
    public async Task Con_la_herramienta_de_programa_la_cita_fuera_del_bloque_y_sin_apoyo_se_descarta_sin_reemplazar_la_respuesta()
    {
        var texto = $"Valentina Castro: Microeconomía (1.8) y Estadística (2.3). ({Reglamento} · Artículo 22)";
        var escenario = Con(GuionDePrograma(texto), Indice(Indexadas(Articulo5, Articulo22), [Articulo22]));

        var resultado = await escenario.PreguntarAsync(Coord02, "¿Qué estudiantes de mi programa tienen alguna asignatura perdida?");

        AfirmarRespondida(resultado, texto, [DatosProgramaAdm]);
        Assert.True(resultado.Respuesta!.UsoDatosEstudiante);
    }

    [Theory]
    [MemberData(nameof(FormasDeValorMonetario))]
    [Trait("ESC", "ESC-166")]
    [Trait("CA", "CA-06")]
    [Trait("SEG", "SEG-10c")]
    public async Task Un_valor_monetario_que_no_esta_en_las_fuentes_se_reemplaza_por_COMP_03_con_auditoria_sin_informacion(string respuesta)
    {
        var escenario = Con(Fijo(respuesta), Indice(BecaPorRendimiento, BecaSocioeconomica));

        var resultado = await escenario.PreguntarAsync(Est001, PreguntaPrecio);

        AfirmarSinInformacionPorConsumoReal(resultado, escenario);
    }

    [Fact]
    [Trait("ESC", "ESC-166")]
    [Trait("CA", "CA-06")]
    [Trait("COMP", "COMP-02")]
    public async Task El_descuento_en_porcentaje_de_una_seccion_entregada_se_responde_con_la_seccion_como_respaldo()
    {
        var texto = "La beca de apoyo socioeconómico da un descuento de hasta el 30%.";
        var escenario = Con(Fijo(texto), Indice(BecaPorRendimiento, BecaSocioeconomica));

        var resultado = await escenario.PreguntarAsync(Est001, PreguntaPrecio);

        Assert.Equal(ResultadoAuditoria.Respondida, resultado.Auditoria);
        Assert.Equal(texto, resultado.Respuesta!.Respuesta);
        Assert.Contains(new Cita(Bienestar, "Sección 3.2"), resultado.Respuesta.Citas);
        Assert.False(resultado.Respuesta.UsoDatosEstudiante);
    }

    [Fact]
    [Trait("ESC", "ESC-166")]
    [Trait("SEG", "SEG-10c")]
    public async Task Un_valor_monetario_presente_en_una_seccion_entregada_no_se_reemplaza()
    {
        var tarifas = new SeccionDocumento(Reglamento, "Artículo 40", "Tarifas", "La inscripción cuesta $50.000 al año.");
        var texto = "La inscripción cuesta $50.000 al año.";
        var escenario = Con(Fijo(texto), Indice(tarifas));

        var resultado = await escenario.PreguntarAsync(Est001, "¿Cuánto cuesta la inscripción?");

        AfirmarRespondida(resultado, texto, [new Cita(Reglamento, "Artículo 40")]);
    }

    private static Func<SolicitudLlm, int, RespuestaLlm> GuionDePrograma(string textoFinal) =>
        (_, llamada) => llamada == 0 ? Herramienta(HerramientasAsistente.Programa) : Texto(textoFinal);

    private static SeccionDocumento[] Indexadas(params SeccionDocumento[] secciones) => secciones;

    private static Cita CitaDe(SeccionDocumento seccion) => new(seccion.Documento, seccion.Seccion);

    private static void AfirmarRespondida(ResultadoPregunta resultado, string texto, Cita[] citas)
    {
        Assert.Equal(ResultadoAuditoria.Respondida, resultado.Auditoria);
        Assert.Equal(texto, resultado.Respuesta!.Respuesta);
        Assert.Equal(citas, resultado.Respuesta.Citas.ToArray());
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
