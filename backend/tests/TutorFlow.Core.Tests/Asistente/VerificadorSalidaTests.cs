using TutorFlow.Core.Asistente;
using TutorFlow.Core.Autorizacion;
using TutorFlow.Core.Conocimiento;
using TutorFlow.Pruebas.Soporte;

namespace TutorFlow.Core.Tests.Asistente;

public sealed class VerificadorSalidaTests
{
    private static readonly DirectorioAcademico Directorio = DatosReferencia.Cargar().Directorio;
    private static readonly BaseConocimientoFalsa Base = BaseConocimientoFalsa.Vigente();
    private static readonly VerificadorSalida Verificador = new();
    private static readonly Usuario Estudiante = new(Rol.Estudiante, "EST-001");
    private static readonly Usuario Coordinador = new(Rol.Coordinador, "COORD-01");
    private static readonly string[] SoloEst001 = ["EST-001"];
    private static readonly string[] Est001YSuCoordinador = ["EST-001", "COORD-01"];
    private static readonly string[] ProgramaIngSis = ["COORD-01", "EST-001", "EST-002", "EST-003"];
    private static readonly string[] FuentePromedio = ["Promedio del periodo: 4.39. Cálculo Integral 4.5, Bases de Datos 4.1"];
    private static readonly string[] SinFuentes = [];

    private static SalidaVerificada Verificar(Usuario usuario, string respuesta, IReadOnlyCollection<string> autorizados, IReadOnlyCollection<string> fuentes, params Cita[] citas) =>
        Verificador.Verificar(usuario, respuesta, citas, autorizados, fuentes, Base, Directorio);

    private static bool CifrasVerificables(string respuesta, string fuente) =>
        Verificar(Estudiante, respuesta, SoloEst001, [fuente]).Resultado != ResultadoAuditoria.NumeroNoVerificado;

    [Theory]
    [InlineData("Andres  Martinez sacó 2.6 en Cálculo")]
    [InlineData("Andrés Martínez perdió la materia")]
    [InlineData("MARTINEZ ANDRES tiene notas bajas")]
    [InlineData("Las notas de EST-002 son bajas")]
    [InlineData("Las notas de est 002 son bajas")]
    [InlineData("Las notas de EST002 son bajas")]
    [InlineData("Camilo Rojas va bien")]
    [Trait("CA", "CA-05")]
    [Trait("SEG", "SEG-10")]
    public void Respuesta_con_persona_no_autorizada_se_reemplaza_por_la_negativa_del_rol(string respuesta)
    {
        var salida = Verificar(Estudiante, respuesta, SoloEst001, SinFuentes);

        Assert.Equal(Textos.NegativaEstudiante, salida.Respuesta);
        Assert.Equal(ResultadoAuditoria.FugaBloqueada, salida.Resultado);
    }

    [Fact]
    [Trait("CA", "CA-05")]
    [Trait("SEG", "SEG-10")]
    public void Fuga_en_la_respuesta_a_un_coordinador_usa_la_negativa_del_coordinador()
    {
        var salida = Verificar(Coordinador, "Camilo Rojas perdió Microeconomía", ProgramaIngSis, SinFuentes);

        Assert.Equal(Textos.NegativaCoordinador, salida.Respuesta);
        Assert.Equal(ResultadoAuditoria.FugaBloqueada, salida.Resultado);
    }

    [Theory]
    [InlineData("Tu promedio es 4.39 y eres Laura Gómez")]
    [InlineData("Tu coordinadora es Marcela Ríos")]
    [Trait("ESC", "ESC-123")]
    [Trait("SEG", "SEG-10")]
    public void Personas_autorizadas_pueden_aparecer_en_la_respuesta(string respuesta)
    {
        var salida = Verificar(Estudiante, respuesta, Est001YSuCoordinador, FuentePromedio);

        Assert.Equal(respuesta, salida.Respuesta);
        Assert.Equal(ResultadoAuditoria.Respondida, salida.Resultado);
    }

