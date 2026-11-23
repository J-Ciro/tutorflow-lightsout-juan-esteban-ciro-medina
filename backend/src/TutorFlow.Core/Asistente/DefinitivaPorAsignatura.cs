using System.Globalization;
using System.Text.RegularExpressions;
using TutorFlow.Core.Academico;
using TutorFlow.Core.Autorizacion;

namespace TutorFlow.Core.Asistente;

internal sealed partial class DefinitivaPorAsignatura
{
    private const int PalabrasDeNegacion = 3;
    private const int PalabrasHastaLaCifra = 8;
    private const string Definitiva = "definitiva";

    private static readonly HashSet<string> Negaciones = new(StringComparer.Ordinal) { "no", "ni", "sin", "aun" };

    private static readonly HashSet<string> PalabrasDeReferencia = new(StringComparer.Ordinal)
    {
        "seccion", "secciones", "articulo", "articulos", "art"
    };

    private readonly IReadOnlyList<AsignaturaCatalogo> _catalogo;

    public DefinitivaPorAsignatura(IReadOnlyList<AsignaturaCatalogo> catalogo)
    {
        _catalogo = catalogo;
    }

    public bool SonVerificables(
        string respuesta,
        Usuario usuario,
        IReadOnlyCollection<string> idsAutorizados,
        DirectorioAcademico directorio,
        IReadOnlyDictionary<string, ResumenEstudiante> resumenesLeidos) =>
        TramosDePersonas.DeRespuesta(respuesta, usuario.Rol, idsAutorizados, directorio)
            .SelectMany(oracion => Atribuciones(oracion, usuario))
            .All(atribucion => Coincide(atribucion, resumenesLeidos));

    private IEnumerable<AtribucionDeDefinitiva> Atribuciones(OracionAnalizada oracion, Usuario usuario)
    {
        var tokens = TokenizadorTexto.TokenizarConCifras(oracion.Texto);
        var apariciones = CatalogoAsignaturas.Apariciones(tokens, _catalogo);
        return Enumerable.Range(0, tokens.Count)
            .Where(indice => tokens[indice].Valor == Definitiva && !EstaNegada(tokens, indice))
            .Select(indice => CifraAsociada(tokens, indice))
            .OfType<int>()
            .SelectMany(cifra => Atribuir(oracion, usuario, tokens, apariciones, cifra));
    }

    private static IEnumerable<AtribucionDeDefinitiva> Atribuir(
        OracionAnalizada oracion,
        Usuario usuario,
        IReadOnlyList<TokenTexto> tokens,
        IReadOnlyList<AparicionDeAsignatura> apariciones,
        int cifra)
    {
        var asignatura = apariciones.MinBy(aparicion => (Distancia(aparicion, cifra), aparicion.Inicio > cifra));
        return asignatura is null
            ? []
            : PersonasDe(oracion, usuario, tokens[asignatura.Inicio].Inicio)
                .Select(persona => new AtribucionDeDefinitiva(persona, asignatura.Clave, tokens[cifra].Valor));
    }

    private static int Distancia(AparicionDeAsignatura asignatura, int cifra) =>
        asignatura.Fin < cifra ? cifra - asignatura.Fin : asignatura.Inicio - cifra;

    private static IEnumerable<string> PersonasDe(OracionAnalizada oracion, Usuario usuario, int posicion) =>
        usuario.Rol == Rol.Coordinador
            ? oracion.Tramos.Where(tramo => tramo.Tokens.Any(token => token.Inicio == posicion)).Select(tramo => tramo.PersonaId)
            : [usuario.Id];

    private static bool EstaNegada(IReadOnlyList<TokenTexto> tokens, int indice) =>
        Enumerable.Range(Math.Max(0, indice - PalabrasDeNegacion), Math.Min(PalabrasDeNegacion, indice))
            .Any(anterior => Negaciones.Contains(tokens[anterior].Valor));

    private static int? CifraAsociada(IReadOnlyList<TokenTexto> tokens, int indice)
    {
        var despues = Enumerable.Range(indice + 1, Math.Min(PalabrasHastaLaCifra, tokens.Count - indice - 1));
        var antes = Enumerable.Range(Math.Max(0, indice - PalabrasHastaLaCifra), Math.Min(PalabrasHastaLaCifra, indice)).Reverse();
        return despues.Concat(antes).Where(posicion => EsCifraDeNota(tokens, posicion)).Select(posicion => (int?)posicion).FirstOrDefault();
    }

    private static bool EsCifraDeNota(IReadOnlyList<TokenTexto> tokens, int posicion) =>
        CifraDeNota().IsMatch(tokens[posicion].Valor)
        && !(posicion > 0 && PalabrasDeReferencia.Contains(tokens[posicion - 1].Valor));

    private static bool Coincide(AtribucionDeDefinitiva atribucion, IReadOnlyDictionary<string, ResumenEstudiante> resumenesLeidos)
    {
        if (!resumenesLeidos.TryGetValue(atribucion.PersonaId, out var resumen))
        {
            return true;
        }

        var resultado = resumen.Asignaturas.FirstOrDefault(r => AmbitoPersonal.Clave(r.Asignatura.Codigo) == atribucion.Clave);
        return resultado is null
            || resultado.Definitiva == decimal.Parse(atribucion.Cifra, CultureInfo.InvariantCulture);
    }

    [GeneratedRegex(@"^\d\.\d{1,2}\z", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex CifraDeNota();

    private sealed record AtribucionDeDefinitiva(string PersonaId, string Clave, string Cifra);
}
