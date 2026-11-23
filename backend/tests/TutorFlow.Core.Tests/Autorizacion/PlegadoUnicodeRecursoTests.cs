using System.Reflection;
using System.Text.RegularExpressions;
using TutorFlow.Core.Autorizacion;

namespace TutorFlow.Core.Tests.Autorizacion;

public sealed partial class PlegadoUnicodeRecursoTests
{
    private const string NombreDelRecurso = "TutorFlow.Core.Autorizacion.PlegadoUnicode.tsv";
    private const string NombreDeLaClase = "TutorFlow.Core.Autorizacion.PlegadoCompatibilidad";
    private const BindingFlags Miembros = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

    public static TheoryData<string, string> ConfusablesDeD61 => new()
    {
        { "Е", "e" }, { "Ѕ", "s" }, { "Т", "t" }, { "С", "c" }, { "О", "o" }, { "Ε", "e" }, { "Τ", "t" }, { "А", "a" },
        { "ŧ", "t" }, { "Ŧ", "t" }, { "đ", "d" }, { "Đ", "d" }, { "ł", "l" }, { "Ł", "l" }, { "ø", "o" }, { "Ø", "o" }
    };

    public static TheoryData<string, string> CaracteresQueNoSeReasignan => new()
    {
        { "0", "0" }, { "1", "1" }, { "l", "l" }, { "I", "i" }, { "O", "o" }, { "á", "a" }, { "ñ", "n" }, { "ü", "u" }, { "e", "e" }, { "5", "5" }
    };

    private static Stream? Recurso() => typeof(PoliticaAcceso).Assembly.GetManifestResourceStream(NombreDelRecurso);

    private static string TextoDelRecurso()
    {
        using var flujo = Recurso() ?? throw new InvalidOperationException($"Core no incrusta el recurso {NombreDelRecurso} (plan §9.1, D-61, D-62 E6).");
        using var lector = new StreamReader(flujo);
        return lector.ReadToEnd();
    }

    private static Type TipoDelPlegado() =>
        typeof(PoliticaAcceso).Assembly.GetType(NombreDeLaClase)
            ?? throw new InvalidOperationException($"Core no define {NombreDeLaClase}.");

    private static string VersionDeLaClase(string miembro) =>
        (TipoDelPlegado().GetProperty(miembro, Miembros)?.GetValue(null) ?? TipoDelPlegado().GetField(miembro, Miembros)?.GetValue(null)) as string
            ?? throw new InvalidOperationException($"{NombreDeLaClase} no expone {miembro} como texto MAYOR.MENOR.PARCHE (plan §9.1, D-62 E6).");

    private static string VersionDeclarada(string archivo)
    {
        var linea = TextoDelRecurso().Split('\n').FirstOrDefault(l => l.Contains(archivo, StringComparison.OrdinalIgnoreCase) && Version().IsMatch(l));
        return linea is null ? string.Empty : Version().Match(linea).Value;
    }

