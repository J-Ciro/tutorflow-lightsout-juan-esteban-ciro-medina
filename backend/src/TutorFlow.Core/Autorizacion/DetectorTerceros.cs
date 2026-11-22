using System.Text.RegularExpressions;

namespace TutorFlow.Core.Autorizacion;

internal readonly record struct MencionPersona(string Clave, int Inicio, int Fin);

public static partial class DetectorTerceros
{
    private const string MarcadorTercero = "[TERCERO]";
    private const int MinimoTokensNombreCompleto = 2;
    private static readonly string[] PrefijosId = ["est", "coord"];

    public static bool MencionaTercero(Usuario usuario, string textoNormalizado, DirectorioAcademico directorio)
    {
        var ambito = AmbitoPersonal.De(usuario, directorio);
        var tokens = TokenizadorTexto.Tokenizar(textoNormalizado);
        return MencionesDeId(tokens, directorio).Any(mencion => !ambito.ClavesDeId.Contains(mencion.Clave))
            || MencionesDeNombre(tokens, directorio).Any(mencion => !ambito.ClavesDeNombre.Contains(mencion.Clave));
    }

    public static bool TieneFormaDeIdPersona(string textoNormalizado, DirectorioAcademico directorio) =>
        MencionesDeId(TokenizadorTexto.Tokenizar(textoNormalizado), directorio).Count > 0;

    public static IReadOnlyList<string> PersonasMencionadas(string textoNormalizado, DirectorioAcademico directorio)
    {
        var tokens = TokenizadorTexto.Tokenizar(textoNormalizado);
        var idsPorClave = PersonasDelRegistro(directorio)
            .ToDictionary(persona => AmbitoPersonal.Clave(persona.Id), persona => persona.Id);
        return MencionesDeId(tokens, directorio)
            .Concat(MencionesDeNombre(tokens, directorio))
            .Where(mencion => idsPorClave.ContainsKey(mencion.Clave))
            .Select(mencion => idsPorClave[mencion.Clave])
            .Distinct()
            .ToList();
    }

    public static string EnmascararTerceros(Usuario usuario, string pregunta, DirectorioAcademico directorio)
    {
        var ambito = AmbitoPersonal.De(usuario, directorio);
        var tokens = TokenizadorTexto.Tokenizar(pregunta);
        var porId = MencionesDeId(tokens, directorio).Where(mencion => !ambito.ClavesDeId.Contains(mencion.Clave));
        var porNombre = MencionesDeNombre(tokens, directorio).Where(mencion => !ambito.ClavesDeNombre.Contains(mencion.Clave));
        var terceros = porId.Concat(porNombre).OrderByDescending(mencion => mencion.Inicio).ToList();
        return Reemplazar(pregunta, terceros);
    }

    private static string Reemplazar(string texto, List<MencionPersona> mencionesDeFinAInicio)
    {
        var limite = texto.Length;
        foreach (var mencion in mencionesDeFinAInicio.Where(mencion => mencion.Fin <= limite))
        {
            texto = string.Concat(texto.AsSpan(0, mencion.Inicio), MarcadorTercero, texto.AsSpan(mencion.Fin));
            limite = mencion.Inicio;
        }

        return texto;
    }

    private static List<MencionPersona> MencionesDeId(IReadOnlyList<TokenTexto> tokens, DirectorioAcademico directorio)
    {
        var codigos = directorio.CodigosAsignatura.Select(AmbitoPersonal.Clave).ToHashSet();
        return Enumerable.Range(0, tokens.Count)
            .Select(indice => MencionDeIdEn(tokens, indice))
            .OfType<MencionPersona>()
            .Where(mencion => !codigos.Contains(mencion.Clave))
            .ToList();
    }

    private static MencionPersona? MencionDeIdEn(IReadOnlyList<TokenTexto> tokens, int indice)
    {
        var actual = tokens[indice];
        if (IdPegado().IsMatch(actual.Valor))
        {
            return new MencionPersona(actual.Valor, actual.Inicio, actual.Fin);
        }

        var hayDigitos = indice + 1 < tokens.Count && SoloDigitos().IsMatch(tokens[indice + 1].Valor);
        return hayDigitos && PrefijosId.Contains(actual.Valor)
            ? new MencionPersona(actual.Valor + tokens[indice + 1].Valor, actual.Inicio, tokens[indice + 1].Fin)
            : null;
    }

    private static List<MencionPersona> MencionesDeNombre(IReadOnlyList<TokenTexto> tokens, DirectorioAcademico directorio) =>
        PersonasDelRegistro(directorio)
            .SelectMany(persona => MencionesDe(tokens, persona))
            .ToList();

    private static IEnumerable<MencionPersona> MencionesDe(IReadOnlyList<TokenTexto> tokens, (string Id, string Nombre) persona)
    {
        var nombre = TokenizadorTexto.Valores(persona.Nombre);
        if (nombre.Count < MinimoTokensNombreCompleto)
        {
            return [];
        }

        IReadOnlyList<string>[] ordenes = [nombre, [.. nombre.Reverse()]];
        return ordenes
            .Select(orden => (Orden: orden, Indice: TokenizadorTexto.BuscarSecuencia(tokens, orden)))
            .Where(encontrado => encontrado.Indice >= 0)
            .Select(encontrado => new MencionPersona(
                AmbitoPersonal.Clave(persona.Id),
                tokens[encontrado.Indice].Inicio,
                tokens[encontrado.Indice + encontrado.Orden.Count - 1].Fin));
    }

    private static IEnumerable<(string Id, string Nombre)> PersonasDelRegistro(DirectorioAcademico directorio) =>
        directorio.Estudiantes.Select(estudiante => (estudiante.Id, estudiante.Nombre))
            .Concat(directorio.Coordinadores.Select(coordinador => (coordinador.Id, coordinador.Nombre)));

    [GeneratedRegex(@"^(est|coord)[0-9]+\z")]
    private static partial Regex IdPegado();

    [GeneratedRegex(@"^[0-9]+\z")]
    private static partial Regex SoloDigitos();
}