    [Fact]
    [Trait("SEG", "SEG-10")]
    public void Sin_autorizar_al_coordinador_su_nombre_en_la_respuesta_es_una_fuga()
    {
        var salida = Verificar(Estudiante, "Tu coordinadora es Marcela Ríos", SoloEst001, SinFuentes);

        Assert.Equal(ResultadoAuditoria.FugaBloqueada, salida.Resultado);
    }

    [Fact]
    [Trait("ESC", "ESC-120")]
    [Trait("SEG", "SEG-04a")]
    public void Codigo_de_asignatura_con_forma_de_id_no_es_una_fuga()
    {
        var salida = Verificar(Estudiante, "En EST-103 aún no tienes definitiva", SoloEst001, SinFuentes);

        Assert.Equal(ResultadoAuditoria.Respondida, salida.Resultado);
    }

    [Theory]
    [InlineData("Tu promedio es 4.40")]
    [InlineData("Tu promedio es 4.3")]
    [InlineData("Tu promedio es 9.99")]
    [InlineData("Perdiste por más del 20% de faltas")]
    [Trait("CA", "CA-05")]
    [Trait("ESC", "ESC-087")]
    [Trait("SEG", "SEG-10")]
    public void Numero_con_forma_de_nota_o_porcentaje_ausente_de_las_fuentes_no_es_verificable(string respuesta)
    {
        var salida = Verificar(Estudiante, respuesta, SoloEst001, FuentePromedio, new Cita(BaseConocimientoFalsa.Reglamento, "Artículo 5"));

        Assert.Equal(Textos.NoVerificable, salida.Respuesta);
        Assert.Empty(salida.Citas);
        Assert.Equal(ResultadoAuditoria.NumeroNoVerificado, salida.Resultado);
    }

    [Theory]
    [InlineData("Tu promedio es 4.39")]
    [InlineData("Cálculo Integral 4.5 y Bases de Datos 4.1")]
    [InlineData("Tienes 2 faltas de 48 clases")]
    [InlineData("Bienestar atiende de 8:00 a 17:00")]
    [InlineData("Puedes ver 3 secciones")]
    [Trait("CA", "CA-01")]
    [Trait("SEG", "SEG-10")]
    public void Numeros_presentes_en_las_fuentes_o_sin_forma_de_nota_son_verificables(string respuesta)
    {
        var salida = Verificar(Estudiante, respuesta, SoloEst001, FuentePromedio);

        Assert.Equal(respuesta, salida.Respuesta);
        Assert.Equal(ResultadoAuditoria.Respondida, salida.Resultado);
    }

    [Fact]
    [Trait("SEG", "SEG-10")]
    public void Porcentaje_presente_en_los_textos_fuente_es_verificable()
    {
        string[] fuentes = ["El estudiante que falte a más del 15% de las clases pierde la asignatura."];

        var salida = Verificar(Estudiante, "Se pierde con más del 15% de faltas", SoloEst001, fuentes);

        Assert.Equal(ResultadoAuditoria.Respondida, salida.Resultado);
    }

    [Fact]
    [Trait("SEG", "SEG-10")]
    public void La_fuga_se_evalua_antes_que_los_numeros()
    {
        var salida = Verificar(Estudiante, "Andres  Martinez sacó 2.6", SoloEst001, SinFuentes);

        Assert.Equal(ResultadoAuditoria.FugaBloqueada, salida.Resultado);
    }

    [Fact]
    [Trait("CA", "CA-05")]
    [Trait("ESC", "ESC-081")]
    [Trait("SEG", "SEG-10")]
    public void Citas_inexistentes_en_el_indice_se_descartan()
    {
        var existente = new Cita(BaseConocimientoFalsa.Reglamento, "Artículo 5");
        var citas = new[]
        {
            existente,
            new Cita(BaseConocimientoFalsa.Reglamento, "Artículo 99"),
            new Cita("reglamento_academico_2023.md", "Artículo 11"),
            new Cita(BaseConocimientoFalsa.Bienestar, "Anexo")
        };

        var resultado = Verificador.DescartarCitasInexistentes(citas, Base);

        Assert.Equal([existente], resultado.ToArray());
    }

