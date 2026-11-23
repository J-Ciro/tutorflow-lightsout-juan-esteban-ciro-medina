using System.Globalization;
using System.Text;

namespace TutorFlow.Core.Autorizacion;

internal readonly record struct TokenTexto(string Valor, int Inicio, int Fin);

internal static class TokenizadorTexto
{
    private const int DesplazamientoAnchoCompleto = 0xFEE0;
    private const char PrimeroAnchoCompleto = '！';
    private const char UltimoAnchoCompleto = '～';
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

    public static int BuscarSecuencia(IReadOnlyList<TokenTexto> tokens, IReadOnlyList<string> secuencia)
    {
        for (var indice = 0; indice + secuencia.Count <= tokens.Count; indice++)
        {
            if (CoincideEn(tokens, indice, secuencia))
            {
                return indice;
            }
        }

        return -1;
    }

    public static string Plegado(string texto) =>
        string.Concat(texto.EnumerateRunes()
            .SelectMany(PlegadoCompatibilidad.De)
            .Where(caracter => !EsIgnorable(caracter))
            .Select(Plegar));

    public static IReadOnlyList<string> Valores(string texto) =>
        Tokenizar(texto).Select(token => token.Valor).ToList();

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
        var sinAnchoCompleto = caracter is >= PrimeroAnchoCompleto and <= UltimoAnchoCompleto
            ? (char)(caracter - DesplazamientoAnchoCompleto)
            : caracter;
        var minuscula = char.ToLowerInvariant(sinAnchoCompleto);
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
}
