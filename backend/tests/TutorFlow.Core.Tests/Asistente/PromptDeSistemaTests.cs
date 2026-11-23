using System.Text.RegularExpressions;
using TutorFlow.Core.Asistente;
using TutorFlow.Core.Autorizacion;

namespace TutorFlow.Core.Tests.Asistente;

public sealed partial class PromptDeSistemaTests
{
    private const int MaximoDePasos = 12;
    private const int MaximoDePalabrasPorPaso = 40;
    private const string MarcadorCanario = "CANARIO-PROMPT-0123456789";

    public static TheoryData<Rol> Roles => new() { Rol.Estudiante, Rol.Coordinador };

    public static TheoryData<Rol, string, string[]> ReglasDelPrompt => new()
    {
        { Rol.Estudiante, "P-01", ["solo", Textos.SinInformacion] },
        { Rol.Coordinador, "P-01", ["solo", Textos.SinInformacion] },
        { Rol.Estudiante, "P-02", ["lugar", "horario", "montos", "beneficio"] },
        { Rol.Coordinador, "P-02", ["lugar", "horario", "montos", "beneficio"] },
        { Rol.Estudiante, "P-03", ["promedio", "definitivas"] },
        { Rol.Coordinador, "P-03", ["promedio", "definitivas"] },
        { Rol.Estudiante, "P-04", ["(archivo · sección)"] },
        { Rol.Coordinador, "P-04", ["(archivo · sección)"] },
        { Rol.Estudiante, "P-05", ["consecuencias", "consejos"] },
        { Rol.Coordinador, "P-05", ["consecuencias", "consejos"] },
        { Rol.Estudiante, "P-06", ["sin definitiva", "perdida"] },
        { Rol.Coordinador, "P-06", ["sin definitiva", "perdida"] },
        { Rol.Estudiante, "P-07", ["herramientas"] },
        { Rol.Coordinador, "P-07", ["herramientas"] }
    };

    [GeneratedRegex(@"^[ \t]*\d+\.[ \t]+(.*?)(?=^[ \t]*\d+\.[ \t]|\n[ \t]*\r?\n|\z)", RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline, matchTimeoutMilliseconds: 1000)]
    private static partial Regex PasoNumerado();

    private static string Prompt(Rol rol) => ConstructorPrompts.PromptSistema(rol, MarcadorCanario);

    private static string[] Pasos(Rol rol) =>
        [.. PasoNumerado().Matches(ConstructorPrompts.Instrucciones(Prompt(rol))).Select(m => string.Join(' ', m.Groups[1].Value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)))];

    private static int Palabras(string texto) => texto.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;

    [Theory]
    [MemberData(nameof(Roles))]
    [Trait("CA", "CA-20")]
    [Trait("ESC", "ESC-191")]
    [Trait("RF", "P-01")]
    public void ESC_191_Las_instrucciones_son_una_lista_numerada_de_como_maximo_12_pasos(Rol rol)
    {
        var pasos = Pasos(rol);

        Assert.InRange(pasos.Length, 7, MaximoDePasos);
    }

    [Theory]
    [MemberData(nameof(Roles))]
    [Trait("CA", "CA-20")]
    [Trait("ESC", "ESC-191")]
    [Trait("RF", "P-01")]
    public void ESC_191_Cada_paso_tiene_40_palabras_o_menos(Rol rol)
    {
        var pasos = Pasos(rol);

        Assert.NotEmpty(pasos);
        Assert.All(pasos, paso => Assert.True(Palabras(paso) <= MaximoDePalabrasPorPaso, $"{Palabras(paso)} palabras: {paso}"));
    }

    [Theory]
    [MemberData(nameof(ReglasDelPrompt))]
    [Trait("CA", "CA-20")]
    [Trait("ESC", "ESC-191")]
    public void ESC_191_Cada_regla_de_la_9_5_esta_en_un_paso_con_sus_terminos(Rol rol, string regla, string[] terminos)
    {
        Assert.NotEmpty(regla);

        var pasos = Pasos(rol);

        Assert.Contains(pasos, paso => terminos.All(t => paso.Contains(t, StringComparison.OrdinalIgnoreCase)));
    }

    [Theory]
    [MemberData(nameof(Roles))]
    [Trait("CA", "CA-20")]
    [Trait("ESC", "ESC-191")]
    [Trait("D", "D-63")]
    public void ESC_191_El_primer_paso_con_todos_los_terminos_de_cada_una_de_las_7_reglas_es_un_paso_distinto(Rol rol)
    {
        var pasos = Pasos(rol);
        var reglas = ReglasDelPrompt.Where(fila => (Rol)fila[0] == rol).Select(fila => (string[])fila[2]).ToArray();

        var elegidos = reglas
            .Select(terminos => Array.FindIndex(pasos, paso => terminos.All(t => paso.Contains(t, StringComparison.OrdinalIgnoreCase))))
            .ToArray();

        Assert.Equal(7, elegidos.Length);
        Assert.DoesNotContain(-1, elegidos);
        Assert.Equal(7, elegidos.Distinct().Count());
    }

    [Theory]
    [MemberData(nameof(Roles))]
    [Trait("CA", "CA-20")]
    [Trait("ESC", "ESC-191")]
    [Trait("RF", "P-04")]
    public void ESC_191_Ninguna_parte_del_prompt_contiene_documento_md(Rol rol)
    {
        Assert.DoesNotContain("documento.md", Prompt(rol), StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [MemberData(nameof(Roles))]
    [Trait("CA", "CA-20")]
    [Trait("ESC", "ESC-191")]
    public void ESC_191_El_bloque_de_ejemplos_y_el_canario_se_conservan_fuera_de_las_instrucciones(Rol rol)
    {
        var prompt = Prompt(rol);
        var instrucciones = ConstructorPrompts.Instrucciones(prompt);

        Assert.Contains("<ejemplos>", prompt, StringComparison.Ordinal);
        Assert.Contains("</ejemplos>", prompt, StringComparison.Ordinal);
        Assert.DoesNotContain("<ejemplos>", instrucciones, StringComparison.Ordinal);
        Assert.Contains(MarcadorCanario, prompt, StringComparison.Ordinal);
    }
}
