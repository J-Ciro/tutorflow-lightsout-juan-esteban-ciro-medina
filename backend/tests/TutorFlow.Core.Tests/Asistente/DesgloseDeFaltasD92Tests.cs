using TutorFlow.Core.Asistente;
using TutorFlow.Core.Autorizacion;
using TutorFlow.Core.Conocimiento;
using static TutorFlow.Core.Tests.Asistente.EscenarioAsistente;
using static TutorFlow.Core.Tests.Asistente.SeccionesDeReferencia;

namespace TutorFlow.Core.Tests.Asistente;

public sealed class DesgloseDeFaltasD92Tests
{
    private const string PreguntaQ10 = "¿Cuántas faltas tengo en Bases de Datos?";
    private const string NotaAlModelo = "El sistema añadirá el desglose de faltas; no escribas cifras de faltas ni de clases.";
    private const string CalculoDeEst001 = "Cálculo Integral: faltas 1 de 64 clases programadas (faltas restantes: 8; máximo sin perder: 9). Si superas 9 inasistencias pierdes la asignatura por inasistencia.";
    private const string PooDeEst001 = "Programación Orientada a Objetos: faltas 0 de 48 clases programadas (faltas restantes: 7; máximo sin perder: 7). Si superas 7 inasistencias pierdes la asignatura por inasistencia.";
    private const string BasesDeEst001 = "Bases de Datos: faltas 2 de 48 clases programadas (faltas restantes: 5; máximo sin perder: 7). Si superas 7 inasistencias pierdes la asignatura por inasistencia.";
    private const string InglesDeEst001 = "Inglés III: faltas 1 de 32 clases programadas (faltas restantes: 3; máximo sin perder: 4). Si superas 4 inasistencias pierdes la asignatura por inasistencia.";
    private const string CalculoDeEst002 = "Cálculo Integral: faltas 3 de 64 clases programadas (faltas restantes: 6; máximo sin perder: 9). Si superas 9 inasistencias pierdes la asignatura por inasistencia.";
    private const string PooDeEst002 = "Programación Orientada a Objetos: faltas 2 de 48 clases programadas (faltas restantes: 5; máximo sin perder: 7). Si superas 7 inasistencias pierdes la asignatura por inasistencia.";
    private const string BasesDeEst002 = "Bases de Datos: faltas 9 de 48 clases programadas (faltas restantes: 0; máximo sin perder: 7). Perdida por inasistencia: superaste 7 inasistencias.";
    private const string InglesDeEst002 = "Inglés III: faltas 1 de 32 clases programadas (faltas restantes: 3; máximo sin perder: 4). Si superas 4 inasistencias pierdes la asignatura por inasistencia.";
    private const string InglesSinRegistro = "Inglés III: sin registro de inasistencias en 32 clases programadas (máximo sin perder: 4). Solicita la revisión a tu coordinador (Artículo 12).";
    private static readonly Usuario Est001 = new(Rol.Estudiante, "EST-001");
    private static readonly Usuario Est002 = new(Rol.Estudiante, "EST-002");
    private static readonly Usuario Est003 = new(Rol.Estudiante, "EST-003");
    private static readonly Usuario Coord01 = new(Rol.Coordinador, "COORD-01");
    private static readonly Cita DatosAsistencia = new("Datos del estudiante", "Asistencia");

    private static IndiceFijo IndiceDeFaltas() => Indice(Articulo11, Articulo12);

    private static string Normalizada(string respuesta) => respuesta.Replace("\r\n", "\n", StringComparison.Ordinal);

    private static string[] Lineas(string respuesta) =>
        Normalizada(respuesta).Split('\n', StringSplitOptions.RemoveEmptyEntries);

