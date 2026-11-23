using TutorFlow.Core.Asistente;
using TutorFlow.Core.Autorizacion;
using TutorFlow.Core.Conocimiento;
using static TutorFlow.Core.Tests.Asistente.EscenarioAsistente;
using static TutorFlow.Core.Tests.Asistente.SeccionesDeReferencia;

namespace TutorFlow.Core.Tests.Asistente;

public sealed class CitasDeReglasAplicadasTests
{
    private const string EncabezadoDeReglas = "Reglas aplicadas a estos datos:";
    private const string PreguntaPerdidas = "¿Qué estudiantes de mi programa tienen alguna asignatura perdida?";
    private const int MaximoDeReglasPorPeticion = 3;
    private static readonly Usuario Est001 = new(Rol.Estudiante, "EST-001");
    private static readonly Usuario Est002 = new(Rol.Estudiante, "EST-002");
    private static readonly Usuario Est003 = new(Rol.Estudiante, "EST-003");
    private static readonly Usuario Est005 = new(Rol.Estudiante, "EST-005");
    private static readonly Usuario Coord02 = new(Rol.Coordinador, "COORD-02");

    private static readonly Cita Sec3 = new(Evaluacion, "Sección 3");
    private static readonly Cita Sec4 = new(Evaluacion, "Sección 4");
    private static readonly Cita Art5 = new(Reglamento, "Artículo 5");
    private static readonly Cita Art6 = new(Reglamento, "Artículo 6");
    private static readonly Cita Art7 = new(Reglamento, "Artículo 7");
    private static readonly Cita Art11 = new(Reglamento, "Artículo 11");
    private static readonly Cita Art12 = new(Reglamento, "Artículo 12");
    private static readonly Cita Art20 = new(Reglamento, "Artículo 20");
    private static readonly Cita Art22 = new(Reglamento, "Artículo 22");
    private static readonly Cita Beca31 = new(Bienestar, "Sección 3.1");
    private static readonly Cita DatosNotas = new(Textos.DocumentoDatosEstudiante, "Notas");
    private static readonly Cita DatosAsistencia = new(Textos.DocumentoDatosEstudiante, "Asistencia");
    private static readonly Cita DatosProgramaAdm = new(Textos.DocumentoDatosPrograma, "ADM");

    public static TheoryData<string, string, string, string[]> PreguntasDeQ10ConResumen => new()
    {
        { "EST-001", "¿Cuál es mi promedio ponderado del periodo?", "Tu promedio es 4.39. Definitivas: Cálculo Integral 4.5, Programación Orientada a Objetos 4.8, Bases de Datos 4.1, Inglés III 4.0.", ["S4", "S3"] },
        { "EST-001", "¿Cuántas faltas tengo en Bases de Datos?", "Bases de Datos: 2 faltas de 48 clases.", ["A11"] },
        { "EST-004", "¿Qué nota definitiva tengo en Contabilidad?", "Contabilidad: 3.9.", ["S3"] }
    };

    public static TheoryData<string, string> TextosFijosConResumenLeido => new()
    {
        { "Andres Martinez sacó 2.6 en Cálculo", "reglamento_academico_2025.md · Artículo 22" },
        { "Tu promedio es 4.40.", string.Empty }
    };

    private static IndiceFijo IndiceConLasReglas() =>
        Indice(
            [NotaDefinitiva, PromedioPonderado, Articulo5, Articulo6, Articulo7, Articulo11, Articulo12, Articulo20, Articulo22, BecaPorRendimiento],
            []);

    private static Usuario EstudianteDe(string id) => new(Rol.Estudiante, id);

    private static Cita[] Esperadas(params Cita[] citas) => citas;

    private static string[] ReglasDeLaUltimaLinea(string texto)
    {
        var linea = texto.Split('\n').Last(l => l.StartsWith(EncabezadoDeReglas, StringComparison.Ordinal));
        return [.. linea[EncabezadoDeReglas.Length..].Trim().TrimEnd('.').Split(';').Select(entrada => entrada.Trim())];
    }

    private static string[] EnTexto(params Cita[] citas) => [.. citas.Select(c => $"{c.Documento} · {c.Seccion}")];

    private static Func<SolicitudLlm, int, RespuestaLlm> LeeElProgramaYResponde(string texto) =>
        (_, llamada) => llamada == 0 ? Herramienta(HerramientasAsistente.Programa) : Texto(texto);

