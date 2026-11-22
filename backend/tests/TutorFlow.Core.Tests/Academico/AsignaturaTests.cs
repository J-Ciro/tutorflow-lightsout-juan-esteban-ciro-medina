using TutorFlow.Core.Academico;
using TutorFlow.Pruebas.Soporte;

namespace TutorFlow.Core.Tests.Academico;

public sealed class AsignaturaTests
{
    public static TheoryData<string, string, decimal?, EstadoAsignatura, bool> Estados => new()
    {
        { "EST-001", "CAL-201", 4.5m, EstadoAsignatura.Aprobada, false },
        { "EST-001", "POO-202", 4.8m, EstadoAsignatura.Aprobada, false },
        { "EST-001", "BD-203", 4.1m, EstadoAsignatura.Aprobada, false },
        { "EST-001", "ING-204", 4.0m, EstadoAsignatura.Aprobada, false },
        { "EST-002", "CAL-201", 2.6m, EstadoAsignatura.Perdida, true },
        { "EST-002", "POO-202", 3.5m, EstadoAsignatura.Aprobada, false },
        { "EST-002", "BD-203", 0.0m, EstadoAsignatura.PerdidaPorInasistencia, false },
        { "EST-002", "ING-204", 3.2m, EstadoAsignatura.Aprobada, false },
        { "EST-003", "CAL-201", 4.1m, EstadoAsignatura.Aprobada, false },
        { "EST-003", "POO-202", 3.8m, EstadoAsignatura.Aprobada, false },
        { "EST-003", "BD-203", 4.1m, EstadoAsignatura.Aprobada, false },
        { "EST-003", "ING-204", 4.6m, EstadoAsignatura.Aprobada, false },
        { "EST-004", "CON-101", 3.9m, EstadoAsignatura.Aprobada, false },
        { "EST-004", "MIC-102", 3.3m, EstadoAsignatura.Aprobada, false },
        { "EST-004", "EST-103", 4.0m, EstadoAsignatura.Aprobada, false },
        { "EST-004", "COM-104", 4.5m, EstadoAsignatura.Aprobada, false },
        { "EST-005", "CON-101", 3.2m, EstadoAsignatura.Aprobada, false },
        { "EST-005", "MIC-102", 1.8m, EstadoAsignatura.Perdida, false },
        { "EST-005", "EST-103", 2.3m, EstadoAsignatura.Perdida, true },
        { "EST-005", "COM-104", 3.8m, EstadoAsignatura.Aprobada, false },
        { "EST-006", "CON-101", 4.2m, EstadoAsignatura.Aprobada, false },
        { "EST-006", "MIC-102", 3.8m, EstadoAsignatura.Aprobada, false },
        { "EST-006", "EST-103", null, EstadoAsignatura.SinDefinitiva, false },
        { "EST-006", "COM-104", 4.2m, EstadoAsignatura.Aprobada, false }
    };

    public static TheoryData<string, string, int, int?> Faltas => new()
    {
        { "EST-001", "CAL-201", 9, 8 },
        { "EST-001", "POO-202", 7, 7 },
        { "EST-001", "BD-203", 7, 5 },
        { "EST-001", "ING-204", 4, 3 },
        { "EST-003", "ING-204", 4, null },
        { "EST-005", "MIC-102", 7, 2 },
        { "EST-005", "EST-103", 9, 3 }
    };

    private static ResultadoAsignatura Evaluar(string estudianteId, string codigo) =>
        CalculosAcademicos.EvaluarAsignatura(DatosReferencia.Cargar().AsignaturasDe(estudianteId).Single(a => a.Codigo == codigo));

    [Theory]
    [MemberData(nameof(Estados))]
    [Trait("CA", "CA-02")]
    [Trait("RN", "RN-02")]
    [Trait("RN", "RN-07")]
    [Trait("RN", "RN-10")]
    public void Evaluar_asignatura_coincide_con_la_tabla_de_referencia(string estudianteId, string codigo, decimal? definitiva, EstadoAsignatura estado, bool habilitable)
    {
        var resultado = Evaluar(estudianteId, codigo);

        Assert.Equal(definitiva, resultado.Definitiva);
        Assert.Equal(estado, resultado.Estado);
        Assert.Equal(habilitable, resultado.Habilitable);
    }

    [Theory]
    [MemberData(nameof(Faltas))]
    [Trait("RN", "RN-13")]
    public void Evaluar_asignatura_informa_faltas_permitidas_y_restantes(string estudianteId, string codigo, int permitidas, int? restantes)
    {
        var resultado = Evaluar(estudianteId, codigo);

        Assert.Equal(permitidas, resultado.FaltasPermitidas);
        Assert.Equal(restantes, resultado.FaltasRestantes);
    }

    [Fact]
    [Trait("CA", "CA-02")]
    [Trait("RN", "RN-09")]
    public void Evaluar_ingles_de_est_003_queda_sin_registro_de_asistencia_y_no_perdida()
    {
        var resultado = Evaluar("EST-003", "ING-204");

        Assert.Equal(EstadoAsistencia.SinRegistro, resultado.EstadoAsistencia);
        Assert.NotEqual(EstadoAsignatura.PerdidaPorInasistencia, resultado.Estado);
    }

    [Fact]
    [Trait("CA", "CA-02")]
    [Trait("RN", "RN-05")]
    public void Evaluar_estadistica_de_est_006_informa_la_nota_acumulada()
    {
        var resultado = Evaluar("EST-006", "EST-103");

        Assert.NotNull(resultado.Acumulada);
        Assert.Equal(2.4m, resultado.Acumulada.Valor);
    }

    [Fact]
    [Trait("RN", "RN-07")]
    public void Perdida_por_inasistencia_da_cero_aunque_los_cortes_sean_perfectos()
    {
        var asignatura = new Asignatura("X-1", "Perfecta", 3, new NotasCorte(5.0m, 5.0m, 5.0m), new Asistencia(48, 8));

        var resultado = CalculosAcademicos.EvaluarAsignatura(asignatura);

        Assert.Equal(0.0m, resultado.Definitiva);
        Assert.Equal(EstadoAsignatura.PerdidaPorInasistencia, resultado.Estado);
    }

    [Fact]
    [Trait("RN", "RN-07")]
    public void Perdida_por_inasistencia_prevalece_sobre_un_corte_sin_calificar()
    {
        var asignatura = new Asignatura("X-2", "Pendiente", 3, new NotasCorte(4.0m, 4.0m, null), new Asistencia(48, 9));

        var resultado = CalculosAcademicos.EvaluarAsignatura(asignatura);

        Assert.Equal(0.0m, resultado.Definitiva);
        Assert.Equal(EstadoAsignatura.PerdidaPorInasistencia, resultado.Estado);
    }
}
