using TutorFlow.Core.Asistente;
using TutorFlow.Core.Autorizacion;
using static TutorFlow.Core.Tests.Asistente.EscenarioAsistente;
using static TutorFlow.Core.Tests.Asistente.SeccionesDeReferencia;

namespace TutorFlow.Core.Tests.Asistente;

public sealed class HtmlYMayusculaInicialTests
{
    private const string PreguntaPerdidas = "¿Qué estudiantes de mi programa tienen alguna asignatura perdida?";
    private static readonly Usuario Coord02 = new(Rol.Coordinador, "COORD-02");

    public static TheoryData<string> EncabezadosHtmlQueEvadenElBloque => new()
    {
        "<b>Mateo Vargas</b>\nAsignatura perdida: Estadística",
        "<h3>Mateo Vargas</h3>\nAsignatura perdida: Estadística",
        "<strong>Mateo Vargas</strong>\nAsignatura perdida: Estadística",
        "<h1>Mateo Vargas</h1>\nAsignatura perdida: Estadística"
    };

    public static TheoryData<string, string> TextosConPrefijoEliminado => new()
    {
        { "Según el documento de bienestar, la oficina atiende de lunes a viernes.", "La oficina atiende de lunes a viernes." },
        { "Según el documento de bienestar: la oficina atiende de lunes a viernes.", "La oficina atiende de lunes a viernes." },
        { "Según el documento de bienestar; la oficina atiende de lunes a viernes.", "La oficina atiende de lunes a viernes." },
        { "Según el documento de bienestar · la oficina atiende de lunes a viernes.", "La oficina atiende de lunes a viernes." }
    };

    private static EscenarioAsistente ConPrograma(string texto) =>
        Con(SalidasVerificadas.LeeElProgramaYResponde(texto), Indice(Articulo5, Articulo6, Articulo20));

    [Theory]
    [MemberData(nameof(EncabezadosHtmlQueEvadenElBloque))]
    [Trait("CA", "CA-17")]
    [Trait("ESC", "ESC-184")]
    [Trait("D", "D-77")]
    public async Task ESC_184_m_El_encabezado_html_pasa_a_markdown_y_el_bloque_de_Mateo_falla(string texto)
    {
        await ClasesVerificadoras.AfirmarFallaAsync("COORD-02", ReferenciasEscritas.Limpiar(texto, []), "EST-004", "EST-005", "EST-006");
    }

    [Theory]
    [InlineData("<b>Valentina Castro</b>\nAsignatura perdida: Estadística")]
    [InlineData("<h3>Valentina Castro</h3>\nAsignatura perdida: Estadística")]
    [Trait("CA", "CA-17")]
    [Trait("ESC", "ESC-184")]
    [Trait("D", "D-77")]
    public async Task ESC_184_m_La_respuesta_fiel_con_html_se_entrega_sin_etiquetas(string texto)
    {
        var escenario = ConPrograma(texto);

        var resultado = await escenario.PreguntarAsync(Coord02, PreguntaPerdidas);

        Assert.Equal(ResultadoAuditoria.Respondida, resultado.Auditoria);
        Assert.DoesNotContain("<b>", resultado.Respuesta!.Respuesta, StringComparison.Ordinal);
        Assert.DoesNotContain("<h3>", resultado.Respuesta.Respuesta, StringComparison.Ordinal);
        Assert.DoesNotContain("</", resultado.Respuesta.Respuesta, StringComparison.Ordinal);
        Assert.Contains("Valentina Castro", resultado.Respuesta.Respuesta, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("<b>Valentina Castro</b>\nAsignatura perdida: Estadística", "**Valentina Castro**\nAsignatura perdida: Estadística")]
    [InlineData("<strong>Valentina Castro</strong>\nAsignatura perdida: Estadística", "**Valentina Castro**\nAsignatura perdida: Estadística")]
    [InlineData("La oficina <i>atiende</i> hoy.", "La oficina atiende hoy.")]
    [InlineData("Es <b>importante</b> llegar.", "Es importante llegar.")]
    [Trait("CA", "CA-17")]
    [Trait("ESC", "ESC-184")]
    [Trait("D", "D-77")]
    public void ESC_184_m_La_limpieza_convierte_la_linea_de_negrita_y_quita_toda_etiqueta(string entrada, string esperada)
    {
        Assert.Equal(esperada, ReferenciasEscritas.Limpiar(entrada, []));
    }

    [Theory]
    [InlineData("<h1>Valentina Castro</h1>\nHola.")]
    [InlineData("<h3>Valentina Castro</h3>\nHola.")]
    [InlineData("<h6>Valentina Castro</h6>\nHola.")]
    [Trait("CA", "CA-17")]
    [Trait("ESC", "ESC-184")]
    [Trait("D", "D-77")]
    public void ESC_184_m_La_limpieza_convierte_hN_en_un_encabezado_de_markdown(string entrada)
    {
        var limpia = ReferenciasEscritas.Limpiar(entrada, []);

        Assert.StartsWith("#", limpia, StringComparison.Ordinal);
        Assert.Contains(" Valentina Castro\nHola.", limpia, StringComparison.Ordinal);
        Assert.DoesNotContain("<", limpia, StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(TextosConPrefijoEliminado))]
    [Trait("CA", "CA-16")]
    [Trait("ESC", "ESC-198")]
    [Trait("D", "D-91")]
    public void ESC_198_l3_La_mayuscula_inicial_se_restituye_tras_un_prefijo_eliminado_con_signos_intermedios(string entrada, string esperada)
    {
        Assert.Equal(esperada, ReferenciasEscritas.Limpiar(entrada, []));
    }

    [Fact]
    [Trait("CA", "CA-16")]
    [Trait("ESC", "ESC-198")]
    [Trait("D", "D-91")]
    public void ESC_198_l3_Sin_prefijo_eliminado_no_se_cambia_la_minuscula_inicial()
    {
        Assert.Equal("la oficina atiende de lunes a viernes.", ReferenciasEscritas.Limpiar("la oficina atiende de lunes a viernes.", []));
    }
}