    [Fact]
    [Trait("ESC", "ESC-081")]
    [Trait("SEG", "SEG-10")]
    public void Verificar_conserva_solo_las_citas_existentes()
    {
        var existente = new Cita(BaseConocimientoFalsa.Evaluacion, "Sección 4");

        var salida = Verificar(Estudiante, "Se promedia ponderado por créditos", SoloEst001, SinFuentes, existente, new Cita("reglamento_academico_2023.md", "Artículo 11"));

        Assert.Equal([existente], salida.Citas.ToArray());
    }

    [Theory]
    [InlineData("EST007 tiene Estadística perdida.")]
    [InlineData("EST 099 tiene un promedio de 4.39.")]
    [InlineData("Según COORD-07, tu promedio es 4.39.")]
    [InlineData("est-099 va bien")]
    [InlineData("COORD 07 lo confirma")]
    [InlineData("Tu coordinador es COORD-02")]
    [Trait("CA", "CA-05")]
    [Trait("ESC", "ESC-176")]
    [Trait("SEG", "SEG-10b")]
    public void Un_token_con_forma_de_id_de_persona_no_autorizado_se_bloquea_exista_o_no(string respuesta)
    {
        var salida = Verificar(Estudiante, respuesta, Est001YSuCoordinador, SinFuentes);

        Assert.Equal(Textos.NegativaEstudiante, salida.Respuesta);
        Assert.Equal([Textos.CitaNegativa()], salida.Citas.ToArray());
        Assert.Equal(ResultadoAuditoria.FugaBloqueada, salida.Resultado);
    }

    [Theory]
    [InlineData("EST-004 tiene todas sus asignaturas aprobadas.")]
    [InlineData("EST007 tiene Estadística perdida.")]
    [InlineData("COORD-02 coordina Administración.")]
    [Trait("CA", "CA-05")]
    [Trait("ESC", "ESC-176")]
    [Trait("SEG", "SEG-10b")]
    public void Un_id_ajeno_o_inventado_en_la_respuesta_a_un_coordinador_usa_la_negativa_del_coordinador(string respuesta)
    {
        var salida = Verificar(Coordinador, respuesta, ProgramaIngSis, SinFuentes);

        Assert.Equal(Textos.NegativaCoordinador, salida.Respuesta);
        Assert.Equal([Textos.CitaNegativa()], salida.Citas.ToArray());
        Assert.Equal(ResultadoAuditoria.FugaBloqueada, salida.Resultado);
    }

    [Fact]
    [Trait("CA", "CA-05")]
    [Trait("ESC", "ESC-176")]
    [Trait("SEG", "SEG-10b")]
    public void La_salida_es_la_misma_si_el_id_ajeno_existe_o_no()
    {
        var inexistente = Verificar(Estudiante, "EST007 tiene Estadística perdida.", SoloEst001, SinFuentes);
        var existente = Verificar(Estudiante, "EST-002 tiene Estadística perdida.", SoloEst001, SinFuentes);

        Assert.Equal(existente.Respuesta, inexistente.Respuesta);
        Assert.Equal(existente.Citas.ToArray(), inexistente.Citas.ToArray());
        Assert.Equal(existente.Resultado, inexistente.Resultado);
    }

    [Theory]
    [InlineData("EST-005 tiene dos asignaturas perdidas.")]
    [InlineData("EST005 tiene dos asignaturas perdidas.")]
    [InlineData("Mateo Vargas tiene Estadística (EST-103) sin definitiva.")]
    [InlineData("Valentina Castro (EST-005) está en prueba académica.")]
    [Trait("ESC", "ESC-176")]
    [Trait("SEG", "SEG-10b")]
    public void El_id_de_una_persona_autorizada_y_los_codigos_de_asignatura_no_se_bloquean(string respuesta)
    {
        string[] autorizados = ["COORD-02", "EST-004", "EST-005", "EST-006"];

        var salida = Verificar(new Usuario(Rol.Coordinador, "COORD-02"), respuesta, autorizados, SinFuentes);

        Assert.Equal(respuesta, salida.Respuesta);
        Assert.Equal(ResultadoAuditoria.Respondida, salida.Resultado);
    }

