using TutorFlow.Core.Autorizacion;

namespace TutorFlow.Core.Asistente;

internal sealed record TramoDePersona(string PersonaId, IReadOnlyList<TokenTexto> Tokens);

internal sealed record PersonasDeBloque(IReadOnlyList<TramoDePersona> Tramos, bool Ambiguo)
{
    public static PersonasDeBloque Ninguna { get; } = new([], false);
}

internal static class TramosDePersonas
{
    private static readonly HashSet<string> PalabrasDelSujetoCoordinado = new(StringComparer.Ordinal) { "y", "e", "id" };

    public static PersonasDeBloque De(
        BloqueDeTexto bloque,
        Usuario usuario,
        IReadOnlyCollection<string> idsAutorizados,
        DirectorioAcademico directorio)
    {
        var tokens = TokenizadorTexto.Tokenizar(bloque.Texto);
        if (usuario.Rol != Rol.Coordinador)
        {
            return new PersonasDeBloque([new TramoDePersona(usuario.Id, tokens)], false);
        }

        var menciones = DetectorTerceros.MencionesDeAutorizados(bloque.Texto, idsAutorizados, directorio);
        var enOracion = menciones.Where(mencion => mencion.Inicio >= bloque.InicioDeLaOracion).ToList();
        var delContexto = menciones.Where(mencion => mencion.Inicio < bloque.InicioDeLaOracion).Select(mencion => mencion.Id).Distinct().ToList();
        return enOracion.Count == 0 ? SoloDelContexto(delContexto, tokens) : new PersonasDeBloque(DeLaOracion(enOracion, delContexto, tokens), false);
    }

    private static PersonasDeBloque SoloDelContexto(List<string> delContexto, IReadOnlyList<TokenTexto> tokens) =>
        delContexto.Count switch
        {
            0 => PersonasDeBloque.Ninguna,
            1 => new PersonasDeBloque([new TramoDePersona(delContexto[0], tokens)], false),
            _ => new PersonasDeBloque([], true)
        };

    private static List<TramoDePersona> DeLaOracion(List<MencionDeAutorizado> menciones, List<string> delContexto, IReadOnlyList<TokenTexto> tokens)
    {
        var primeraDeCualquiera = menciones[0].Inicio;
        var tramos = menciones.GroupBy(mencion => mencion.Id)
            .Select(grupo => TramoDe(grupo.Key, grupo.First().Inicio, menciones, tokens, primeraDeCualquiera))
            .ToList();
        var coordinados = Coordinar(tramos, menciones);
        return [.. coordinados.Concat(delContexto.Where(id => coordinados.All(tramo => tramo.PersonaId != id)).Select(id => new TramoDePersona(id, tokens)))];
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

    private static List<TramoDePersona> Coordinar(IReadOnlyList<TramoDePersona> tramos, IReadOnlyList<MencionDeAutorizado> menciones)
    {
        var coordinados = tramos.ToList();
        for (var indice = coordinados.Count - 2; indice >= 0; indice--)
        {
            coordinados[indice] = ComparteElPredicado(coordinados[indice], menciones)
                ? coordinados[indice] with { Tokens = [.. coordinados[indice].Tokens.Union(PropiosDe(coordinados[indice + 1], menciones)).OrderBy(token => token.Inicio)] }
                : coordinados[indice];
        }

        return coordinados;
    }

    private static IEnumerable<TokenTexto> PropiosDe(TramoDePersona siguiente, IReadOnlyList<MencionDeAutorizado> menciones) =>
        siguiente.Tokens.Where(token => !EnMencion(token, siguiente.PersonaId, menciones));

    private static bool ComparteElPredicado(TramoDePersona tramo, IReadOnlyList<MencionDeAutorizado> menciones)
    {
        var primera = menciones.First(mencion => mencion.Id == tramo.PersonaId).Inicio;
        return tramo.Tokens
            .Where(token => token.Inicio >= primera && !EnMencion(token, tramo.PersonaId, menciones))
            .All(token => PalabrasDelSujetoCoordinado.Contains(token.Valor));
    }

    private static bool EnMencion(TokenTexto token, string personaId, IReadOnlyList<MencionDeAutorizado> menciones) =>
        menciones.Any(mencion => mencion.Id == personaId && token.Inicio >= mencion.Inicio && token.Inicio < mencion.Fin);
}
