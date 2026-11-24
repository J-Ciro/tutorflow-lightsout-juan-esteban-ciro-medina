using System.Text.RegularExpressions;
using TutorFlow.Core.Asistente;
using TutorFlow.Core.Conocimiento;
using TutorFlow.Pruebas.Soporte;

namespace TutorFlow.Core.Tests.Asistente;

public sealed partial class PresentacionDeSeccionesTests
{
    private const string ProsaDeLaSeccion2 = "Cada asignatura se evalúa en tres cortes: Corte 1 (semanas 1 a 5), peso 30%; Corte 2 (semanas 6 a 10), peso 30%; Corte 3 (semanas 11 a 16), peso 40%.";
    private const string PresentacionDeLaSeccion31 = "Beca por rendimiento académico: da 50% de descuento en la matrícula del periodo siguiente.\n\nRequisitos:\n1. Promedio ponderado del periodo igual o superior a 4.3.\n2. Ninguna asignatura perdida en el periodo, incluidas las perdidas por inasistencia.\n3. Haber matriculado al menos 12 créditos.";

    private static readonly IReadOnlyList<SeccionDocumento> Indice = BaseConocimientoFalsa.Vigente().Secciones;

    public static TheoryData<string> CifrasDeLaSeccion2 => new() { "30%", "40%", "1", "5", "6", "10", "11", "16", "2", "3" };

    public static TheoryData<string> RaicesDeLaSeccion2 => new() { "cada", "asig", "eval", "tres", "cort", "sema", "peso" };

    private static SeccionDocumento Seccion(string documento, string seccion) =>
        Indice.Single(s => s.Documento == documento && s.Seccion == seccion);

    private static IEnumerable<string> Cifras(string texto) =>
        CifraOEntero().Matches(texto).Select(m => m.Value.Replace(" ", string.Empty, StringComparison.Ordinal)).Distinct();

    [GeneratedRegex(@"(?<![\w.])\d+(?:\.\d+)?(?: ?%)?", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex CifraOEntero();

    [Fact]
    [Trait("CA", "CA-23")]
    [Trait("ESC", "ESC-198")]
    [Trait("D", "D-73")]
    public void ESC_198h3_La_Seccion_2_se_entrega_en_prosa_con_porcentajes_y_sin_tabla()
    {
        var contenido = ConstructorPrompts.ContenidoEntregado(Seccion("evaluacion_y_calificaciones.md", "Sección 2"));

        Assert.Equal(ProsaDeLaSeccion2, contenido);
    }

    [Fact]
    [Trait("CA", "CA-23")]
    [Trait("ESC", "ESC-198")]
    [Trait("D", "D-73")]
    public void ESC_198h1_La_Seccion_3_1_abre_con_el_beneficio_y_trae_los_tres_requisitos()
    {
        var contenido = ConstructorPrompts.ContenidoEntregado(Seccion("bienestar_y_becas.md", "Sección 3.1"));

        Assert.Equal(PresentacionDeLaSeccion31, contenido);
    }

    [Fact]
    [Trait("CA", "CA-23")]
    [Trait("ESC", "ESC-198")]
    [Trait("D", "D-73")]
    public void ESC_198h3_La_Seccion_3_se_entrega_con_su_texto_original()
    {
        var original = Seccion("evaluacion_y_calificaciones.md", "Sección 3");

        var contenido = ConstructorPrompts.ContenidoEntregado(original);

        Assert.Equal($"{original.Titulo}. {original.Texto}", contenido);
    }

    [Theory]
    [MemberData(nameof(CifrasDeLaSeccion2))]
    [Trait("CA", "CA-23")]
    [Trait("ESC", "ESC-198")]
    [Trait("D", "D-73")]
    public void ESC_198j2_Cada_cifra_de_la_presentacion_de_la_Seccion_2_esta_en_el_original_y_en_la_presentacion(string cifra)
    {
        var original = Seccion("evaluacion_y_calificaciones.md", "Sección 2");

        var presentado = ConstructorPrompts.ContenidoEntregado(original);

        Assert.Contains(cifra, Cifras(presentado));
        Assert.Contains(cifra, Cifras($"{original.Titulo} {original.Texto}"));
    }

    [Theory]
    [MemberData(nameof(RaicesDeLaSeccion2))]
    [Trait("CA", "CA-23")]
    [Trait("ESC", "ESC-198")]
    [Trait("D", "D-73")]
    public void ESC_198j2_Cada_raiz_de_la_presentacion_de_la_Seccion_2_esta_en_el_original_y_en_la_presentacion(string raiz)
    {
        var original = Seccion("evaluacion_y_calificaciones.md", "Sección 2");

        var presentado = ConstructorPrompts.ContenidoEntregado(original);

        Assert.Contains(raiz, RaicesDeContenido.De(presentado));
        Assert.Contains(raiz, RaicesDeContenido.De($"{original.Titulo} {original.Texto}"));
    }

    [Fact]
    [Trait("CA", "CA-23")]
    [Trait("ESC", "ESC-198")]
    [Trait("D", "D-73")]
    public void ESC_198j2_La_presentacion_de_la_Seccion_2_no_agrega_cifras_ni_raices_ajenas_al_original()
    {
        var original = Seccion("evaluacion_y_calificaciones.md", "Sección 2");

        var presentado = ConstructorPrompts.ContenidoEntregado(original);

        Assert.Empty(Cifras(presentado).Except(Cifras($"{original.Titulo} {original.Texto}")));
        Assert.Empty(RaicesDeContenido.De(presentado).Except(RaicesDeContenido.De($"{original.Titulo} {original.Texto}")));
    }
}
