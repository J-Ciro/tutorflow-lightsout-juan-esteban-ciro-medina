using System.Text.RegularExpressions;
using TutorFlow.Core.Autorizacion;

namespace TutorFlow.Core.Asistente;

internal sealed record TramoDePersona(string PersonaId, IReadOnlyList<TokenTexto> Tokens);

internal sealed record OracionAnalizada(string Texto, IReadOnlyList<TramoDePersona> Tramos);

internal static partial class TramosDePersonas
{
    private static readonly HashSet<string> PalabrasDelSujetoCoordinado = new(StringComparer.Ordinal) { "y", "e", "id" };

    public static IEnumerable<string> Oraciones(string respuesta) =>
        CorteDeOracion().Split(respuesta).Where(oracion => !string.IsNullOrWhiteSpace(oracion));

    public static IReadOnlyList<OracionAnalizada> DeRespuesta(
        string respuesta,
        Rol rol,
        IReadOnlyCollection<string> idsAutorizados,
        DirectorioAcademico directorio)
    {
        var herencia = new HerenciaEnListas(rol == Rol.Coordinador);
        return [.. SaltoDeLinea().Split(respuesta).SelectMany(linea =>
        {
            var persona = herencia.PersonaHeredada(linea, DetectorTerceros.MencionesDeAutorizados(linea, idsAutorizados, directorio));
            return persona is null
                ? Oraciones(linea).Select(oracion => new OracionAnalizada(oracion, De(oracion, idsAutorizados, directorio)))
                : [new OracionAnalizada(linea, [new TramoDePersona(persona, TokenizadorTexto.Tokenizar(linea))])];
        })];
    }

    public static IReadOnlyList<TramoDePersona> De(
        string oracion,
        IReadOnlyCollection<string> idsAutorizados,
        DirectorioAcademico directorio)
    {
        var menciones = DetectorTerceros.MencionesDeAutorizados(oracion, idsAutorizados, directorio);
        if (menciones.Count == 0)
        {
            return [];
        }

        var tokens = TokenizadorTexto.Tokenizar(oracion);
        var primeraDeCualquiera = menciones[0].Inicio;
        var tramos = menciones.GroupBy(mencion => mencion.Id)
            .Select(grupo => TramoDe(grupo.Key, grupo.First().Inicio, menciones, tokens, primeraDeCualquiera))
            .ToList();
        return Coordinar(tramos, menciones);
    }

    private static TramoDePersona TramoDe(
        string personaId,
        int inicio,
        IReadOnlyList<MencionDeAutorizado> menciones,
        IReadOnlyList<TokenTexto> tokens,
        int primeraDeCualquiera)
    {
        var fin = menciones
            .Where(mencion => mencion.Id != personaId && mencion.Inicio > inicio)
            .Select(mencion => mencion.Inicio)
            .DefaultIfEmpty(int.MaxValue)
            .Min();
        var delTramo = tokens.Where(token => token.Inicio < primeraDeCualquiera || (token.Inicio >= inicio && token.Inicio < fin));
        return new TramoDePersona(personaId, [.. delTramo]);
    }

    private static List<TramoDePersona> Coordinar(List<TramoDePersona> tramos, IReadOnlyList<MencionDeAutorizado> menciones)
    {
        for (var indice = tramos.Count - 2; indice >= 0; indice--)
        {
            if (ComparteElPredicado(tramos[indice], menciones))
            {
                var siguiente = tramos[indice + 1];
                var propios = siguiente.Tokens.Where(token => !EnMencion(token, siguiente.PersonaId, menciones));
                tramos[indice] = tramos[indice] with { Tokens = [.. tramos[indice].Tokens.Union(propios).OrderBy(token => token.Inicio)] };
            }
        }

        return tramos;
    }

    private static bool ComparteElPredicado(TramoDePersona tramo, IReadOnlyList<MencionDeAutorizado> menciones)
    {
        var primera = menciones.First(mencion => mencion.Id == tramo.PersonaId).Inicio;
        return tramo.Tokens
            .Where(token => token.Inicio >= primera && !EnMencion(token, tramo.PersonaId, menciones))
            .All(token => PalabrasDelSujetoCoordinado.Contains(token.Valor));
    }

    private static bool EnMencion(TokenTexto token, string personaId, IReadOnlyList<MencionDeAutorizado> menciones) =>
        menciones.Any(mencion => mencion.Id == personaId && token.Inicio >= mencion.Inicio && token.Inicio < mencion.Fin);

    [GeneratedRegex(@"\r?\n|(?<=[.!?;])(?=\s|$)", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: TiempoLimiteRegex.Milisegundos)]
    private static partial Regex CorteDeOracion();

    [GeneratedRegex(@"\r?\n", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: TiempoLimiteRegex.Milisegundos)]
    private static partial Regex SaltoDeLinea();

    private sealed partial class HerenciaEnListas(bool activa)
    {
        private string? _encabezado;
        private bool _enBloque;

        public string? PersonaHeredada(string linea, IReadOnlyList<MencionDeAutorizado> menciones)
        {
            if (!activa)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(linea) || menciones.Count > 0)
            {
                Reiniciar(menciones.Count > 0 ? menciones[^1].Id : null);
                return null;
            }

            return Continuar(linea);
        }

        private void Reiniciar(string? encabezado)
        {
            _encabezado = encabezado;
            _enBloque = false;
        }

        private string? Continuar(string linea)
        {
            _enBloque = _enBloque || (_encabezado is not null && MarcadorDeLista().IsMatch(linea));
            _encabezado = _enBloque ? _encabezado : null;
            return _encabezado;
        }

        [GeneratedRegex(@"^\s*(?:[-*•]|\d+[.)])\s", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: TiempoLimiteRegex.Milisegundos)]
        private static partial Regex MarcadorDeLista();
    }
}
