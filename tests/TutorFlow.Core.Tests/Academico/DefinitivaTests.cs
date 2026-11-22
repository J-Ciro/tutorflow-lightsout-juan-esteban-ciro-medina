using TutorFlow.Core.Academico;
using TutorFlow.Pruebas.Soporte;

namespace TutorFlow.Core.Tests.Academico;

public sealed class DefinitivaTests
{
    private static readonly decimal[] PorcentajesAceptados = [60m, 0.6m];

    public static TheoryData<decimal, decimal, decimal, decimal> EjemplosCa02 => new()
    {
        { 4.2m, 4.5m, 4.6m, 4.5m },
        { 3.5m, 3.2m, 3.6m, 3.5m },
        { 4.0m, 3.9m, 4.2m, 4.1m },
        { 4.5m, 4.4m, 4.7m, 4.6m },
        { 3.8m, 3.5m, 3.9m, 3.8m },
        { 4.1m, 4.0m, 4.3m, 4.2m },
        { 3.6m, 3.9m, 4.1m, 3.9m },
        { 4.4m, 4.4m, 4.5m, 4.4m },
        { 0.0m, 0.0m, 0.0m, 0.0m },
        { 5.0m, 5.0m, 5.0m, 5.0m }
    };

    public static TheoryData<string, string, decimal?> DefinitivasDeReferencia => new()
    {
        { "EST-001", "CAL-201", 4.5m },
        { "EST-001", "POO-202", 4.8m },
        { "EST-001", "BD-203", 4.1m },
        { "EST-001", "ING-204", 4.0m },
        { "EST-002", "CAL-201", 2.6m },
        { "EST-002", "POO-202", 3.5m },
        { "EST-002", "BD-203", 3.8m },
        { "EST-002", "ING-204", 3.2m },
        { "EST-003", "CAL-201", 4.1m },
        { "EST-003", "POO-202", 3.8m },
        { "EST-003", "BD-203", 4.1m },
        { "EST-003", "ING-204", 4.6m },
        { "EST-004", "CON-101", 3.9m },
        { "EST-004", "MIC-102", 3.3m },
        { "EST-004", "EST-103", 4.0m },
        { "EST-004", "COM-104", 4.5m },
        { "EST-005", "CON-101", 3.2m },
        { "EST-005", "MIC-102", 1.8m },
        { "EST-005", "EST-103", 2.3m },
        { "EST-005", "COM-104", 3.8m },
        { "EST-006", "CON-101", 4.2m },
        { "EST-006", "MIC-102", 3.8m },
        { "EST-006", "EST-103", null },
        { "EST-006", "COM-104", 4.2m }
    };

    [Theory]
    [MemberData(nameof(EjemplosCa02))]
    [Trait("CA", "CA-02")]
    [Trait("RN", "RN-03")]
    [Trait("RN", "RN-04")]
    public void Definitiva_se_redondea_a_una_cifra_con_medio_hacia_arriba(decimal c1, decimal c2, decimal c3, decimal esperada)
    {
        var definitiva = CalculosAcademicos.CalcularDefinitiva(new NotasCorte(c1, c2, c3));

        Assert.Equal(esperada, definitiva);
    }

    [Theory]
    [MemberData(nameof(DefinitivasDeReferencia))]
    [Trait("CA", "CA-02")]
    [Trait("RN", "RN-03")]
    public void Definitiva_de_los_datos_de_referencia_coincide_con_la_spec(string estudianteId, string codigo, decimal? esperada)
    {
        var asignatura = DatosReferencia.Cargar().AsignaturasDe(estudianteId).Single(a => a.Codigo == codigo);

        var definitiva = CalculosAcademicos.CalcularDefinitiva(asignatura.Cortes);

        Assert.Equal(esperada, definitiva);
    }

    [Theory]
    [InlineData(null, 4.0, 4.0)]
    [InlineData(4.0, null, 4.0)]
    [InlineData(4.0, 4.0, null)]
    [Trait("CA", "CA-02")]
    [Trait("RN", "RN-05")]
    public void Definitiva_no_existe_si_algun_corte_es_nulo(double? c1, double? c2, double? c3)
    {
        var cortes = new NotasCorte((decimal?)c1, (decimal?)c2, (decimal?)c3);

        Assert.Null(CalculosAcademicos.CalcularDefinitiva(cortes));
    }

    [Fact]
    [Trait("CA", "CA-02")]
    [Trait("RN", "RN-05")]
    public void Nota_acumulada_de_est_006_es_2_4_sobre_el_60_por_ciento_evaluado()
    {
        var estadistica = DatosReferencia.Cargar().AsignaturasDe("EST-006").Single(a => a.Codigo == "EST-103");

        var acumulada = CalculosAcademicos.CalcularNotaAcumulada(estadistica.Cortes);

        Assert.NotNull(acumulada);
        Assert.Equal(2.4m, acumulada.Valor);
        Assert.Equal(4.0m, acumulada.EquivalenteSobreEvaluado);
        Assert.Contains(acumulada.PorcentajeEvaluado, PorcentajesAceptados);
    }

    [Theory]
    [InlineData(4.45, 1, 4.5)]
    [InlineData(4.44, 1, 4.4)]
    [InlineData(3.45, 1, 3.5)]
    [InlineData(4.05, 1, 4.1)]
    [InlineData(4.55, 1, 4.6)]
    [InlineData(3.75, 1, 3.8)]
    [InlineData(4.15, 1, 4.2)]
    [InlineData(2.275, 2, 2.28)]
    [InlineData(2.674, 2, 2.67)]
    [InlineData(2.5, 0, 3)]
    [InlineData(0.5, 0, 1)]
    [Trait("CA", "CA-02")]
    [Trait("RN", "RN-04")]
    [Trait("RN", "RN-06")]
    public void Redondear_lleva_el_punto_medio_hacia_arriba(double valor, int decimales, double esperado)
    {
        Assert.Equal((decimal)esperado, CalculosAcademicos.Redondear((decimal)valor, decimales));
    }
}