    [Fact]
    [Trait("CA", "CA-27")]
    [Trait("ESC", "ESC-189")]
    [Trait("D", "D-92")]
    public async Task ESC_189_h_La_pregunta_Q10_3_recibe_la_oracion_una_linea_vacia_la_linea_de_Bases_de_Datos_y_el_total()
    {
        var escenario = Con(Fijo("Aquí está el detalle de tus faltas."), IndiceDeFaltas());

        var resultado = await escenario.PreguntarAsync(Est001, PreguntaQ10);

        Assert.Single(escenario.Llm.Solicitudes);
        Assert.Equal($"Aquí está el detalle de tus faltas.\n\n{BasesDeEst001}\nTotal: 2 faltas", Normalizada(resultado.Respuesta!.Respuesta));
        Assert.Contains("2 de 48 clases programadas", resultado.Respuesta.Respuesta, StringComparison.Ordinal);
        Assert.Contains(DatosAsistencia, resultado.Respuesta.Citas);
        Assert.Contains(new Cita(Reglamento, "Artículo 11"), resultado.Respuesta.Citas);
        Assert.True(resultado.Respuesta.UsoDatosEstudiante);
        Assert.Equal(ResultadoAuditoria.Respondida, resultado.Auditoria);
    }

    [Fact]
    [Trait("CA", "CA-27")]
    [Trait("ESC", "ESC-189")]
    [Trait("D", "D-92")]
    public async Task ESC_189_h_El_turno_del_modelo_lleva_la_nota_fija_sobre_las_cifras_de_faltas()
    {
        var escenario = Con(Fijo("Aquí está el detalle de tus faltas."), IndiceDeFaltas());

        await escenario.PreguntarAsync(Est001, PreguntaQ10);

        Assert.Contains(NotaAlModelo, escenario.Llm.TextoDeLlamada(0), StringComparison.Ordinal);
    }

    [Fact]
    [Trait("CA", "CA-27")]
    [Trait("ESC", "ESC-189")]
    [Trait("D", "D-92")]
    public async Task ESC_189_i_Sin_asignatura_nombrada_el_desglose_trae_las_cuatro_lineas_en_orden_y_el_total_de_15()
    {
        var escenario = Con(Fijo("Tienes 30 faltas en total."), IndiceDeFaltas());

        var resultado = await escenario.PreguntarAsync(Est002, "¿Cuántas faltas tengo?");

        Assert.Equal([CalculoDeEst002, PooDeEst002, BasesDeEst002, InglesDeEst002, "Total: 15 faltas"], Lineas(resultado.Respuesta!.Respuesta));
        Assert.DoesNotContain("30", resultado.Respuesta.Respuesta, StringComparison.Ordinal);
        Assert.Single(escenario.Llm.Solicitudes);
        Assert.False(escenario.Auditoria.Eventos.Single().Reintento);
        Assert.Contains(DatosAsistencia, resultado.Respuesta.Citas);
        Assert.Contains(new Cita(Reglamento, "Artículo 11"), resultado.Respuesta.Citas);
    }

    [Fact]
    [Trait("CA", "CA-27")]
    [Trait("ESC", "ESC-189")]
    [Trait("D", "D-92")]
    public async Task ESC_189_j_Solo_las_asignaturas_nombradas_aparecen_y_el_total_las_suma()
    {
        var escenario = Con(Fijo("Este es tu detalle."), IndiceDeFaltas());

        var resultado = await escenario.PreguntarAsync(Est001, "¿Cuántas faltas tengo en Cálculo Integral y en Inglés III?");

        Assert.Equal(["Este es tu detalle.", CalculoDeEst001, InglesDeEst001, "Total: 2 faltas"], Lineas(resultado.Respuesta!.Respuesta));
    }

