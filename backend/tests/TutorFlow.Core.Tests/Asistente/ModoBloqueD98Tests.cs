using TutorFlow.Core.Asistente;
using TutorFlow.Core.Autorizacion;
using TutorFlow.Core.Conocimiento;
using TutorFlow.Pruebas.Soporte;
using static TutorFlow.Core.Tests.Asistente.EscenarioAsistente;
using static TutorFlow.Core.Tests.Asistente.SeccionesDeReferencia;

namespace TutorFlow.Core.Tests.Asistente;

public sealed class ModoBloqueD98Tests
{
    private const string PreguntaQ10Dos = "¿Cuál es mi promedio ponderado del periodo?";
    private const string PreguntaQ10Tres = "¿Cuántas faltas tengo en Bases de Datos?";
    private const string PreguntaQ10Cinco = "¿Qué nota definitiva tengo en Contabilidad?";
    private const string PreguntaQ10Seis = "¿Qué estudiantes de mi programa tienen alguna asignatura perdida?";
    private const string PreguntaQ10Ocho = "¿Qué requisitos tiene la beca por rendimiento académico?";
    private const string PreguntaQ10Uno = "¿Cuál es la nota mínima para aprobar una asignatura?";
    private const string PreguntaQ10Cuatro = "¿Cómo se calcula la nota definitiva de una asignatura?";
    private const string PreguntaMisMaterias = "¿Cómo voy en mis materias?";
    private const string PreguntaPrueba = "¿Estoy en prueba académica?";
    private const string PreguntaPromedio = "¿Cuál es mi promedio?";
    private const string EncabezadoLaura = "Datos de Laura Gómez (EST-001)";
    private const string EncabezadoAndres = "Datos de Andrés Martínez (EST-002)";
    private const string EncabezadoCamilo = "Datos de Camilo Rojas (EST-004)";
    private const string LineaPerdidasAdm = "Estudiantes con asignaturas perdidas: Valentina Castro (Microeconomía 1.8; Estadística 2.3)";
    private const string BasesDeLaura = "Bases de Datos: faltas 2 de 48 clases programadas (faltas restantes: 5; máximo sin perder: 7). Si superas 7 inasistencias pierdes la asignatura por inasistencia.";
    private static readonly Usuario Est001 = new(Rol.Estudiante, "EST-001");
    private static readonly Usuario Est002 = new(Rol.Estudiante, "EST-002");
    private static readonly Usuario Est004 = new(Rol.Estudiante, "EST-004");
    private static readonly Usuario Est005 = new(Rol.Estudiante, "EST-005");
    private static readonly Usuario Coord01 = new(Rol.Coordinador, "COORD-01");
    private static readonly Usuario Coord02 = new(Rol.Coordinador, "COORD-02");
    private static readonly Cita Art5 = new(Reglamento, "Artículo 5");
    private static readonly Cita Art6 = new(Reglamento, "Artículo 6");
    private static readonly Cita Art11 = new(Reglamento, "Artículo 11");
    private static readonly Cita Art20 = new(Reglamento, "Artículo 20");
    private static readonly Cita Sec3 = new(Evaluacion, "Sección 3");
    private static readonly Cita Sec4 = new(Evaluacion, "Sección 4");
    private static readonly Cita Beca31 = new(Bienestar, "Sección 3.1");
    private static readonly Cita DatosAsistencia = new(Textos.DocumentoDatosEstudiante, "Asistencia");
    private static readonly Cita DatosProgramaAdm = new(Textos.DocumentoDatosPrograma, "ADM");

    private static readonly SeccionDocumento[] Catalogo =
        [Articulo5, Articulo6, Articulo7, Articulo11, Articulo12, Articulo20, Articulo22, NotaDefinitiva, PromedioPonderado, Cortes, Atencion, BecaPorRendimiento];

    public static TheoryData<string> FrasesDeS01SinLaura => new()
    {
        "Andrés Martínez aprobó todas sus asignaturas y no está en prueba académica.",
        "Andrés no perdió Cálculo Integral.",
        "no ha perdido ninguna materia",
        "lleva nueve faltas en Cálculo Integral",
        "promedio de cuatro punto tres nueve",
        "Pasaste Cálculo Integral.",
        "Tienes beca por rendimiento."
    };

    public static TheoryData<string> FrasesDeS02 => new()
    {
        "Laura Gómez si está en prueba académica.",
        "Para Laura Gómez se requiere 2.28 en Cálculo Integral; tiene 2.28.",
        "La definitiva mínima es 2.28 para Laura Gómez."
    };

    public static TheoryData<string> FrasesDeS03 => new()
    {
        "Estas son las faltas de Andrés Martínez:",
        "Andrés Martínez nunca ha faltado a clase."
    };

