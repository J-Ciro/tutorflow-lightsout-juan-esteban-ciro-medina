using TutorFlow.Core.Academico;
using TutorFlow.Pruebas.Soporte;

namespace TutorFlow.Core.Tests.Academico;

public sealed class PromedioTests
{
    public static TheoryData<string, decimal, bool, int, ElegibilidadBeca> Resumenes => new()
    {
        { "EST-001", 4.39m, false, 12, ElegibilidadBeca.Elegible },
        { "EST-002", 2.28m, true, 12, ElegibilidadBeca.NoElegible },
        { "EST-003", 4.11m, false, 12, ElegibilidadBeca.NoElegible },
        { "EST-004", 3.88m, false, 12, ElegibilidadBeca.NoElegible },
        { "EST-005", 2.65m, true, 12, ElegibilidadBeca.NoElegible },
        { "EST-006", 4.05m, false, 12, ElegibilidadBeca.NoDeterminable }
    };

    private static ResultadoAsignatura Resultado(decimal nota, int creditos, int clases = 48, int faltas = 0) =>
        CalculosAcademicos.EvaluarAsignatura(new Asignatura("S-" + creditos, "Sintetica", creditos, new NotasCorte(nota, nota, nota), new Asistencia(clases, faltas)));

    [Theory]
    [MemberData(nameof(Resumenes))]
    [Trait("CA", "CA-02")]
    [Trait("RN", "RN-06")]
    [Trait("RN", "RN-11")]
    [Trait("RN", "RN-12")]
    public void Resumen_del_estudiante_coincide_con_la_tabla_de_referencia(string estudianteId, decimal promedio, bool prueba, int creditos, ElegibilidadBeca beca)
    {
        var resumen = CalculosAcademicos.ResumirEstudiante(estudianteId, DatosReferencia.Cargar().AsignaturasDe(estudianteId));

        Assert.Equal(estudianteId, resumen.EstudianteId);
        Assert.Equal(promedio, resumen.Promedio);
        Assert.Equal(prueba, resumen.PruebaAcademica);
        Assert.Equal(creditos, resumen.CreditosMatriculados);
        Assert.Equal(beca, resumen.Beca);
        Assert.Equal(4, resumen.Asignaturas.Count);
    }

    [Fact]
    [Trait("CA", "CA-02")]
    [Trait("RN", "RN-08")]
    public void Promedio_de_est_002_cuenta_cero_en_la_asignatura_perdida_por_inasistencia_y_redondea_hacia_arriba()
    {
        var resultados = DatosReferencia.Cargar().AsignaturasDe("EST-002").Select(CalculosAcademicos.EvaluarAsignatura).ToList();

        Assert.Equal(2.28m, CalculosAcademicos.CalcularPromedioPonderado(resultados));
    }

    [Fact]
    [Trait("RN", "RN-06")]
    public void Promedio_sin_asignaturas_con_definitiva_no_existe()
    {
        Assert.Null(CalculosAcademicos.CalcularPromedioPonderado([]));
    }

    [Fact]
    [Trait("RN", "RN-08")]
    public void Promedio_incluye_la_perdida_por_inasistencia_con_cero()
    {
        var resultados = new[] { Resultado(4.0m, 3), Resultado(5.0m, 3, 48, 8) };

        Assert.Equal(2.0m, CalculosAcademicos.CalcularPromedioPonderado(resultados));
    }

    [Theory]
    [InlineData(2.99, true)]
    [InlineData(2.65, true)]
    [InlineData(3.0, false)]
    [InlineData(4.39, false)]
    [Trait("RN", "RN-11")]
    public void Prueba_academica_solo_con_promedio_menor_a_tres(double promedio, bool esperado)
    {
        Assert.Equal(esperado, CalculosAcademicos.EstaEnPruebaAcademica((decimal)promedio));
    }

    [Fact]
    [Trait("RN", "RN-12")]
    public void Beca_es_elegible_con_promedio_exacto_de_4_3_y_doce_creditos()
    {
        var resultados = new[] { Resultado(4.3m, 4), Resultado(4.3m, 4), Resultado(4.3m, 4) };
        var promedio = CalculosAcademicos.CalcularPromedioPonderado(resultados);

        Assert.Equal(4.3m, promedio);
        Assert.Equal(ElegibilidadBeca.Elegible, CalculosAcademicos.EvaluarBeca(resultados, promedio));
    }

    [Fact]
    [Trait("RN", "RN-12")]
    public void Beca_no_es_elegible_con_promedio_de_4_2()
    {
        var resultados = new[] { Resultado(4.2m, 4), Resultado(4.2m, 4), Resultado(4.2m, 4) };

        Assert.Equal(ElegibilidadBeca.NoElegible, CalculosAcademicos.EvaluarBeca(resultados, CalculosAcademicos.CalcularPromedioPonderado(resultados)));
    }

    [Fact]
    [Trait("RN", "RN-12")]
    public void Beca_no_es_elegible_con_menos_de_doce_creditos()
    {
        var resultados = new[] { Resultado(5.0m, 4), Resultado(5.0m, 4), Resultado(5.0m, 3) };

        Assert.Equal(ElegibilidadBeca.NoElegible, CalculosAcademicos.EvaluarBeca(resultados, CalculosAcademicos.CalcularPromedioPonderado(resultados)));
    }

    [Fact]
    [Trait("RN", "RN-12")]
    public void Beca_no_es_elegible_con_una_asignatura_perdida_por_inasistencia_aunque_el_promedio_supere_4_3()
    {
        var resultados = new[]
        {
            Resultado(5.0m, 4), Resultado(5.0m, 4), Resultado(5.0m, 4), Resultado(5.0m, 4), Resultado(5.0m, 4),
            Resultado(5.0m, 3, 48, 8)
        };
        var promedio = CalculosAcademicos.CalcularPromedioPonderado(resultados);

        Assert.True(promedio >= 4.3m);
        Assert.Equal(ElegibilidadBeca.NoElegible, CalculosAcademicos.EvaluarBeca(resultados, promedio));
    }

    [Fact]
    [Trait("RN", "RN-12")]
    [Trait("ESC", "ESC-030")]
    public void Beca_no_es_determinable_con_una_asignatura_sin_definitiva_aunque_el_promedio_supere_4_3()
    {
        var pendiente = CalculosAcademicos.EvaluarAsignatura(new Asignatura("P-1", "Pendiente", 4, new NotasCorte(5.0m, 5.0m, null), new Asistencia(48, 0)));
        var resultados = new[] { Resultado(5.0m, 4), Resultado(5.0m, 4), pendiente };

        Assert.Equal(ElegibilidadBeca.NoDeterminable, CalculosAcademicos.EvaluarBeca(resultados, CalculosAcademicos.CalcularPromedioPonderado(resultados)));
    }
}