    [Fact]
    [Trait("CA", "CA-27")]
    [Trait("ESC", "ESC-189")]
    [Trait("D", "D-92")]
    public async Task ESC_189_k_La_asignatura_perdida_por_inasistencia_trae_cero_restantes_y_el_total_de_9()
    {
        var escenario = Con(Fijo("Este es tu detalle."), IndiceDeFaltas());

        var resultado = await escenario.PreguntarAsync(Est002, "¿Cuántas faltas me quedan en Bases de Datos?");

        Assert.Equal(["Este es tu detalle.", BasesDeEst002, "Total: 9 faltas"], Lineas(resultado.Respuesta!.Respuesta));
        Assert.Contains("faltas restantes: 0", resultado.Respuesta.Respuesta, StringComparison.Ordinal);
        Assert.Contains("Perdida por inasistencia: superaste 7 inasistencias.", resultado.Respuesta.Respuesta, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("CA", "CA-27")]
    [Trait("ESC", "ESC-189")]
    [Trait("D", "D-92")]
    public async Task ESC_189_l_La_oracion_del_modelo_con_cifras_de_faltas_se_elimina_sin_reintento_y_queda_solo_el_desglose()
    {
        var escenario = Con(Fijo("Tienes 9 faltas en Bases de Datos."), IndiceDeFaltas());

        var resultado = await escenario.PreguntarAsync(Est001, PreguntaQ10);

        Assert.Single(escenario.Llm.Solicitudes);
        Assert.Equal([BasesDeEst001, "Total: 2 faltas"], Lineas(resultado.Respuesta!.Respuesta));
        Assert.DoesNotContain("9 faltas", resultado.Respuesta.Respuesta, StringComparison.Ordinal);
        Assert.Equal(ResultadoAuditoria.Respondida, resultado.Auditoria);
        Assert.False(escenario.Auditoria.Eventos.Single().Reintento);
    }

    [Theory]
    [InlineData("Tienes 0 faltas. Total: 0 faltas.")]
    [InlineData("Hay 48 clases programadas y tienes 2 faltas.")]
    [InlineData("Llevas 3 inasistencias.")]
    [Trait("CA", "CA-27")]
    [Trait("ESC", "ESC-189")]
    [Trait("D", "D-92")]
    public async Task ESC_189_l_El_modelo_no_puede_imponer_cifras_de_faltas_ni_de_clases_en_ninguna_forma(string texto)
    {
        var escenario = Con(Fijo(texto), IndiceDeFaltas());

        var resultado = await escenario.PreguntarAsync(Est001, PreguntaQ10);

        Assert.Single(escenario.Llm.Solicitudes);
        Assert.Equal([BasesDeEst001, "Total: 2 faltas"], Lineas(resultado.Respuesta!.Respuesta));
    }

    [Fact]
    [Trait("CA", "CA-27")]
    [Trait("ESC", "ESC-189")]
    [Trait("D", "D-92")]
    public async Task ESC_189_l_Solo_se_elimina_la_oracion_con_cifras_y_la_introduccion_sin_cifras_se_conserva()
    {
        var escenario = Con(Fijo("Aquí está tu detalle. Tienes 9 faltas en Bases de Datos."), IndiceDeFaltas());

        var resultado = await escenario.PreguntarAsync(Est001, PreguntaQ10);

        Assert.Equal(["Aquí está tu detalle.", BasesDeEst001, "Total: 2 faltas"], Lineas(resultado.Respuesta!.Respuesta));
    }

    [Fact]
    [Trait("CA", "CA-27")]
    [Trait("ESC", "ESC-189")]
    [Trait("D", "D-92")]
    public async Task ESC_189_m_La_pregunta_normativa_sin_marcador_personal_no_trae_desglose_ni_lee_asistencia()
    {
        var escenario = Con(Fijo("Se pierde al superar el 15% de las clases programadas (reglamento_academico_2025.md · Artículo 11)."), Indice(Articulo11));

        var resultado = await escenario.PreguntarAsync(Est001, "¿Con cuántas faltas se pierde una materia?");

        Assert.DoesNotContain("Total:", resultado.Respuesta!.Respuesta, StringComparison.Ordinal);
        Assert.DoesNotContain("faltas restantes", resultado.Respuesta.Respuesta, StringComparison.Ordinal);
        Assert.Empty(escenario.Api.LecturasAsistencia);
        Assert.False(resultado.Respuesta.UsoDatosEstudiante);
        Assert.Contains(new Cita(Reglamento, "Artículo 11"), resultado.Respuesta.Citas);
    }

    [Fact]
    [Trait("CA", "CA-27")]
    [Trait("ESC", "ESC-189")]
    [Trait("D", "D-92")]
    public async Task ESC_189_m_El_coordinador_sin_persona_nombrada_tampoco_activa_el_desglose()
    {
        var escenario = Con(Fijo("Se pierde al superar el 15% de las clases programadas (reglamento_academico_2025.md · Artículo 11)."), Indice(Articulo11));

        var resultado = await escenario.PreguntarAsync(Coord01, "¿Con cuántas faltas se pierde una materia?");

        Assert.DoesNotContain("Total:", resultado.Respuesta!.Respuesta, StringComparison.Ordinal);
        Assert.Empty(escenario.Api.LecturasAsistencia);
    }

    [Fact]
    [Trait("CA", "CA-27")]
    [Trait("ESC", "ESC-189")]
    [Trait("D", "D-92")]
    public async Task ESC_189_n_El_coordinador_con_el_nombre_de_un_estudiante_suyo_recibe_el_desglose_en_tercera_persona()
    {
        var escenario = Con(Fijo("Este es el detalle de Andrés Martínez."), IndiceDeFaltas());

        var resultado = await escenario.PreguntarAsync(Coord01, "¿Cuántas faltas tiene Andrés Martínez?");

        var lineas = Lineas(resultado.Respuesta!.Respuesta);
        Assert.Equal(6, lineas.Length);
        Assert.Equal("Este es el detalle de Andrés Martínez.", lineas[0]);
        Assert.Equal("Cálculo Integral: faltas 3 de 64 clases programadas (faltas restantes: 6; máximo sin perder: 9). Si supera 9 inasistencias pierde la asignatura por inasistencia.", lineas[1]);
        Assert.Equal("Programación Orientada a Objetos: faltas 2 de 48 clases programadas (faltas restantes: 5; máximo sin perder: 7). Si supera 7 inasistencias pierde la asignatura por inasistencia.", lineas[2]);
        Assert.Equal("Bases de Datos: faltas 9 de 48 clases programadas (faltas restantes: 0; máximo sin perder: 7). Perdida por inasistencia: superó 7 inasistencias.", lineas[3]);
        Assert.Equal("Inglés III: faltas 1 de 32 clases programadas (faltas restantes: 3; máximo sin perder: 4). Si supera 4 inasistencias pierde la asignatura por inasistencia.", lineas[4]);
        Assert.Equal("Total: 15 faltas", lineas[5]);
        Assert.All(escenario.Api.LecturasAsistencia, id => Assert.Equal("EST-002", id));
        Assert.All(escenario.Api.LecturasNotas, id => Assert.Equal("EST-002", id));
    }

    [Fact]
    [Trait("CA", "CA-27")]
    [Trait("ESC", "ESC-189")]
    [Trait("SEG", "SEG-04")]
    [Trait("D", "D-92")]
    public async Task ESC_189_n_El_otro_coordinador_con_la_misma_pregunta_recibe_403_por_tercero_mencionado()
    {
        var escenario = Con(Fijo("No debe llamarse."), IndiceDeFaltas());

        var resultado = await escenario.PreguntarAsync(new Usuario(Rol.Coordinador, "COORD-02"), "¿Cuántas faltas tiene Andrés Martínez?");

        Assert.Equal(TipoResultadoPregunta.Denegada, resultado.Tipo);
        Assert.Equal(MotivoDenegacion.TerceroMencionado, resultado.Motivo);
        Assert.Empty(escenario.Llm.Solicitudes);
        Assert.Empty(escenario.Api.LecturasAsistencia);
    }

    [Fact]
    [Trait("CA", "CA-27")]
    [Trait("ESC", "ESC-026")]
    [Trait("D", "D-92")]
    public async Task ESC_026_Sin_registro_de_asistencia_el_desglose_trae_la_linea_sin_registro_y_el_total_sin_registro()
    {
        var escenario = Con(Fijo("Revisa tus datos."), IndiceDeFaltas());

        var resultado = await escenario.PreguntarAsync(Est003, "¿Cuántas faltas tengo en Inglés III?");

        Assert.Equal(["Revisa tus datos.", InglesSinRegistro, "Total: sin registro"], Lineas(resultado.Respuesta!.Respuesta));
        Assert.Contains(new Cita(Reglamento, "Artículo 12"), resultado.Respuesta.Citas);
    }

    [Fact]
    [Trait("CA", "CA-27")]
    [Trait("ESC", "ESC-026")]
    [Trait("D", "D-92")]
    public async Task ESC_026_Con_una_asignatura_sin_registro_el_total_suma_las_demas_y_la_nombra()
    {
        var escenario = Con(Fijo("Este es tu detalle."), IndiceDeFaltas());

        var resultado = await escenario.PreguntarAsync(Est003, "¿Cuántas faltas tengo?");

        var lineas = Lineas(resultado.Respuesta!.Respuesta);
        Assert.Equal(6, lineas.Length);
        Assert.Contains(InglesSinRegistro, lineas);
        Assert.Equal("Total: 3 faltas (sin registro: Inglés III)", lineas[^1]);
    }

    [Fact]
    [Trait("CA", "CA-27")]
    [Trait("ESC", "ESC-154")]
    [Trait("D", "D-92")]
    public async Task ESC_154_La_copia_de_la_linea_del_resumen_se_elimina_y_la_respuesta_es_el_desglose()
    {
        var escenario = Con((solicitud, _) => Texto(BasesDeEst001), IndiceDeFaltas());

        var resultado = await escenario.PreguntarAsync(Est001, PreguntaQ10);

        Assert.Single(escenario.Llm.Solicitudes);
        Assert.Equal([BasesDeEst001, "Total: 2 faltas"], Lineas(resultado.Respuesta!.Respuesta));
        Assert.Contains(DatosAsistencia, resultado.Respuesta.Citas);
        Assert.Contains("máximo sin perder: 7", escenario.Llm.TextoDeLlamada(0), StringComparison.Ordinal);
    }

    [Fact]
    [Trait("CA", "CA-27")]
    [Trait("ESC", "ESC-177")]
    [Trait("D", "D-92")]
    public async Task ESC_177_j_Las_citas_copiadas_de_las_reglas_aplicadas_se_descartan_y_la_respuesta_es_el_desglose()
    {
        const string texto = "Bases de Datos: 2 faltas de 48 clases (evaluacion_y_calificaciones.md · Sección 3; evaluacion_y_calificaciones.md · Sección 4).";
        var escenario = Con(Fijo(texto), Indice(Articulo11, Articulo12, NotaDefinitiva, PromedioPonderado));

        var resultado = await escenario.PreguntarAsync(Est001, PreguntaQ10);

        Assert.Single(escenario.Llm.Solicitudes);
        Assert.Equal([BasesDeEst001, "Total: 2 faltas"], Lineas(resultado.Respuesta!.Respuesta));
        Assert.Equal([new Cita(Reglamento, "Artículo 11")], resultado.Respuesta.Citas.Where(c => c.Documento is Reglamento or Evaluacion));
    }

    [Fact]
    [Trait("CA", "CA-27")]
    [Trait("ESC", "ESC-189")]
    [Trait("D", "D-92")]
    public async Task ESC_189_La_palabra_asistencia_con_marcador_personal_activa_el_desglose_de_todas_las_asignaturas()
    {
        var escenario = Con(Fijo("Este es tu detalle."), IndiceDeFaltas());

        var resultado = await escenario.PreguntarAsync(Est001, "¿Cómo va mi asistencia?");

        Assert.Equal(["Este es tu detalle.", CalculoDeEst001, PooDeEst001, BasesDeEst001, InglesDeEst001, "Total: 4 faltas"], Lineas(resultado.Respuesta!.Respuesta));
    }

    [Fact]
    [Trait("CA", "CA-27")]
    [Trait("ESC", "ESC-189")]
    [Trait("D", "D-92")]
    public async Task ESC_189_Las_asignaturas_que_la_pregunta_nombra_y_la_persona_no_cursa_no_dan_desglose()
    {
        var escenario = Con(Fijo("No tengo ese dato."), IndiceDeFaltas());

        var resultado = await escenario.PreguntarAsync(Est001, "¿Cuántas faltas tengo en Contabilidad?");

        Assert.DoesNotContain("Total:", resultado.Respuesta!.Respuesta, StringComparison.Ordinal);
        Assert.DoesNotContain("faltas restantes", resultado.Respuesta.Respuesta, StringComparison.Ordinal);
    }
}