    public static TheoryData<string, string, string, string, string, string, string> FilasDeCa17 => new()
    {
        { "EST-002", PreguntaPrueba, "Andrés Martínez aprobó todas sus asignaturas y no está en prueba académica.", EncabezadoAndres, "Prueba académica: sí", "aprobó todas", "" },
        { "EST-001", PreguntaPrueba, "Laura Gómez si está en prueba académica.", EncabezadoLaura, "Prueba académica: no", "si está", "" },
        { "COORD-01", "¿Cuántas faltas tiene Laura Gómez?", "Estas son las faltas de Andrés Martínez:", EncabezadoLaura, "Total: 4 faltas", "Andrés Martínez", "" },
        { "COORD-02", PreguntaQ10Seis, "Mateo Vargas perdió Estadística.", "Estudiantes con asignaturas perdidas", "Valentina Castro (Microeconomía 1.8; Estadística 2.3)", "Mateo Vargas", "" },
        { "EST-002", PreguntaPromedio, "Tu promedio es de cuatro punto tres nueve.", EncabezadoAndres, "Promedio ponderado del periodo: 2.28", "cuatro punto", "" }
    };

    public static TheoryData<string, string, string, string, string[], bool> FilasDeCa14 => new()
    {
        { "EST-001", PreguntaQ10Dos, "Tu promedio es 4.39.", "", ["evaluacion_y_calificaciones.md · Sección 4", "evaluacion_y_calificaciones.md · Sección 3"], true },
        { "EST-004", PreguntaQ10Cinco, "Tu definitiva en Contabilidad es 3.9 (reglamento_academico_2025.md · Artículo 5).", "", ["evaluacion_y_calificaciones.md · Sección 3"], true },
        { "COORD-02", PreguntaQ10Seis, "Este es el listado.", "", ["reglamento_academico_2025.md · Artículo 5"], true },
        { "EST-005", PreguntaPrueba, "Sí, estás en prueba académica: tu promedio es 2.65.", "", ["reglamento_academico_2025.md · Artículo 20", "evaluacion_y_calificaciones.md · Sección 4"], true },
        { "EST-004", PreguntaQ10Ocho, "Requisitos: promedio de 4.3 o más, ninguna asignatura perdida y 12 créditos.", "", ["bienestar_y_becas.md · Sección 3.1"], false }
    };

    private static IndiceFijo IndiceDeReglas(params SeccionDocumento[] entregadas) => Indice(Catalogo, entregadas);

    private static Usuario Usuario(string id) => new(id.StartsWith("COORD", StringComparison.Ordinal) ? Rol.Coordinador : Rol.Estudiante, id);

    private static string Norm(string respuesta) => respuesta.Replace("\r\n", "\n", StringComparison.Ordinal);

    private static string[] Lineas(string respuesta) => Norm(respuesta).Split('\n', StringSplitOptions.RemoveEmptyEntries);

    private static string[] Documentos(ResultadoPregunta r) =>
        [.. r.Respuesta!.Citas.Where(c => c.Documento is not (Textos.DocumentoDatosEstudiante or Textos.DocumentoDatosPrograma)).Select(c => $"{c.Documento} · {c.Seccion}")];

    private static void AfirmarUnaLlamadaSinHerramientas(EscenarioAsistente escenario)
    {
        var solicitud = Assert.Single(escenario.Llm.Solicitudes);
        Assert.Empty(solicitud.Herramientas);
    }

    private static void AfirmarRespondidaSinReintento(ResultadoPregunta resultado, EscenarioAsistente escenario)
    {
        Assert.Equal(ResultadoAuditoria.Respondida, resultado.Auditoria);
        Assert.False(ReintentoDeLlm.ReintentoRegistrado(Assert.Single(escenario.Auditoria.Eventos)));
    }

    [Theory]
    [MemberData(nameof(FrasesDeS01SinLaura))]
    [Trait("CA", "CA-17")]
    [Trait("ESC", "ESC-189")]
    [Trait("D", "D-98")]
    public async Task ESC_189_q1_Ninguna_de_las_7_frases_de_S_01_llega_y_la_respuesta_es_solo_el_bloque_de_estado(string frase)
    {
        var escenario = Con(Fijo(frase), IndiceDeReglas(Articulo5, Articulo6));

        var resultado = await escenario.PreguntarAsync(Est002, PreguntaMisMaterias);

        var lineas = Lineas(resultado.Respuesta!.Respuesta);
        Assert.Equal(
            [
                EncabezadoAndres,
                "Estado de Cálculo Integral: perdida con definitiva 2.6, habilitable",
                "Estado de Programación Orientada a Objetos: aprobada con definitiva 3.5",
                "Estado de Bases de Datos: perdida por inasistencia, no habilitable",
                "Estado de Inglés III: aprobada con definitiva 3.2"
            ],
            lineas);
        AfirmarUnaLlamadaSinHerramientas(escenario);
        AfirmarRespondidaSinReintento(resultado, escenario);
    }

    [Fact]
    [Trait("CA", "CA-17")]
    [Trait("ESC", "ESC-189")]
    [Trait("SEG", "SEG-10b")]
    public async Task ESC_189_q2_La_frase_con_Laura_Gomez_da_la_negativa_del_estudiante_con_fuga_bloqueada()
    {
        var escenario = Con(Fijo("Laura Gómez reprobó Cálculo."), IndiceDeReglas(Articulo5));

        var resultado = await escenario.PreguntarAsync(Est002, PreguntaMisMaterias);

        Assert.Equal(Textos.NegativaEstudiante, resultado.Respuesta!.Respuesta);
        Assert.Equal(ResultadoAuditoria.FugaBloqueada, resultado.Auditoria);
        Assert.Single(escenario.Llm.Solicitudes);
    }