    [GeneratedRegex(@"\d+\.\d+\.\d+", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex Version();

    [Fact]
    [Trait("CA", "CA-24")]
    [Trait("ESC", "ESC-196")]
    [Trait("D", "D-62")]
    public void ESC_196_El_recurso_PlegadoUnicode_tsv_esta_incrustado_en_Core_y_no_esta_vacio()
    {
        using var flujo = Recurso();

        Assert.NotNull(flujo);
        Assert.True(flujo.Length > 0);
    }

    [Theory]
    [InlineData("VersionUnicode", "UnicodeData")]
    [InlineData("VersionConfusables", "confusables")]
    [Trait("CA", "CA-24")]
    [Trait("ESC", "ESC-196")]
    [Trait("ESC", "ESC-199")]
    [Trait("D", "D-62")]
    public void ESC_196_El_recurso_declara_las_versiones_de_UnicodeData_y_confusables_iguales_a_las_de_PlegadoCompatibilidad(string miembro, string archivo)
    {
        var enLaClase = VersionDeLaClase(miembro);

        Assert.Matches(@"^\d+\.\d+\.\d+$", enLaClase);
        Assert.Equal(enLaClase, VersionDeclarada(archivo));
    }

    [Theory]
    [MemberData(nameof(ConfusablesDeD61))]
    [Trait("CA", "CA-24")]
    [Trait("ESC", "ESC-199")]
    [Trait("D", "D-61")]
    public void ESC_199b_La_tabla_pliega_cada_confusable_y_cada_letra_con_trazo_a_una_letra_latina(string origen, string destino)
    {
        Assert.Equal(destino, NormalizadorTexto.Normalizar(origen));
        Assert.Equal(destino, NormalizadorTexto.Normalizar(NormalizadorTexto.Normalizar(origen)));
    }

    [Theory]
    [MemberData(nameof(CaracteresQueNoSeReasignan))]
    [Trait("CA", "CA-24")]
    [Trait("ESC", "ESC-199")]
    [Trait("D", "D-61")]
    public void ESC_199b_Ningun_caracter_ASCII_ni_Latin_1_comun_se_reasigna_a_otra_letra(string entrada, string esperado)
    {
        Assert.Equal(esperado, NormalizadorTexto.Normalizar(entrada));
    }

    private static List<(string Tipo, int Origen, string Destino)> Filas() =>
        TextoDelRecurso().Split('\n').Skip(2).Where(l => l.Length > 0)
            .Select(l => l.Split('\t'))
            .Select(c => (c[0], int.Parse(c[1], System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture), c.Length > 2 ? c[2] : string.Empty))
            .ToList();

    private static bool EsLetraODigitoAscii(string destino) =>
        destino.Length == 4 && int.TryParse(destino, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out var punto)
        && (punto is >= 0x30 and <= 0x39 or >= 0x41 and <= 0x5A or >= 0x61 and <= 0x7A);

    [Fact]
    [Trait("CA", "CA-24")]
    [Trait("ESC", "ESC-199")]
    [Trait("D", "D-66")]
    public void ESC_199b_Ninguna_fila_conf_tiene_un_origen_de_digito_Nd_ni_un_origen_Latin_1_ni_de_trazo_y_su_destino_es_una_letra_o_digito_ASCII()
    {
        var confusables = Filas().Where(f => f.Tipo == "conf").ToList();
        var trazo = Filas().Where(f => f.Tipo == "trazo").Select(f => f.Origen).ToHashSet();

        Assert.NotEmpty(confusables);
        Assert.DoesNotContain(confusables, f => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(f.Origen) == System.Globalization.UnicodeCategory.DecimalDigitNumber);
        Assert.DoesNotContain(confusables, f => f.Origen <= 0xFF);
        Assert.DoesNotContain(confusables, f => trazo.Contains(f.Origen));
        Assert.DoesNotContain(confusables, f => !EsLetraODigitoAscii(f.Destino));
    }

    [Fact]
    [Trait("CA", "CA-24")]
    [Trait("ESC", "ESC-199")]
    [Trait("D", "D-66")]
    public void ESC_199b_Las_filas_nd_llevan_un_digito_ASCII_y_el_recurso_no_repite_pares_de_tipo_y_origen()
    {
        var filas = Filas();

        Assert.All(filas.Where(f => f.Tipo == "nd"), f => Assert.Matches("^003[0-9]$", f.Destino));
        Assert.Equal(filas.Count, filas.Select(f => (f.Tipo, f.Origen)).Distinct().Count());
        Assert.Equal(8, filas.Count(f => f.Tipo == "trazo"));
    }

    [Fact]
    [Trait("CA", "CA-24")]
    [Trait("ESC", "ESC-196")]
    [Trait("D", "D-51")]
    public void ESC_196_El_codigo_de_Core_no_lleva_la_tabla_de_confusables_como_literal()
    {
        var literales = new[] { "Е", "Ѕ", "Т", "Ŧ", "đ", "\\u0415", "\\u0405", "\\u0422", "\\u0166", "\\u0111" };

        var fuentes = Directory.GetFiles(RutaDeCore(), "*.cs", SearchOption.AllDirectories);

        Assert.NotEmpty(fuentes);
        Assert.All(fuentes, ruta => Assert.DoesNotContain(literales, l => File.ReadAllText(ruta).Contains(l, StringComparison.OrdinalIgnoreCase)));
    }

    private static string RutaDeCore() =>
        Path.Combine(TutorFlow.Pruebas.Soporte.DatosReferencia.RutaRepositorio(), "backend", "src", "TutorFlow.Core");
}
