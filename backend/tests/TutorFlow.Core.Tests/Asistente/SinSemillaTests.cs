using TutorFlow.Core.Asistente;

namespace TutorFlow.Core.Tests.Asistente;

public sealed class SinSemillaTests
{
    [Theory]
    [InlineData(typeof(SolicitudLlm))]
    [InlineData(typeof(AjustesDelModelo))]
    [Trait("CA", "CA-07")]
    [Trait("ESC", "ESC-192")]
    [Trait("D", "D-85")]
    public void ESC_192_h_Ningun_tipo_de_la_solicitud_al_modelo_lleva_semilla(Type tipo)
    {
        Assert.DoesNotContain(tipo.GetProperties(), p => p.Name.Contains("Semilla", StringComparison.OrdinalIgnoreCase) || p.Name.Contains("Seed", StringComparison.OrdinalIgnoreCase));
    }
}