    [Theory]
    [MemberData(nameof(FrasesDeS02))]
    [Trait("CA", "CA-17")]
    [Trait("ESC", "ESC-189")]
    [Trait("D", "D-98")]
    public async Task ESC_189_r_Las_3_frases_de_S_02_se_podan_y_la_respuesta_es_exactamente_el_bloque_de_prueba_academica(string frase)
    {
        var escenario = Con(Fijo(frase), IndiceDeReglas(Articulo20, PromedioPonderado));

        var resultado = await escenario.PreguntarAsync(Est001, PreguntaPrueba);

        Assert.Equal($"{EncabezadoLaura}\nPromedio ponderado del periodo: 4.39\nPrueba académica: no", Norm(resultado.Respuesta!.Respuesta));
        Assert.DoesNotContain("2.28", resultado.Respuesta.Respuesta, StringComparison.Ordinal);
        AfirmarUnaLlamadaSinHerramientas(escenario);
        AfirmarRespondidaSinReintento(resultado, escenario);
    }

    [Theory]
    [MemberData(nameof(FrasesDeS03))]
    [Trait("CA", "CA-17")]
    [Trait("ESC", "ESC-189")]
    [Trait("SEG", "SEG-02")]
    public async Task ESC_189_s_La_oracion_sobre_otra_persona_autorizada_se_poda_y_queda_el_bloque_de_Laura_en_tercera_persona(string frase)
    {
        var escenario = Con(Fijo(frase), IndiceDeReglas(Articulo11, Articulo12));

        var resultado = await escenario.PreguntarAsync(Coord01, "¿Cuántas faltas tiene Laura Gómez?");

        var lineas = Lineas(resultado.Respuesta!.Respuesta);
        Assert.Equal(EncabezadoLaura, lineas[0]);
        Assert.Contains("Cálculo Integral: faltas 1 de 64 clases programadas (faltas restantes: 8; máximo sin perder: 9). Si supera 9 inasistencias pierde la asignatura por inasistencia.", lineas);
        Assert.Contains("Bases de Datos: faltas 2 de 48 clases programadas (faltas restantes: 5; máximo sin perder: 7). Si supera 7 inasistencias pierde la asignatura por inasistencia.", lineas);
        Assert.Equal("Total: 4 faltas", lineas[^1]);
        Assert.Equal(6, lineas.Length);
        Assert.DoesNotContain("Andrés", resultado.Respuesta.Respuesta, StringComparison.Ordinal);
        Assert.All(escenario.Api.LecturasNotas.Concat(escenario.Api.LecturasAsistencia), id => Assert.Equal("EST-001", id));
        AfirmarUnaLlamadaSinHerramientas(escenario);
    }

    [Fact]
    [Trait("CA", "CA-17")]
    [Trait("ESC", "ESC-189")]
    [Trait("COMP", "COMP-09")]
    public async Task ESC_189_t_P6_conserva_la_introduccion_de_lista_cerrada_y_el_bloque_empieza_por_las_perdidas_con_1_8_y_2_3()
    {
        var escenario = Con(Fijo("Este es el listado."), IndiceDeReglas(Articulo5));

        var resultado = await escenario.PreguntarAsync(Coord02, PreguntaQ10Seis);

        Assert.StartsWith($"Este es el listado.\n\n{LineaPerdidasAdm}", Norm(resultado.Respuesta!.Respuesta), StringComparison.Ordinal);
        Assert.Equal([Art5, DatosProgramaAdm], resultado.Respuesta.Citas);
        Assert.DoesNotContain(resultado.Respuesta.Citas, c => c.Seccion == "Sección 3");
        AfirmarUnaLlamadaSinHerramientas(escenario);
    }

