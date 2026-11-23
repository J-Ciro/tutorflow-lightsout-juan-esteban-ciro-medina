using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace TutorFlow.Core.Autorizacion;

internal readonly record struct TokenTexto(string Valor, int Inicio, int Fin);

internal static partial class TokenizadorTexto
{
    private const int UnDigito = 1;
    private const int HastaDosDigitos = 2;
    private const int SinLimite = int.MaxValue;
    private static readonly Dictionary<string, string> NombreDelSimbolo = new() { ["≤"] = "<=", ["≥"] = ">=" };

    private const string CaracteresIgnorables = "​‌‍⁠﻿­";

    private static readonly IReadOnlyDictionary<char, char> SinAcento = ConstruirTablaSinAcento(
        ('a', "áàäâãåāăą"),
        ('e', "éèëêēėęě"),
        ('i', "íìïîīįı"),
        ('o', "óòöôõøōő"),
        ('u', "úùüûūůűų"),
        ('n', "ñń"),
        ('c', "çćč"),
        ('y', "ýÿ"));

    public static IReadOnlyList<TokenTexto> Tokenizar(string texto)
    {
        var acumulador = new AcumuladorTokens();
        var inicio = 0;
        foreach (var rune in texto.EnumerateRunes())
        {
            var fin = inicio + rune.Utf16SequenceLength;
            ProcesarRune(acumulador, rune, inicio, fin);
            inicio = fin;
        }

        acumulador.Cerrar();
        return acumulador.Tokens;
    }

    public static IReadOnlyList<TokenTexto> TokenizarConCifras(string texto)
    {
        var tokens = Tokenizar(texto);
        var resultado = new List<TokenTexto>();
        for (var indice = 0; indice < tokens.Count; indice++)
        {
            var esCifra = EsCifraDeNota(texto, tokens, indice);
            resultado.Add(esCifra
                ? new TokenTexto(tokens[indice].Valor + "." + tokens[indice + 1].Valor, tokens[indice].Inicio, tokens[indice + 1].Fin)
                : tokens[indice]);
            indice += esCifra ? 1 : 0;
        }

        return [.. resultado.Concat(Simbolos(texto)).OrderBy(token => token.Inicio)];
    }

    public static int BuscarSecuencia(IReadOnlyList<TokenTexto> tokens, IReadOnlyList<string> secuencia) =>
        BuscarSecuencias(tokens, secuencia).DefaultIfEmpty(-1).First();

    public static IEnumerable<int> BuscarSecuencias(IReadOnlyList<TokenTexto> tokens, IReadOnlyList<string> secuencia) =>
        Enumerable.Range(0, Math.Max(0, tokens.Count - secuencia.Count + 1))
            .Where(indice => CoincideEn(tokens, indice, secuencia));

    public static string Plegado(string texto) =>
        string.Concat(texto.EnumerateRunes()
            .SelectMany(PlegadoCompatibilidad.De)
            .Where(caracter => !EsIgnorable(caracter))
            .Select(Plegar));

    public static IReadOnlyList<string> Valores(string texto) =>
        Tokenizar(texto).Select(token => token.Valor).ToList();

    private static IEnumerable<TokenTexto> Simbolos(string texto) =>
        SimboloDeComparacion().Matches(texto)
            .Select(coincidencia => new TokenTexto(NombreDelSimbolo.GetValueOrDefault(coincidencia.Value, coincidencia.Value), coincidencia.Index, coincidencia.Index + coincidencia.Length));

    private static bool EsCifraDeNota(string texto, IReadOnlyList<TokenTexto> tokens, int indice) =>
        indice + 1 < tokens.Count
        && FormaDeCifra(texto, tokens[indice], tokens[indice + 1])
        && !ContinuaUnaCifra(texto, tokens, indice - 1, indice)
        && !ContinuaUnaCifra(texto, tokens, indice + 1, indice + 2);

    private static bool FormaDeCifra(string texto, TokenTexto entera, TokenTexto decimales) =>
        EsDigitos(entera.Valor, UnDigito) && EsDigitos(decimales.Valor, HastaDosDigitos) && UnidosPorPunto(texto, entera, decimales);

    private static bool ContinuaUnaCifra(string texto, IReadOnlyList<TokenTexto> tokens, int desde, int hasta) =>
        desde >= 0
        && hasta < tokens.Count
        && EsDigitos(tokens[desde].Valor, SinLimite)
        && EsDigitos(tokens[hasta].Valor, SinLimite)
        && UnidosPorPunto(texto, tokens[desde], tokens[hasta]);

    private static bool EsDigitos(string valor, int maximo) =>
        valor.Length <= maximo && valor.All(char.IsAsciiDigit);

    private static bool UnidosPorPunto(string texto, TokenTexto anterior, TokenTexto siguiente) =>
        PlegadoCompatibilidad.Aplicar(texto[anterior.Fin..siguiente.Inicio]) == ".";

    private static bool CoincideEn(IReadOnlyList<TokenTexto> tokens, int indice, IReadOnlyList<string> secuencia) =>
        secuencia.Select((valor, desplazamiento) => tokens[indice + desplazamiento].Valor == valor).All(coincide => coincide);

    private static void ProcesarRune(AcumuladorTokens acumulador, Rune rune, int inicio, int fin)
    {
        foreach (var caracter in PlegadoCompatibilidad.De(rune))
        {
            Procesar(acumulador, caracter, inicio, fin);
        }
    }

    private static void Procesar(AcumuladorTokens acumulador, char caracter, int inicio, int fin)
    {
        if (EsIgnorable(caracter))
        {
            acumulador.Extender(fin);
            return;
        }

        var plegado = Plegar(caracter);
        if (char.IsLetterOrDigit(plegado))
        {
            acumulador.Agregar(plegado, inicio, fin);
            return;
        }

        acumulador.Cerrar();
    }

    private static bool EsIgnorable(char caracter) =>
        CaracteresIgnorables.Contains(caracter)
        || CharUnicodeInfo.GetUnicodeCategory(caracter) == UnicodeCategory.NonSpacingMark;

    private static char Plegar(char caracter)
    {
        var minuscula = char.ToLowerInvariant(caracter);
        return SinAcento.GetValueOrDefault(minuscula, minuscula);
    }

    private static Dictionary<char, char> ConstruirTablaSinAcento(params (char Base, string Variantes)[] grupos) =>
        grupos.SelectMany(grupo => grupo.Variantes.Select(variante => (variante, grupo.Base)))
            .ToDictionary(par => par.variante, par => par.Base);

    private sealed class AcumuladorTokens
    {
        private readonly List<TokenTexto> _tokens = [];
        private readonly StringBuilder _valor = new();
        private int _inicio;
        private int _fin;

        public IReadOnlyList<TokenTexto> Tokens => _tokens;

        public void Agregar(char caracter, int inicio, int fin)
        {
            if (_valor.Length == 0)
            {
                _inicio = inicio;
            }

            _valor.Append(caracter);
            _fin = fin;
        }

        public void Extender(int fin)
        {
            if (_valor.Length > 0)
            {
                _fin = fin;
            }
        }

        public void Cerrar()
        {
            if (_valor.Length == 0)
            {
                return;
            }

            _tokens.Add(new TokenTexto(_valor.ToString(), _inicio, _fin));
            _valor.Clear();
        }
    }

    [GeneratedRegex("<=|>=|[<>≤≥]", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: TiempoLimiteRegex.Milisegundos)]
    private static partial Regex SimboloDeComparacion();
}
