using System.Text.RegularExpressions;
using TutorFlow.Api.Tests.Dobles;
using TutorFlow.Core.Asistente;
using TutorFlow.Core.Conocimiento;
using TutorFlow.Pruebas.Soporte;

namespace TutorFlow.Api.Tests.Contrato;

public sealed partial class PresentacionDeSeccionYEjemplosApiTests
{
    private const string FuenteBeca31 = "bienestar_y_becas.md · Sección 3.1";
    private const string FuenteBienestar1 = "bienestar_y_becas.md · Sección 1";
    private const string PreguntaP7 = "¿Cuál es el horario de atención de Bienestar?";
    private const string PreguntaP8 = "¿Qué requisitos tiene la beca por rendimiento académico?";
    private const string EncabezadoPresentado = "Requisitos de la beca por rendimiento académico (50% de descuento en la matrícula del periodo siguiente):";
    private const string EncabezadoOriginal = "Descuento del 50% en la matrícula del periodo siguiente. Requisitos:";
    private const string ContenidoPresentado = EncabezadoPresentado + "\n\n1. Promedio ponderado del periodo igual o superior a 4.3.\n2. Ninguna asignatura perdida en el periodo, incluidas las perdidas por inasistencia.\n3. Haber matriculado al menos 12 créditos.";

    private static readonly SeccionDocumento OriginalDeLa31 = BaseConocimientoFalsa.Vigente().Secciones
        .Single(s => s.Documento == "bienestar_y_becas.md" && s.Seccion == "Sección 3.1");

    private static async Task<BloqueDelimitado> BloqueEntregadoAsync(string estudiante, string pregunta, string fuente)
    {
        using var fabrica = new FabricaApi(llm: GuionesLlm.SinHerramientas(_ => "Revisa el detalle."), conocimientoReal: true);

        await fabrica.PreguntarEstudianteAsync(estudiante, pregunta);

        return Assert.Single(GuionesLlm.Bloques(Assert.Single(fabrica.Llm.Solicitudes)), b => b.Fuente == fuente);
    }

    private static IEnumerable<string> Cifras(string texto) =>
        CifraOEntero().Matches(texto).Select(m => m.Value.Replace(" ", string.Empty, StringComparison.Ordinal)).Distinct();

    [GeneratedRegex(@"(?<![\w.])\d+(?:\.\d+)?(?: ?%)?", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex CifraOEntero();

    [Fact]
    [Trait("CA", "CA-23")]
    [Trait("ESC", "ESC-198")]
    [Trait("D", "D-58")]
    public async Task ESC_198h1_El_bloque_de_la_Seccion_3_1_trae_el_encabezado_presentado_y_los_tres_requisitos()
    {
        var bloque = await BloqueEntregadoAsync("EST-004", PreguntaP8, FuenteBeca31);

        Assert.Contains(ContenidoPresentado, bloque.Cuerpo, StringComparison.Ordinal);
        Assert.DoesNotContain(EncabezadoOriginal, bloque.Cuerpo, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("CA", "CA-23")]
    [Trait("ESC", "ESC-198")]
    [Trait("D", "D-58")]
    public async Task ESC_198h2_El_bloque_de_la_Seccion_1_de_Bienestar_conserva_el_titulo_y_el_texto_originales()
    {
        var bloque = await BloqueEntregadoAsync("EST-001", PreguntaP7, FuenteBienestar1);

        Assert.Contains("La oficina de Bienestar atiende en el edificio B, piso 2:", bloque.Cuerpo, StringComparison.Ordinal);
        Assert.Contains("8:00 a 17:00", bloque.Cuerpo, StringComparison.Ordinal);
        Assert.Contains("8:00 a 12:00", bloque.Cuerpo, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("CA", "CA-23")]
    [Trait("ESC", "ESC-198")]
    [Trait("D", "D-58")]
    public async Task ESC_198j_Toda_cifra_y_toda_raiz_de_la_presentacion_de_la_Seccion_3_1_esta_en_el_titulo_o_el_texto_originales()
    {
        var presentado = (await BloqueEntregadoAsync("EST-004", PreguntaP8, FuenteBeca31)).Cuerpo;
        var original = $"{OriginalDeLa31.Titulo} {OriginalDeLa31.Texto}";

        var cifrasQueSobran = Cifras(presentado).Except(Cifras(original)).ToArray();
        var raicesQueSobran = TutorFlow.Core.Conocimiento.RaicesDeContenido.De(presentado).Except(TutorFlow.Core.Conocimiento.RaicesDeContenido.De(original)).ToArray();

        Assert.Empty(cifrasQueSobran);
        Assert.Empty(raicesQueSobran);
        Assert.Contains("50%", Cifras(presentado));
        Assert.Contains("requ", TutorFlow.Core.Conocimiento.RaicesDeContenido.De(presentado));
    }

    [Fact]
    [Trait("CA", "CA-23")]
    [Trait("ESC", "ESC-198")]
    [Trait("D", "D-58")]
    public async Task ESC_198i_Si_el_modelo_copia_las_cifras_de_EJ_2_las_cifras_ajenas_a_las_fuentes_dan_TEXTO_NO_VERIFICABLE_con_numero_no_verificado()
    {
        using var fabrica = new FabricaApi(llm: GuionesLlm.SinHerramientas(_ => "Tu promedio es 2.77. Definitivas: Física 2.7, Química 2.9."), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", "¿Cuál es mi promedio del periodo?");

        Assert.Equal(200, respuesta.Estado);
        Assert.Equal(Textos.NoVerificable, respuesta.Respuesta);
        Assert.Empty(respuesta.Citas);
        Assert.Equal(ResultadoAuditoria.NumeroNoVerificado, Assert.Single(fabrica.Auditoria.Eventos).Resultado);
    }

    [Theory]
    [InlineData("estudiante", "EST-001", PreguntaP7)]
    [InlineData("coordinador", "COORD-02", "¿Qué estudiantes de mi programa tienen alguna asignatura perdida?")]
    [Trait("CA", "CA-23")]
    [Trait("ESC", "ESC-198")]
    [Trait("D", "D-58")]
    public async Task ESC_198i_El_prompt_enviado_lleva_exactamente_cuatro_ejemplos_y_el_documento_ficticio_no_esta_entre_las_secciones_entregadas(string rol, string id, string pregunta)
    {
        using var fabrica = new FabricaApi(llm: GuionesLlm.SinHerramientas(_ => "Revisa el detalle."), conocimientoReal: true);

        await fabrica.PreguntarAsync(rol, id, pregunta);

        var solicitud = Assert.Single(fabrica.Llm.Solicitudes);
        Assert.Equal(4, PartesDelPrompt.CasosDeEjemplo(solicitud.PromptSistema).Count);
        Assert.DoesNotContain(GuionesLlm.Bloques(solicitud), b => b.Fuente.Contains("documento_ejemplo.md", StringComparison.Ordinal));
    }
}
