using System.Text.RegularExpressions;
using TutorFlow.Core.Autorizacion;

namespace TutorFlow.Core.Asistente;

internal sealed record TramoDePersona(string PersonaId, IReadOnlyList<TokenTexto> Tokens);

internal static partial class TramosDePersonas
{
    public static IEnumerable<string> Oraciones(string respuesta) =>
        CorteDeOracion().Split(respuesta).Where(oracion => !string.IsNullOrWhiteSpace(oracion));

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
        return [.. menciones.GroupBy(mencion => mencion.Id)
            .Select(grupo => TramoDe(grupo.Key, grupo.First().Inicio, menciones, tokens, primeraDeCualquiera))];
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

    [GeneratedRegex(@"\r?\n|(?<=[.!?;])(?=\s|$)", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex CorteDeOracion();
}