    [Theory]
    [InlineData("¿Qué estudiantes de mi programa tienen alguna asignatura perdida?")]
    [Trait("CA", "CA-01")]
    [Trait("ESC", "ESC-189")]
    [Trait("D", "D-98")]
    public async Task ESC_189_t_P6_trae_1_8_y_2_3_sin_la_palabra_definitiva_aunque_el_modelo_no_escriba_nada_util(string pregunta)
    {
        var escenario = Con(Fijo("Valentina Castro: Microeconomía (1.8) y Estadística (2.3)."), IndiceDeReglas(Articulo5));

        var resultado = await escenario.PreguntarAsync(Coord02, pregunta);

        var respuesta = Norm(resultado.Respuesta!.Respuesta);
        Assert.Equal(LineaPerdidasAdm, respuesta);
        Assert.DoesNotContain("definitiva", respuesta, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("EST-004", PreguntaQ10Ocho)]
    [InlineData("EST-001", PreguntaQ10Uno)]
    [InlineData("EST-001", PreguntaQ10Cuatro)]
    [Trait("CA", "CA-14")]
    [Trait("ESC", "ESC-189")]
    [Trait("D", "D-98")]
    public async Task ESC_189_u_Sin_activador_no_hay_bloque_ni_lecturas_ni_uso_de_datos(string id, string pregunta)
    {
        var escenario = Con(Fijo("Una asignatura se aprueba con 3.0 o más (reglamento_academico_2025.md · Artículo 5)."), IndiceDeReglas(Articulo5, NotaDefinitiva, Cortes, BecaPorRendimiento));

        var resultado = await escenario.PreguntarAsync(Usuario(id), pregunta);

        Assert.DoesNotContain("Datos de", resultado.Respuesta!.Respuesta, StringComparison.Ordinal);
        Assert.Empty(escenario.Api.LecturasNotas);
        Assert.Empty(escenario.Api.LecturasAsistencia);
        Assert.False(resultado.Respuesta.UsoDatosEstudiante);
        Assert.DoesNotContain(resultado.Respuesta.Citas, c => c.Documento == Textos.DocumentoDatosEstudiante);
    }

    [Fact]
    [Trait("CA", "CA-14")]
    [Trait("ESC", "ESC-189")]
    [Trait("D", "D-101")]
    public async Task ESC_189_v_P5_poda_la_oracion_y_la_respuesta_es_el_bloque_de_definitiva_con_solo_la_Seccion_3()
    {
        var escenario = Con(Fijo("Tu definitiva en Contabilidad es 3.9 (reglamento_academico_2025.md · Artículo 5)."), IndiceDeReglas(Articulo5, NotaDefinitiva, Cortes));

        var resultado = await escenario.PreguntarAsync(Est004, PreguntaQ10Cinco);

        Assert.Equal($"{EncabezadoCamilo}\nDefinitivas: Contabilidad 3.9", Norm(resultado.Respuesta!.Respuesta));
        Assert.Equal(["evaluacion_y_calificaciones.md · Sección 3"], Documentos(resultado));
        Assert.DoesNotContain(Art5, resultado.Respuesta.Citas);
    }

    [Fact]
    [Trait("CA", "CA-17")]
    [Trait("ESC", "ESC-189")]
    [Trait("RN", "RN-10")]
    public async Task ESC_189_w1_Queda_la_oracion_normativa_citada_y_se_poda_la_de_segunda_persona()
    {
        var escenario = Con(Fijo("Una asignatura se pierde con definitiva menor a 3.0 (reglamento_academico_2025.md · Artículo 5). Perdiste por poco."), IndiceDeReglas(Articulo5, Articulo6));

        var resultado = await escenario.PreguntarAsync(Est002, "¿Por qué perdí Cálculo Integral?");

        var respuesta = Norm(resultado.Respuesta!.Respuesta);
        Assert.StartsWith("Una asignatura se pierde con definitiva menor a 3.0.", respuesta, StringComparison.Ordinal);
        Assert.DoesNotContain("Perdiste por poco", respuesta, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("CA", "CA-17")]
    [Trait("ESC", "ESC-189")]
    [Trait("RN", "RN-10")]
    public async Task ESC_189_w2_La_pregunta_mixta_trae_el_estado_de_Calculo_Integral_con_los_Articulos_5_y_6()
    {
        var escenario = Con(Fijo("Una asignatura se pierde con definitiva menor a 3.0 (reglamento_academico_2025.md · Artículo 5). Perdiste por poco."), IndiceDeReglas(Articulo5, Articulo6));

        var resultado = await escenario.PreguntarAsync(Est002, "¿Por qué perdí Cálculo Integral?");

        Assert.Contains("Estado de Cálculo Integral: perdida con definitiva 2.6, habilitable", Lineas(resultado.Respuesta!.Respuesta));
        Assert.Contains(Art5, resultado.Respuesta.Citas);
        Assert.Contains(Art6, resultado.Respuesta.Citas);
    }

    [Theory]
    [InlineData("EST-001", PreguntaPromedio)]
    [InlineData("COORD-02", PreguntaQ10Seis)]
    [Trait("CA", "CA-19")]
    [Trait("ESC", "ESC-189")]
    [Trait("SEG", "SEG-06")]
    public async Task ESC_189_x_Un_modelo_que_intenta_invocar_herramientas_no_las_recibe_ni_lee_a_otros_estudiantes(string id, string pregunta)
    {
        var escenario = Con((_, _) => Herramienta(HerramientasAsistente.Programa), IndiceDeReglas(Articulo5));

        await escenario.PreguntarAsync(Usuario(id), pregunta);

        Assert.All(escenario.Llm.Solicitudes, s => Assert.Empty(s.Herramientas));
        var lecturas = escenario.Api.LecturasNotas.Concat(escenario.Api.LecturasAsistencia).Distinct().ToList();
        string[] permitidos = id == "EST-001" ? ["EST-001"] : ["EST-004", "EST-005", "EST-006"];
        Assert.All(lecturas, lectura => Assert.Contains(lectura, permitidos));
    }

    [Theory]
    [InlineData("Estos son tus datos:", true)]
    [InlineData("Estos son los datos de Laura Gómez.", true)]
    [InlineData("Estos son los datos de Andrés Martínez.", false)]
    [Trait("CA", "CA-17")]
    [Trait("ESC", "ESC-189")]
    [Trait("SEG", "SEG-10b")]
    public async Task ESC_189_y_Las_introducciones_de_la_lista_cerrada_se_conservan_y_la_de_otra_persona_da_NEG(string texto, bool seConserva)
    {
        var escenario = Con(Fijo(texto), IndiceDeReglas(PromedioPonderado, NotaDefinitiva));

        var resultado = await escenario.PreguntarAsync(Est001, PreguntaPromedio);

        if (seConserva)
        {
            Assert.StartsWith(texto, Norm(resultado.Respuesta!.Respuesta), StringComparison.Ordinal);
            Assert.Contains(EncabezadoLaura, resultado.Respuesta.Respuesta, StringComparison.Ordinal);
            return;
        }

        Assert.Equal(Textos.NegativaEstudiante, resultado.Respuesta!.Respuesta);
        Assert.Equal(ResultadoAuditoria.FugaBloqueada, resultado.Auditoria);
    }

    [Theory]
    [InlineData("Se registran nueve faltas (reglamento_academico_2025.md · Artículo 11).")]
    [InlineData("Bases de Datos se aprueba con 3.0 (reglamento_academico_2025.md · Artículo 5).")]
    [Trait("CA", "CA-17")]
    [Trait("ESC", "ESC-189")]
    [Trait("D", "D-98")]
    public async Task ESC_189_z_La_oracion_citada_con_numero_en_letras_o_asignatura_del_catalogo_se_poda(string texto)
    {
        var escenario = Con(Fijo(texto), IndiceDeReglas(Articulo5, Articulo11, Articulo12));

        var resultado = await escenario.PreguntarAsync(Est001, PreguntaQ10Tres);

        var lineas = Lineas(resultado.Respuesta!.Respuesta);
        Assert.Equal(EncabezadoLaura, lineas[0]);
        Assert.Contains(BasesDeLaura, lineas);
        Assert.DoesNotContain("nueve", resultado.Respuesta.Respuesta, StringComparison.Ordinal);
        Assert.DoesNotContain("se aprueba", resultado.Respuesta.Respuesta, StringComparison.Ordinal);
        AfirmarUnaLlamadaSinHerramientas(escenario);
    }

    [Fact]
    [Trait("CA", "CA-17")]
    [Trait("ESC", "ESC-189")]
    [Trait("SEG", "SEG-10c")]
    public async Task ESC_189_aa_La_norma_con_15_por_ciento_se_conserva_y_pasa_la_verificacion_de_cifras()
    {
        var escenario = Con(Fijo("Se pierde con más del 15% de inasistencias (reglamento_academico_2025.md · Artículo 11)."), IndiceDeReglas(Articulo11, Articulo12));

        var resultado = await escenario.PreguntarAsync(Est001, PreguntaQ10Tres);

        Assert.StartsWith("Se pierde con más del 15% de inasistencias.", Norm(resultado.Respuesta!.Respuesta), StringComparison.Ordinal);
        Assert.Equal(ResultadoAuditoria.Respondida, resultado.Auditoria);
    }

    [Fact]
    [Trait("CA", "CA-17")]
    [Trait("ESC", "ESC-189")]
    [Trait("SEG", "SEG-10c")]
    public async Task ESC_189_aa_La_norma_con_20_por_ciento_da_numero_no_verificado_con_reintento()
    {
        var escenario = Con(Fijo("Se pierde con más del 20% de inasistencias (reglamento_academico_2025.md · Artículo 11)."), IndiceDeReglas(Articulo11, Articulo12));

        var resultado = await escenario.PreguntarAsync(Est001, PreguntaQ10Tres);

        Assert.Equal(Textos.NoVerificable, resultado.Respuesta!.Respuesta);
        Assert.Equal(ResultadoAuditoria.NumeroNoVerificado, resultado.Auditoria);
        Assert.Equal(2, escenario.Llm.Solicitudes.Count);
        Assert.True(ReintentoDeLlm.ReintentoRegistrado(Assert.Single(escenario.Auditoria.Eventos)));
    }

    [Fact]
    [Trait("CA", "CA-17")]
    [Trait("ESC", "ESC-189")]
    [Trait("D", "D-98")]
    public async Task ESC_189_ab_La_asignatura_sin_matricula_lleva_la_linea_de_sin_matricula_y_ninguna_cifra()
    {
        var escenario = Con(Fijo("Esto es lo que encuentro."), IndiceDeReglas(NotaDefinitiva));

        var resultado = await escenario.PreguntarAsync(Est004, "¿Cuál es mi nota en Cálculo Integral?");

        Assert.Equal($"{EncabezadoCamilo}\nSin matrícula en el periodo 2026-2: Cálculo Integral", Norm(resultado.Respuesta!.Respuesta));
        AfirmarUnaLlamadaSinHerramientas(escenario);
    }

    [Fact]
    [Trait("CA", "CA-27")]
    [Trait("ESC", "ESC-189")]
    [Trait("SEG", "SEG-02")]
    public async Task ESC_189_n1_Un_estudiante_nombrado_da_su_bloque_sin_prelectura_ni_herramientas()
    {
        var escenario = Con(Fijo("Este es el detalle de Andrés Martínez."), IndiceDeReglas(Articulo11, Articulo12));

        var resultado = await escenario.PreguntarAsync(Coord01, "¿Cuántas faltas tiene Andrés Martínez?");

        var lineas = Lineas(resultado.Respuesta!.Respuesta);
        Assert.Equal("Este es el detalle de Andrés Martínez.", lineas[0]);
        Assert.Equal(EncabezadoAndres, lineas[1]);
        Assert.Contains("Bases de Datos: faltas 9 de 48 clases programadas (faltas restantes: 0; máximo sin perder: 7). Perdida por inasistencia: superó 7 inasistencias.", lineas);
        Assert.Equal("Total: 15 faltas", lineas[^1]);
        Assert.NotEmpty(escenario.Api.LecturasNotas);
        Assert.NotEmpty(escenario.Api.LecturasAsistencia);
        Assert.All(escenario.Api.LecturasNotas.Concat(escenario.Api.LecturasAsistencia), id => Assert.Equal("EST-002", id));
        Assert.DoesNotContain(resultado.Respuesta.Citas, c => c.Documento == Textos.DocumentoDatosPrograma);
        AfirmarUnaLlamadaSinHerramientas(escenario);
    }

    [Fact]
    [Trait("CA", "CA-27")]
    [Trait("ESC", "ESC-189")]
    [Trait("SEG", "SEG-02")]
    public async Task ESC_189_n2_El_coordinador_de_otro_programa_recibe_tercero_mencionado()
    {
        var escenario = Con(Fijo("Este es el detalle de Andrés Martínez."), IndiceDeReglas(Articulo11));

        var resultado = await escenario.PreguntarAsync(Coord02, "¿Cuántas faltas tiene Andrés Martínez?");

        Assert.Equal(MotivoDenegacion.TerceroMencionado, resultado.Motivo);
        Assert.Empty(escenario.Llm.Solicitudes);
    }

    [Fact]
    [Trait("CA", "CA-27")]
    [Trait("ESC", "ESC-189")]
    [Trait("D", "D-94")]
    public async Task ESC_189_o1_La_oracion_citada_con_se_y_sin_cifra_de_faltas_se_conserva_antes_del_bloque()
    {
        var escenario = Con(Fijo("Las inasistencias se rigen por reglamento_academico_2025.md · Artículo 11."), IndiceDeReglas(Articulo11, Articulo12));

        var resultado = await escenario.PreguntarAsync(Est001, PreguntaQ10Tres);

        Assert.StartsWith("Las inasistencias se rigen por", Norm(resultado.Respuesta!.Respuesta), StringComparison.Ordinal);
        Assert.Contains(EncabezadoLaura, resultado.Respuesta.Respuesta, StringComparison.Ordinal);
        Assert.Contains(Art11, resultado.Respuesta.Citas);
    }

    [Fact]
    [Trait("CA", "CA-27")]
    [Trait("ESC", "ESC-189")]
    [Trait("D", "D-98")]
    public async Task ESC_189_o2_La_oracion_en_segunda_persona_se_poda_y_queda_solo_el_bloque()
    {
        var escenario = Con(Fijo("Tus inasistencias se rigen por reglamento_academico_2025.md · Artículo 11."), IndiceDeReglas(Articulo11, Articulo12));

        var resultado = await escenario.PreguntarAsync(Est001, PreguntaQ10Tres);

        Assert.Equal(EncabezadoLaura, Lineas(resultado.Respuesta!.Respuesta)[0]);
        Assert.DoesNotContain("Tus inasistencias", resultado.Respuesta.Respuesta, StringComparison.Ordinal);
        Assert.Contains(Art11, resultado.Respuesta.Citas);
        Assert.Contains(DatosAsistencia, resultado.Respuesta.Citas);
    }

    [Fact]
    [Trait("CA", "CA-27")]
    [Trait("ESC", "ESC-189")]
    [Trait("D", "D-98")]
    public async Task ESC_189_p_Dos_estudiantes_nombrados_dan_dos_bloques_y_se_leen_solo_ellos()
    {
        var escenario = Con(Fijo("Ambos tienen faltas registradas."), IndiceDeReglas(Articulo11, Articulo12));

        var resultado = await escenario.PreguntarAsync(Coord01, "¿Cuántas faltas tienen Andrés Martínez y Sofía Herrera?");

        var lineas = Lineas(resultado.Respuesta!.Respuesta);
        Assert.Equal(EncabezadoAndres, lineas[0]);
        Assert.Contains("Datos de Sofía Herrera (EST-003)", lineas);
        Assert.Contains("Total: 15 faltas", lineas);
        Assert.Contains("Total: 3 faltas (sin registro: Inglés III)", lineas);
        Assert.DoesNotContain("Ambos tienen", resultado.Respuesta.Respuesta, StringComparison.Ordinal);
        var lecturas = escenario.Api.LecturasNotas.Concat(escenario.Api.LecturasAsistencia).Distinct().Order().ToArray();
        Assert.Equal(["EST-002", "EST-003"], lecturas);
    }

    [Theory]
    [MemberData(nameof(FilasDeCa17))]
    [Trait("CA", "CA-17")]
    [Trait("ESC", "ESC-189")]
    [Trait("D", "D-98")]
    public async Task CA_17_Los_datos_de_personas_los_escribe_el_backend(string usuario, string pregunta, string texto, string inicio, string dato, string falso, string vacio)
    {
        Assert.Empty(vacio);
        var escenario = Con(Fijo(texto), IndiceDeReglas(Articulo5, Articulo11, Articulo12, Articulo20, PromedioPonderado));

        var resultado = await escenario.PreguntarAsync(Usuario(usuario), pregunta);

        var respuesta = resultado.Respuesta!.Respuesta;
        Assert.StartsWith(inicio, respuesta, StringComparison.Ordinal);
        Assert.Contains(dato, respuesta, StringComparison.Ordinal);
        Assert.DoesNotContain(falso, respuesta, StringComparison.Ordinal);
        AfirmarUnaLlamadaSinHerramientas(escenario);
        AfirmarRespondidaSinReintento(resultado, escenario);
    }

    [Theory]
    [MemberData(nameof(FilasDeCa14))]
    [Trait("CA", "CA-14")]
    [Trait("ESC", "ESC-189")]
    [Trait("D", "D-98")]
    public async Task CA_14_Las_citas_de_documento_salen_del_bloque_y_la_respuesta_termina_con_el_bloque(string usuario, string pregunta, string texto, string vacio, string[] documentos, bool hayBloque)
    {
        Assert.Empty(vacio);
        var entregadas = Catalogo.Where(s => documentos.Contains($"{s.Documento} · {s.Seccion}")).ToArray();
        var escenario = Con(Fijo(texto), IndiceDeReglas(entregadas));

        var resultado = await escenario.PreguntarAsync(Usuario(usuario), pregunta);

        Assert.Equal(documentos, Documentos(resultado));
        var respuesta = resultado.Respuesta!.Respuesta;
        Assert.Equal(hayBloque, respuesta.Contains("Datos de", StringComparison.Ordinal) || respuesta.Contains("Estudiantes con asignaturas", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("CA", "CA-14")]
    [Trait("ESC", "ESC-005")]
    [Trait("D", "D-98")]
    public async Task ESC_005_La_definitiva_de_Contabilidad_llega_en_el_bloque_aunque_el_modelo_solo_escriba_la_cifra()
    {
        var escenario = Con(Fijo("3.9."), IndiceDeReglas(NotaDefinitiva, Cortes));

        var resultado = await escenario.PreguntarAsync(Est004, PreguntaQ10Cinco);

        Assert.Equal($"{EncabezadoCamilo}\nDefinitivas: Contabilidad 3.9", Norm(resultado.Respuesta!.Respuesta));
        Assert.Contains(new Cita(Textos.DocumentoDatosEstudiante, "Notas"), resultado.Respuesta.Citas);
        Assert.Equal(["evaluacion_y_calificaciones.md · Sección 3"], Documentos(resultado));
        Assert.True(resultado.Respuesta.UsoDatosEstudiante);
    }

    [Fact]
    [Trait("CA", "CA-01")]
    [Trait("ESC", "ESC-006")]
    [Trait("D", "D-98")]
    public async Task ESC_006_Valentina_Castro_llega_en_el_bloque_del_programa_con_el_Articulo_5_y_nadie_mas()
    {
        var escenario = Con(Fijo("Valentina Castro: Microeconomía (1.8) y Estadística (2.3)."), IndiceDeReglas(Articulo5));

        var resultado = await escenario.PreguntarAsync(Coord02, PreguntaQ10Seis);

        Assert.Equal(LineaPerdidasAdm, Norm(resultado.Respuesta!.Respuesta));
        Assert.Equal([Art5, DatosProgramaAdm], resultado.Respuesta.Citas);
        Assert.True(resultado.Respuesta.UsoDatosEstudiante);
    }

    [Fact]
    [Trait("CA", "CA-01")]
    [Trait("ESC", "ESC-007")]
    [Trait("D", "D-100")]
    public async Task ESC_007_El_horario_de_Bienestar_sin_el_lugar_recibe_la_linea_obligatoria_tras_el_texto()
    {
        var escenario = Con(Fijo("Bienestar atiende de lunes a viernes de 8:00 a 17:00 y los sábados de 8:00 a 12:00 (bienestar_y_becas.md · Sección 1)."), IndiceDeReglas(Atencion));

        var resultado = await escenario.PreguntarAsync(Est001, "¿Cuál es el horario de atención de Bienestar?");

        Assert.Contains("Lugar: edificio B, piso 2.", resultado.Respuesta!.Respuesta, StringComparison.Ordinal);
        Assert.False(resultado.Respuesta.UsoDatosEstudiante);
        Assert.DoesNotContain("Datos de", resultado.Respuesta.Respuesta, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("CA", "CA-01")]
    [Trait("ESC", "ESC-033")]
    [Trait("D", "D-98")]
    public async Task ESC_033_Una_asignatura_fuera_del_catalogo_da_el_bloque_de_definitivas_sin_lineas_ni_cifras_de_Quimica()
    {
        var escenario = Con(Fijo("Esto es lo que encuentro."), IndiceDeReglas(NotaDefinitiva));

        var resultado = await escenario.PreguntarAsync(Est001, "¿Qué nota tengo en Química?");

        var respuesta = Norm(resultado.Respuesta!.Respuesta);
        Assert.Contains("Definitivas: Cálculo Integral 4.5, Programación Orientada a Objetos 4.8, Bases de Datos 4.1, Inglés III 4.0", respuesta, StringComparison.Ordinal);
        Assert.DoesNotContain("Química", respuesta, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("CA", "CA-01")]
    [Trait("ESC", "ESC-041")]
    [Trait("SEG", "SEG-02")]
    public async Task ESC_041_El_promedio_de_Valentina_Castro_llega_en_su_bloque_con_citas_de_notas_y_sin_datos_del_programa()
    {
        var escenario = Con(Fijo("Este es el detalle de Valentina Castro."), IndiceDeReglas(PromedioPonderado));

        var resultado = await escenario.PreguntarAsync(Coord02, "¿Cuál es el promedio de Valentina Castro?");

        Assert.Contains("Datos de Valentina Castro (EST-005)", Lineas(resultado.Respuesta!.Respuesta));
        Assert.Contains("Promedio ponderado del periodo: 2.65", Lineas(resultado.Respuesta.Respuesta));
        Assert.Contains(new Cita(Textos.DocumentoDatosEstudiante, "Notas"), resultado.Respuesta.Citas);
        Assert.DoesNotContain(resultado.Respuesta.Citas, c => c.Documento == Textos.DocumentoDatosPrograma);
        Assert.All(escenario.Api.LecturasNotas, id => Assert.Equal("EST-005", id));
    }

    [Fact]
    [Trait("CA", "CA-14")]
    [Trait("ESC", "ESC-155")]
    [Trait("D", "D-98")]
    public async Task ESC_155a_La_beca_sola_no_activa_resumen_ni_bloque_ni_lecturas()
    {
        var escenario = Con(Fijo("Requisitos: promedio de 4.3 o más, ninguna asignatura perdida y 12 créditos."), IndiceDeReglas(BecaPorRendimiento));

        var resultado = await escenario.PreguntarAsync(Est004, PreguntaQ10Ocho);

        Assert.Empty(escenario.Api.LecturasNotas);
        Assert.Empty(escenario.Api.LecturasAsistencia);
        Assert.DoesNotContain("Datos de", resultado.Respuesta!.Respuesta, StringComparison.Ordinal);
        Assert.False(resultado.Respuesta.UsoDatosEstudiante);
        Assert.Equal([Beca31], resultado.Respuesta.Citas);
    }

    [Fact]
    [Trait("CA", "CA-19")]
    [Trait("ESC", "ESC-190")]
    [Trait("D", "D-98")]
    public async Task ESC_190_b_El_promedio_de_un_estudiante_nombrado_no_hace_prelectura_y_lee_solo_a_EST_005()
    {
        var escenario = Con(Fijo("Este es el detalle de Valentina Castro."), IndiceDeReglas(PromedioPonderado));

        var resultado = await escenario.PreguntarAsync(Coord02, "¿Cuál es el promedio de Valentina Castro?");

        Assert.Contains("Promedio ponderado del periodo: 2.65", Lineas(resultado.Respuesta!.Respuesta));
        Assert.Contains(Lineas(resultado.Respuesta.Respuesta), l => l.StartsWith("Definitivas:", StringComparison.Ordinal));
        Assert.NotEmpty(escenario.Api.LecturasNotas);
        Assert.All(escenario.Api.LecturasNotas, id => Assert.Equal("EST-005", id));
        Assert.Contains(new Cita(Textos.DocumentoDatosEstudiante, "Notas"), resultado.Respuesta.Citas);
    }

    [Fact]
    [Trait("CA", "CA-19")]
    [Trait("ESC", "ESC-190")]
    [Trait("D", "D-98")]
    public async Task ESC_190_c_La_linea_de_perdidas_copiada_por_el_modelo_se_poda_y_el_bloque_cierra_la_respuesta()
    {
        string Copia(SolicitudLlm solicitud) =>
            string.Join("\n", solicitud.Mensajes.Select(m => m.Contenido)).Split('\n').First(l => l.StartsWith("Estudiantes con asignaturas perdidas", StringComparison.Ordinal));
        var escenario = Con((solicitud, _) => Texto(Copia(solicitud)), IndiceDeReglas(Articulo5));

        var resultado = await escenario.PreguntarAsync(Coord02, PreguntaQ10Seis);

        Assert.Equal(LineaPerdidasAdm, Norm(resultado.Respuesta!.Respuesta));
        AfirmarUnaLlamadaSinHerramientas(escenario);
        Assert.Contains(Art5, resultado.Respuesta.Citas);
        Assert.Equal(ResultadoAuditoria.Respondida, resultado.Auditoria);
    }

    [Theory]
    [InlineData("¿Qué dice el reglamento de mis estudiantes en prueba académica?", "Estudiantes en prueba académica: Valentina Castro (promedio 2.65)")]
    [InlineData("Según el reglamento, ¿Valentina Castro pierde la beca?", "Datos de Valentina Castro (EST-005)")]
    [Trait("CA", "CA-19")]
    [Trait("ESC", "ESC-190")]
    [Trait("D", "D-98")]
    public async Task ESC_190_l_El_bloque_se_activa_con_mis_o_con_el_nombre_de_un_estudiante_suyo(string pregunta, string linea)
    {
        var escenario = Con(Fijo("Este es el listado."), IndiceDeReglas(Articulo20, Articulo5, BecaPorRendimiento));

        var resultado = await escenario.PreguntarAsync(Coord02, pregunta);

        Assert.Contains(linea, Lineas(resultado.Respuesta!.Respuesta));
        AfirmarUnaLlamadaSinHerramientas(escenario);
    }
}
