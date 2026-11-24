using TutorFlow.Core.Asistente;
using TutorFlow.Core.Autorizacion;

namespace TutorFlow.Core.Tests.Asistente;

public sealed class VentanaDePalabrasM01Tests
{
    private static IReadOnlyList<string> Candidatos(string texto) =>
        [.. EnterosDeAsistencia.Candidatos(texto, TokenizadorTexto.TokenizarConCifras(texto))];

    private static bool VozDeTuEn(string texto) => VozDeTu.Hay(TokenizadorTexto.Tokenizar(texto));

    [Theory]
    [InlineData("9 gatos azules rojos verdes faltas")]
    [InlineData("9 gatos azules rojos verdes inasistencias")]
    [InlineData("9 gatos azules rojos verdes clases")]
    [Trait("Hallazgo", "M-01")]
    public void M01_una_cifra_en_la_posicion_0_no_se_liga_a_una_palabra_clave_en_la_posicion_5(string texto)
    {
        Assert.Empty(Candidatos(texto));
    }

    [Theory]
    [InlineData("9 gatos azules faltas")]
    [InlineData("9 faltas")]
    [Trait("Hallazgo", "M-01")]
    public void M01_una_cifra_en_la_posicion_0_se_liga_a_una_palabra_clave_dentro_de_la_ventana(string texto)
    {
        Assert.Equal(["9"], Candidatos(texto));
    }

    [Theory]
    [InlineData("tu gatos azules rojos definitiva", false)]
    [InlineData("tu gatos azules rojos promedio", false)]
    [InlineData("tu gatos definitiva", true)]
    [InlineData("tu definitiva", true)]
    [Trait("Hallazgo", "M-01")]
    public void M01_la_voz_de_tu_en_la_posicion_0_solo_mira_dos_palabras_hacia_adelante(string texto, bool esperado)
    {
        Assert.Equal(esperado, VozDeTuEn(texto));
    }
}