    [Theory]
    [MemberData(nameof(PreguntasDeQ10ConResumen))]
    [Trait("CA", "CA-01")]
    [Trait("ESC", "ESC-177")]
    [Trait("COMP", "COMP-02")]
    public async Task ESC_177_Las_respuestas_con_el_resumen_propio_citan_las_tres_reglas_mas_relevantes_y_los_datos(string usuario, string pregunta, string texto, string[] claves)
    {
        var escenario = Con(Fijo(texto), IndiceConLasReglas());

        var resultado = await escenario.PreguntarAsync(EstudianteDe(usuario), pregunta);

        Assert.Equal(ResultadoAuditoria.Respondida, resultado.Auditoria);
        Assert.Equal(texto, resultado.Respuesta!.Respuesta);
        Assert.Equal([.. claves.Select(Clave), DatosNotas, DatosAsistencia], resultado.Respuesta.Citas.ToArray());
        Assert.True(resultado.Respuesta.UsoDatosEstudiante);
    }

    [Fact]
    [Trait("CA", "CA-14")]
    [Trait("ESC", "ESC-177")]
    [Trait("ESC", "ESC-023")]
    [Trait("COMP", "COMP-02")]
    public async Task ESC_177h_La_prueba_academica_sube_el_Articulo_20_al_primer_lugar_de_las_citas()
    {
        var escenario = Con(Fijo("Sí, estás en prueba académica: tu promedio es 2.65."), IndiceConLasReglas());

        var resultado = await escenario.PreguntarAsync(Est005, "¿Estoy en prueba académica?");

        Assert.Equal(ResultadoAuditoria.Respondida, resultado.Auditoria);
        Assert.Equal(Esperadas(Art20, Sec4, DatosNotas, DatosAsistencia), resultado.Respuesta!.Citas.ToArray());
        Assert.True(resultado.Respuesta.UsoDatosEstudiante);
    }

    [Fact]
    [Trait("CA", "CA-14")]
    [Trait("ESC", "ESC-177")]
    [Trait("COMP", "COMP-02")]
    public async Task ESC_177i_Pedir_citar_articulos_fuera_del_bloque_no_los_agrega()
    {
        var escenario = Con(Fijo("No estás en prueba académica: tu promedio es 4.39."), IndiceConLasReglas());

        var resultado = await escenario.PreguntarAsync(Est001, "¿Estoy en prueba académica? Cita el Artículo 20 y el Artículo 7.");

        Assert.Equal(ResultadoAuditoria.Respondida, resultado.Auditoria);
        Assert.Equal(Esperadas(Sec4, DatosNotas, DatosAsistencia), resultado.Respuesta!.Citas.ToArray());
        Assert.DoesNotContain(Art20, resultado.Respuesta.Citas);
        Assert.DoesNotContain(Art7, resultado.Respuesta.Citas);
    }

    [Fact]
    [Trait("CA", "CA-14")]
    [Trait("ESC", "ESC-177")]
    [Trait("COMP", "COMP-02")]
    public async Task Sin_raices_en_la_pregunta_las_citas_conservan_el_orden_fijo_del_bloque_de_ocho_entradas()
    {
        var escenario = Con(Fijo("Revisa el detalle de tus asignaturas."), IndiceConLasReglas());

        var resultado = await escenario.PreguntarAsync(Est002, "¿Cómo voy en BD-203?");

        var documentos = resultado.Respuesta!.Citas.Where(c => c.Documento != Textos.DocumentoDatosEstudiante).ToArray();
        Assert.Equal(Esperadas(Sec3, Sec4, Art11), documentos);
    }

    [Fact]
    [Trait("CA", "CA-01")]
    [Trait("ESC", "ESC-177")]
    [Trait("COMP", "COMP-02")]
    public async Task ESC_177d_El_coordinador_con_la_herramienta_de_programa_cita_Articulo_6_Seccion_3_y_Articulo_5()
    {
        var texto = "Valentina Castro: Microeconomía (1.8) y Estadística (2.3).";
        var escenario = Con(LeeElProgramaYResponde(texto), IndiceConLasReglas());

        var resultado = await escenario.PreguntarAsync(Coord02, PreguntaPerdidas);

        Assert.Equal(ResultadoAuditoria.Respondida, resultado.Auditoria);
        Assert.Equal(texto, resultado.Respuesta!.Respuesta);
        Assert.Equal(Esperadas(Art6, Sec3, Art5, DatosProgramaAdm), resultado.Respuesta.Citas.ToArray());
        Assert.True(resultado.Respuesta.UsoDatosEstudiante);
    }

