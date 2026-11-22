using TutorFlow.Core.Academico;

namespace TutorFlow.Core.Tests.Academico;

public sealed class HabilitacionTests
{
    [Theory]
    [InlineData(2.0, false, true)]
    [InlineData(2.3, false, true)]
    [InlineData(2.9, false, true)]
    [InlineData(1.9, false, false)]
    [InlineData(1.8, false, false)]
    [InlineData(3.0, false, false)]
    [InlineData(4.5, false, false)]
    [InlineData(2.5, true, false)]
    [InlineData(0.0, true, false)]
    [Trait("RN", "RN-10")]
    public void Habilita_con_definitiva_desde_2_0_hasta_antes_de_3_0_y_sin_perdida_por_inasistencia(double definitiva, bool perdidaPorInasistencia, bool esperado)
    {
        Assert.Equal(esperado, CalculosAcademicos.PuedeHabilitar((decimal)definitiva, perdidaPorInasistencia));
    }

    [Fact]
    [Trait("RN", "RN-10")]
    public void Sin_definitiva_no_se_puede_habilitar()
    {
        Assert.False(CalculosAcademicos.PuedeHabilitar(null, false));
    }
}
