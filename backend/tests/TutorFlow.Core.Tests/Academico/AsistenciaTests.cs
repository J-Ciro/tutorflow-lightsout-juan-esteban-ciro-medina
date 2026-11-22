using TutorFlow.Core.Academico;

namespace TutorFlow.Core.Tests.Academico;

public sealed class AsistenciaTests
{
    public static TheoryData<int, int, bool, int> EjemplosCa02 => new()
    {
        { 48, 7, false, 7 },
        { 48, 8, true, 7 },
        { 48, 9, true, 7 },
        { 64, 9, false, 9 },
        { 64, 10, true, 9 },
        { 32, 5, true, 4 },
        { 100, 15, false, 15 },
        { 100, 16, true, 15 },
        { 20, 3, false, 3 },
        { 20, 4, true, 3 },
        { 1, 0, false, 0 },
        { 1, 1, true, 0 }
    };

    [Theory]
    [MemberData(nameof(EjemplosCa02))]
    [Trait("CA", "CA-02")]
    [Trait("RN", "RN-07")]
    [Trait("RN", "RN-13")]
    public void Pierde_solo_si_las_faltas_superan_el_quince_por_ciento(int clases, int faltas, bool pierde, int maximo)
    {
        var asistencia = new Asistencia(clases, faltas);

        Assert.Equal(pierde, CalculosAcademicos.PierdePorInasistencia(asistencia));
        Assert.Equal(maximo, CalculosAcademicos.CalcularFaltasPermitidas(clases));
    }

    [Fact]
    [Trait("CA", "CA-02")]
    [Trait("RN", "RN-09")]
    public void Inasistencias_nulas_no_se_declaran_perdida_y_quedan_sin_registro()
    {
        var asistencia = new Asistencia(32, null);

        Assert.False(CalculosAcademicos.PierdePorInasistencia(asistencia));
        Assert.Equal(EstadoAsistencia.SinRegistro, CalculosAcademicos.ObtenerEstadoAsistencia(asistencia));
        Assert.Null(CalculosAcademicos.CalcularFaltasRestantes(asistencia));
    }

    [Fact]
    [Trait("RN", "RN-09")]
    public void Inasistencias_registradas_dan_estado_registrada()
    {
        var estado = CalculosAcademicos.ObtenerEstadoAsistencia(new Asistencia(48, 0));

        Assert.Equal(EstadoAsistencia.Registrada, estado);
    }

    [Theory]
    [InlineData(48, 2, 5)]
    [InlineData(48, 7, 0)]
    [InlineData(64, 1, 8)]
    [InlineData(32, 1, 3)]
    [Trait("RN", "RN-13")]
    [Trait("ESC", "ESC-027")]
    public void Faltas_restantes_son_el_maximo_permitido_menos_las_registradas(int clases, int faltas, int restantes)
    {
        Assert.Equal(restantes, CalculosAcademicos.CalcularFaltasRestantes(new Asistencia(clases, faltas)));
    }
}