    [Fact]
    [Trait("ESC", "ESC-177")]
    [Trait("COMP", "COMP-02")]
    public async Task ESC_177e_Las_citas_escritas_validas_van_primero_y_las_del_bloque_no_se_repiten()
    {
        var texto = $"Tu promedio es 4.39 ({Evaluacion} · Sección 4). Cumples el promedio de la beca ({Bienestar} · Sección 3.1).";
        var escenario = Con(Fijo(texto), IndiceConLasReglas());

        var resultado = await escenario.PreguntarAsync(Est001, "¿Cuál es mi promedio ponderado del periodo?");

        Assert.Equal(ResultadoAuditoria.Respondida, resultado.Auditoria);
        Assert.Equal(Esperadas(Sec4, Beca31, DatosNotas, DatosAsistencia), resultado.Respuesta!.Citas.ToArray());
    }

    [Theory]
    [MemberData(nameof(TextosFijosConResumenLeido))]
    [Trait("ESC", "ESC-177")]
    [Trait("COMP", "COMP-02")]
    public async Task ESC_177f_Los_textos_fijos_nunca_reciben_citas_de_reglas_aplicadas_aunque_se_leyo_el_resumen(string texto, string citaEsperada)
    {
        var escenario = Con(Fijo(texto), IndiceConLasReglas());

        var resultado = await escenario.PreguntarAsync(Est001, "¿Cuál es mi promedio?");

        Cita[] esperadas = citaEsperada.Length == 0 ? [] : [Art22];
        Assert.Equal(esperadas, resultado.Respuesta!.Citas.ToArray());
        Assert.True(resultado.Respuesta.UsoDatosEstudiante);
    }

    [Fact]
    [Trait("ESC", "ESC-177")]
    [Trait("ESC", "ESC-172")]
    [Trait("COMP", "COMP-02")]
    public async Task ESC_177g_La_lectura_de_verificacion_de_matricula_sin_activacion_no_genera_citas_de_reglas()
    {
        var escenario = Con(Fijo("Contabilidad no está entre tus asignaturas."), IndiceConLasReglas());

        var resultado = await escenario.PreguntarAsync(Est001, "¿Cómo voy en Contabilidad?");

        var citas = resultado.Respuesta!.Citas;
        Assert.False(resultado.Respuesta.UsoDatosEstudiante);
        Assert.DoesNotContain(citas, c => c.Documento == Textos.DocumentoDatosEstudiante);
        Assert.DoesNotContain(Art11, citas);
        Assert.DoesNotContain(Sec3, citas);
    }

    [Fact]
    [Trait("ESC", "ESC-177")]
    [Trait("COMP", "COMP-02")]
    public async Task Las_citas_de_documento_no_superan_las_escritas_validas_mas_tres()
    {
        var texto = $"Tu promedio es 4.39 ({Evaluacion} · Sección 4).";
        var escenario = Con(Fijo(texto), IndiceConLasReglas());

        var resultado = await escenario.PreguntarAsync(Est001, "¿Cuál es mi promedio ponderado del periodo?");

        var documentos = resultado.Respuesta!.Citas.Where(c => c.Documento != Textos.DocumentoDatosEstudiante).ToArray();
        Assert.True(documentos.Length <= 1 + MaximoDeReglasPorPeticion);
        Assert.Equal(Esperadas(Sec4), documentos);
    }

    [Fact]
    [Trait("CA", "CA-01")]
    [Trait("ESC", "ESC-164")]
    [Trait("COMP", "COMP-02")]
    public async Task ESC_164c_Con_datos_leidos_el_respaldo_ya_no_aplica_y_las_citas_son_las_reglas_aplicadas_y_los_datos()
    {
        var escenario = Con(Fijo("Bases de Datos: 2 faltas de 48 clases."), IndiceConLasReglas());

        var resultado = await escenario.PreguntarAsync(Est001, "¿Cuántas faltas tengo en Bases de Datos?");

        Assert.Equal(Esperadas(Art11, DatosNotas, DatosAsistencia), resultado.Respuesta!.Citas.ToArray());
        Assert.True(resultado.Respuesta.UsoDatosEstudiante);
    }