    [Theory]
    [InlineData("Promedio de 4.3 o más (Sección 3.1)", "Promedio igual o superior a 4.3", true)]
    [InlineData("Se aprueba con un promedio de 4.3 y 12 créditos (bienestar_y_becas.md · Sección 3.1).", "Promedio igual o superior a 4.3", true)]
    [InlineData("Estos requisitos se respaldan en bienestar_y_becas.md · Sección 3.1.", "", true)]
    [InlineData("Tu promedio es 4.39 (evaluacion_y_calificaciones.md · Secciones 3 y 4).", "Promedio 4.39", true)]
    [InlineData("Se respalda en las Secciones 3.1 y 3.2.", "", true)]
    [InlineData("Consulta el Artículo 5 y el Art. 11.", "", true)]
    [InlineData("Consulta los artículos 5, 6 y 7.", "", true)]
    [InlineData("Consulta el § 3.1.", "", true)]
    [InlineData("Tu promedio es 4.40 (Sección 4).", "Promedio 4.39", false)]
    [InlineData("Una asignatura se aprueba con 3.7 según el Artículo 5.", "Se aprueba con 3.0 en una escala de 0.0 a 5.0", false)]
    [InlineData("La Sección 3.1 exige 4.4.", "Promedio igual o superior a 4.3", false)]
    [InlineData("Un promedio de 3.1 en la sección 2.", "", false)]
    [InlineData("El Artículo 11 exige 15%.", "", false)]
    [Trait("CA", "CA-01")]
    [Trait("ESC", "ESC-174")]
    [Trait("SEG", "SEG-10c")]
    public void Los_numeros_de_una_referencia_a_seccion_no_se_verifican_pero_las_demas_cifras_si(string respuesta, string fuente, bool esperado)
    {
        Assert.Equal(esperado, CifrasVerificables(respuesta, fuente));
    }

    [Theory]
    [InlineData("Tu promedio es 4.40 (Sección 4).")]
    [InlineData("Una asignatura se aprueba con 3.7 según el Artículo 5.")]
    [Trait("ESC", "ESC-174")]
    [Trait("SEG", "SEG-10c")]
    public void Una_cifra_no_respaldada_junto_a_una_referencia_reemplaza_la_respuesta_por_texto_no_verificable(string respuesta)
    {
        var salida = Verificar(Estudiante, respuesta, SoloEst001, FuentePromedio);

        Assert.Equal(Textos.NoVerificable, salida.Respuesta);
        Assert.Empty(salida.Citas);
        Assert.Equal(ResultadoAuditoria.NumeroNoVerificado, salida.Resultado);
    }

    [Fact]
    [Trait("SEG", "SEG-10")]
    public void Contiene_persona_no_autorizada_respeta_la_lista_de_autorizados()
    {
        Assert.True(Verificador.ContienePersonaNoAutorizada("habla de Laura Gómez", [], Directorio));
        Assert.False(Verificador.ContienePersonaNoAutorizada("habla de Laura Gómez", SoloEst001, Directorio));
    }

    [Theory]
    [InlineData("promedio 4.39", "promedio 4.39", true)]
    [InlineData("promedio 4.3", "promedio 4.39", false)]
    [InlineData("promedio 4.39", "promedio 4.3", false)]
    [InlineData("0.0", "definitiva 0.0 por inasistencia", true)]
    [InlineData("sin numeros", "", true)]
    [Trait("SEG", "SEG-10")]
    public void La_verificacion_de_cifras_compara_numeros_completos(string respuesta, string fuente, bool esperado)
    {
        Assert.Equal(esperado, CifrasVerificables(respuesta, fuente));
    }
}