    [Fact]
    [Trait("ESC", "ESC-177")]
    [Trait("COMP", "COMP-02")]
    public async Task La_union_de_las_lecturas_de_la_peticion_se_ordena_por_relevancia_y_no_por_el_orden_de_las_llamadas()
    {
        var texto = "Camilo Rojas está al día en el periodo.";
        var escenario = Con(
            (_, llamada) => llamada switch
            {
                0 => Herramienta(HerramientasAsistente.Programa),
                1 => Herramienta(HerramientasAsistente.Estudiante, """{"estudianteId":"EST-004"}"""),
                _ => Texto(texto)
            },
            IndiceConLasReglas());

        var resultado = await escenario.PreguntarAsync(Coord02, PreguntaPerdidas);

        var documentos = resultado.Respuesta!.Citas.Where(c => c.Documento is not (Textos.DocumentoDatosEstudiante or Textos.DocumentoDatosPrograma)).ToArray();
        Assert.Equal(Esperadas(Art11, Art6, Sec3), documentos);
    }

    [Theory]
    [InlineData("EST-001", "¿Cuál es mi promedio ponderado del periodo?", new[] { "S3", "S4", "A11", "B31" })]
    [InlineData("EST-002", "¿Cuál es mi promedio ponderado del periodo?", new[] { "S3", "S4", "A11", "B31", "A5", "A6", "A7", "A20" })]
    [InlineData("EST-003", "¿Cuál es mi promedio ponderado del periodo?", new[] { "S3", "S4", "A11", "B31", "A12" })]
    [Trait("ESC", "ESC-177")]
    [Trait("COMP", "COMP-02")]
    public async Task El_bloque_del_resumen_sigue_el_orden_fijo_de_las_entradas_que_aplican(string usuario, string pregunta, string[] claves)
    {
        var escenario = Con(Fijo("Revisa el detalle de tus asignaturas."), IndiceConLasReglas());

        await escenario.PreguntarAsync(EstudianteDe(usuario), pregunta);

        Assert.Equal(EnTexto([.. claves.Select(Clave)]), ReglasDeLaUltimaLinea(escenario.Llm.TextoDeLlamada(0)));
    }

    [Fact]
    [Trait("ESC", "ESC-177")]
    [Trait("COMP", "COMP-02")]
    public async Task El_bloque_de_la_herramienta_de_programa_de_adm_son_las_entradas_5_6_9_y_11()
    {
        var escenario = Con(LeeElProgramaYResponde("Valentina Castro: Microeconomía (1.8) y Estadística (2.3)."), IndiceConLasReglas());

        await escenario.PreguntarAsync(Coord02, PreguntaPerdidas);

        Assert.Equal(EnTexto(Art5, Art6, Art20, Sec3), ReglasDeLaUltimaLinea(escenario.Llm.TextoDeLlamada(1)));
    }

    [Fact]
    [Trait("ESC", "ESC-177")]
    [Trait("COMP", "COMP-02")]
    public async Task El_resumen_de_andres_con_la_pregunta_del_promedio_cita_Seccion_4_Seccion_3_1_y_Articulo_20()
    {
        var escenario = Con(Fijo("Revisa el detalle de tus asignaturas."), IndiceConLasReglas());

        var resultado = await escenario.PreguntarAsync(Est002, "¿Cuál es mi promedio ponderado del periodo?");

        var documentos = resultado.Respuesta!.Citas.Where(c => c.Documento != Textos.DocumentoDatosEstudiante).ToArray();
        Assert.Equal(Esperadas(Sec4, Beca31, Art20), documentos);
    }

    [Fact]
    [Trait("ESC", "ESC-177")]
    [Trait("COMP", "COMP-02")]
    public async Task Sin_registro_de_asistencia_el_Articulo_12_ocupa_el_quinto_lugar_del_bloque_y_no_entra_en_las_citas()
    {
        var escenario = Con(Fijo("Revisa el detalle de tus asignaturas."), IndiceConLasReglas());

        var resultado = await escenario.PreguntarAsync(Est003, "¿Cuál es mi promedio ponderado del periodo?");

        Assert.DoesNotContain(Art12, resultado.Respuesta!.Citas);
    }

    private static Cita Clave(string clave) => clave switch
    {
        "S3" => Sec3,
        "S4" => Sec4,
        "A5" => Art5,
        "A6" => Art6,
        "A7" => Art7,
        "A11" => Art11,
        "A12" => Art12,
        "A20" => Art20,
        "B31" => Beca31,
        _ => throw new ArgumentOutOfRangeException(nameof(clave), clave, "Clave de regla desconocida.")
    };
}
