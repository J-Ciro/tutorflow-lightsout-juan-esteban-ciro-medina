using System.Globalization;
using System.Text.RegularExpressions;
using TutorFlow.Core.Academico;
using TutorFlow.Core.Autorizacion;

namespace TutorFlow.Core.Asistente;

internal sealed partial class DefinitivaPorAsignatura
{
    private const int PalabrasHastaLaCifra = 8;
    private const int PalabrasDelRequisito = 4;
    private const string Definitiva = "definitiva";
    private const string Definitivas = "definitivas";
    private const string NotaFinal = "final";

    private static readonly HashSet<string> Comparadores = new(StringComparer.Ordinal)
    {
        "menor", "mayor", "inferior", "superior", "minimo", "minima", "maximo", "maxima", "supera", "superar", "entre",
        "<", ">", "<=", ">="
    };

    private static readonly HashSet<(string, string)> ComparadoresDeDosPalabras = new()
    {
        ("o", "igual"), ("al", "menos"), ("menos", "de"), ("mas", "de")
    };

    private static readonly HashSet<string> PalabrasDeEnlace = new(StringComparer.Ordinal)
    {
        "definitiva", "definitivas", "nota", "final", "calificacion", "con", "de", "en", "cuya", "es"
    };

    private static readonly HashSet<string> Enlaces =new(StringComparer.Ordinal) { "a", "de", "que" };

    private static readonly HashSet<string> Determinantes = new(StringComparer.Ordinal) { "una", "la" };

    private static readonly HashSet<string> Negaciones = new(StringComparer.Ordinal) { "no", "ni", "sin" };

    private static readonly HashSet<string>[] SaltosPrevios = [new(StringComparer.Ordinal) { "nota", "calificacion" }, Determinantes, FormasDePerdida.Auxiliares];

    private readonly IReadOnlyList<AsignaturaCatalogo> _catalogo;

    public DefinitivaPorAsignatura(IReadOnlyList<AsignaturaCatalogo> catalogo)
    {
        _catalogo = catalogo;
    }

    public string? Motivo(
        IReadOnlyList<BloqueDeTexto> bloques,
        Usuario usuario,
        IReadOnlyCollection<string> idsAutorizados,
        DirectorioAcademico directorio,
        IReadOnlyDictionary<string, ResumenEstudiante> resumenesLeidos)
    {
        var catalogo = CatalogoAsignaturas.Vigente(_catalogo, directorio);
        return bloques
            .SelectMany(bloque => Atribuciones(bloque, catalogo).Select(atribucion => (Bloque: bloque, Atribucion: atribucion)))
            .Select(par => MotivoDe(par.Atribucion, TramosDePersonas.De(par.Bloque, usuario, idsAutorizados, directorio), usuario, directorio, resumenesLeidos))
            .FirstOrDefault(motivo => motivo is not null);
    }

    public IEnumerable<(string Clave, string Cifra)> ParesDe(string respuesta, DirectorioAcademico directorio)
    {
        var catalogo = CatalogoAsignaturas.Vigente(_catalogo, directorio);
        return BloquesDeRespuesta.De(respuesta)
            .SelectMany(bloque => Atribuciones(bloque, catalogo))
            .Select(atribucion => (atribucion.Clave, atribucion.Cifra));
    }

    public bool SinCifrasNiAsignaturas(string respuesta, DirectorioAcademico directorio)
    {
        var catalogo = CatalogoAsignaturas.Vigente(_catalogo, directorio);
        return BloquesDeRespuesta.De(respuesta).All(bloque => SinCifrasNiAsignaturas(bloque, catalogo));
    }

    private static bool SinCifrasNiAsignaturas(BloqueDeTexto bloque, IReadOnlyList<AsignaturaCatalogo> catalogo)
    {
        var tokens = TokenizadorTexto.TokenizarConCifras(bloque.Texto);
        return CatalogoAsignaturas.Apariciones(tokens, catalogo).Count == 0
            && !Enumerable.Range(0, tokens.Count).Any(posicion => EsCifraDeNota(tokens, posicion));
    }

    private static string? MotivoDe(
        AtribucionDeDefinitiva atribucion,
        PersonasDeBloque personas,
        Usuario usuario,
        DirectorioAcademico directorio,
        IReadOnlyDictionary<string, ResumenEstudiante> resumenesLeidos)
    {
        if (personas.Ambiguo)
        {
            return Motivo(atribucion, null, usuario, directorio);
        }

        return personas.Tramos
            .Where(tramo => tramo.Tokens.Any(token => token.Inicio == atribucion.InicioDeAsignatura))
            .Where(tramo => !Coincide(atribucion, tramo.PersonaId, resumenesLeidos))
            .Select(tramo => Motivo(atribucion, tramo.PersonaId, usuario, directorio))
            .FirstOrDefault();
    }

    private static string Motivo(AtribucionDeDefinitiva atribucion, string? personaId, Usuario usuario, DirectorioAcademico directorio)
    {
        var persona = personaId is null || usuario.Rol != Rol.Coordinador ? string.Empty : $" de {NombreDe(personaId, directorio)}";
        return $"la definitiva {atribucion.Cifra} de {atribucion.Escrita}{persona} no coincide con los datos entregados";
    }

    private static string NombreDe(string personaId, DirectorioAcademico directorio) =>
        directorio.Estudiantes.Select(estudiante => (estudiante.Id, estudiante.Nombre))
            .Concat(directorio.Coordinadores.Select(coordinador => (coordinador.Id, coordinador.Nombre)))
            .FirstOrDefault(persona => persona.Id == personaId).Nombre ?? personaId;

    private static IEnumerable<AtribucionDeDefinitiva> Atribuciones(BloqueDeTexto bloque, IReadOnlyList<AsignaturaCatalogo> catalogo)
    {
        var tokens = TokenizadorTexto.TokenizarConCifras(bloque.Texto);
        var apariciones = CatalogoAsignaturas.Apariciones(tokens, catalogo);
        var hayDefinitiva = Enumerable.Range(0, tokens.Count).Any(indice => EsDefinitiva(tokens, indice) && !EstaNegada(tokens, indice));
        var porCifra = Enumerable.Range(0, tokens.Count)
            .Where(indice => EsDefinitivaSingular(tokens, indice) && !EstaNegada(tokens, indice))
            .Select(indice => CifraEfectiva(tokens, indice))
            .OfType<int>()
            .SelectMany(cifra => AtribucionMasCercana(bloque, tokens, apariciones, cifra));
        return porCifra.Concat(Pares(bloque, tokens, apariciones, hayDefinitiva));
    }

    private static IEnumerable<AtribucionDeDefinitiva> AtribucionMasCercana(
        BloqueDeTexto bloque,
        IReadOnlyList<TokenTexto> tokens,
        IReadOnlyList<AparicionDeAsignatura> apariciones,
        int cifra)
    {
        var asignatura = AnteriorEnlazada(tokens, apariciones, cifra)
            ?? apariciones.MinBy(aparicion => (Distancia(aparicion, cifra), aparicion.Inicio > cifra));
        return asignatura is null ? [] : [Construir(bloque, tokens, asignatura, cifra, conDefinitiva: true)];
    }

    private static AparicionDeAsignatura? AnteriorEnlazada(
        IReadOnlyList<TokenTexto> tokens,
        IReadOnlyList<AparicionDeAsignatura> apariciones,
        int cifra)
    {
        var anterior = apariciones.Where(aparicion => aparicion.Fin < cifra).MaxBy(aparicion => aparicion.Fin);
        return anterior is not null && Enumerable.Range(anterior.Fin + 1, cifra - anterior.Fin - 1).All(posicion => PalabrasDeEnlace.Contains(tokens[posicion].Valor))
            ? anterior
            : null;
    }

    private static IEnumerable<AtribucionDeDefinitiva> Pares(
        BloqueDeTexto bloque,
        IReadOnlyList<TokenTexto> tokens,
        IReadOnlyList<AparicionDeAsignatura> apariciones,
        bool hayDefinitiva) =>
        apariciones
            .Where(aparicion => aparicion.Fin + 1 < tokens.Count && EsCifraDeNota(tokens, aparicion.Fin + 1) && SoloSeparadores(bloque.Texto, tokens, aparicion))
            .Where(aparicion => !EsUmbralDeCifra(tokens, aparicion.Fin + 1))
            .Select(aparicion => Construir(bloque, tokens, aparicion, aparicion.Fin + 1, hayDefinitiva));

    private static bool SoloSeparadores(string texto, IReadOnlyList<TokenTexto> tokens, AparicionDeAsignatura aparicion) =>
        SeparadoresDePar().IsMatch(texto[tokens[aparicion.Fin].Fin..tokens[aparicion.Fin + 1].Inicio]);

    private static AtribucionDeDefinitiva Construir(
        BloqueDeTexto bloque,
        IReadOnlyList<TokenTexto> tokens,
        AparicionDeAsignatura asignatura,
        int cifra,
        bool conDefinitiva) =>
        new(
            asignatura.Clave,
            tokens[cifra].Valor,
            tokens[asignatura.Inicio].Inicio,
            bloque.Texto[tokens[asignatura.Inicio].Inicio..tokens[asignatura.Fin].Fin],
            conDefinitiva);

    private static int Distancia(AparicionDeAsignatura asignatura, int cifra) =>
        asignatura.Fin < cifra ? cifra - asignatura.Fin : asignatura.Inicio - cifra;

    private static bool EsDefinitiva(IReadOnlyList<TokenTexto> tokens, int indice) =>
        tokens[indice].Valor == Definitivas || EsDefinitivaSingular(tokens, indice);

    private static bool EsDefinitivaSingular(IReadOnlyList<TokenTexto> tokens, int indice) =>
        tokens[indice].Valor == Definitiva || EsNotaFinal(tokens, indice);

    private static bool EsNotaFinal(IReadOnlyList<TokenTexto> tokens, int indice) =>
        tokens[indice].Valor == NotaFinal && indice > 0 && tokens[indice - 1].Valor is "nota" or "calificacion";

    internal static int? CifraEfectiva(IReadOnlyList<TokenTexto> tokens, int definitiva)
    {
        var cifra = CifraAsociada(tokens, definitiva);
        return cifra is { } asociada && EsUmbralDeCifra(tokens, asociada) ? SiguienteNoUmbral(tokens, asociada) : cifra;
    }

    private static int? SiguienteNoUmbral(IReadOnlyList<TokenTexto> tokens, int desde) =>
        Enumerable.Range(desde + 1, Math.Max(0, tokens.Count - desde - 1))
            .Where(posicion => EsCifraDeNota(tokens, posicion) && !EsUmbralDeCifra(tokens, posicion))
            .Select(posicion => (int?)posicion)
            .FirstOrDefault();

    private static bool EsUmbralDeCifra(IReadOnlyList<TokenTexto> tokens, int cifra) =>
        PrecedidaDeComparador(tokens, cifra) || SigueARequisito(tokens, cifra) || UmbralNormativo.Es(tokens, cifra);

    private static bool PrecedidaDeComparador(IReadOnlyList<TokenTexto> tokens, int cifra) =>
        TerminaComparador(tokens, cifra - 1) || (cifra > 0 && Enlaces.Contains(tokens[cifra - 1].Valor) && TerminaComparador(tokens, cifra - 2));

    private static bool TerminaComparador(IReadOnlyList<TokenTexto> tokens, int indice) =>
        indice >= 0
        && (Comparadores.Contains(tokens[indice].Valor)
            || (indice > 0 && ComparadoresDeDosPalabras.Contains((tokens[indice - 1].Valor, tokens[indice].Valor))));

    private static bool SigueARequisito(IReadOnlyList<TokenTexto> tokens, int cifra) =>
        RequisitoPrevio().IsMatch(string.Join(' ', tokens.Take(cifra).TakeLast(PalabrasDelRequisito).Select(token => token.Valor)));

    internal static bool EstaNegada(IReadOnlyList<TokenTexto> tokens, int indice)
    {
        var anterior = SaltosPrevios.Aggregate(indice - 1, (posicion, saltables) => Saltar(tokens, posicion, saltables));
        return anterior >= 0 && Negaciones.Contains(tokens[anterior].Valor);
    }

    private static int Saltar(IReadOnlyList<TokenTexto> tokens, int posicion, HashSet<string> saltables) =>
        posicion >= 0 && saltables.Contains(tokens[posicion].Valor) ? posicion - 1 : posicion;

    private static int? CifraAsociada(IReadOnlyList<TokenTexto> tokens, int indice)
    {
        var despues = Enumerable.Range(indice + 1, Math.Min(PalabrasHastaLaCifra, tokens.Count - indice - 1));
        var antes = Enumerable.Range(Math.Max(0, indice - PalabrasHastaLaCifra), Math.Min(PalabrasHastaLaCifra, indice)).Reverse();
        return despues.Concat(antes).Where(posicion => EsCifraDeNota(tokens, posicion)).Select(posicion => (int?)posicion).FirstOrDefault();
    }

    private static bool EsCifraDeNota(IReadOnlyList<TokenTexto> tokens, int posicion) =>
        CifraDeNota().IsMatch(tokens[posicion].Valor)
        && !(posicion > 0 && PalabrasDeReferencia.Contains(tokens[posicion - 1].Valor));

    private static bool Coincide(AtribucionDeDefinitiva atribucion, string personaId, IReadOnlyDictionary<string, ResumenEstudiante> resumenesLeidos)
    {
        if (!resumenesLeidos.TryGetValue(personaId, out var resumen))
        {
            return true;
        }

        var resultado = resumen.Asignaturas.FirstOrDefault(r => AmbitoPersonal.Clave(r.Asignatura.Codigo) == atribucion.Clave);
        return resultado is null || ValoresAceptados(resultado, atribucion.ConDefinitiva).Contains(decimal.Parse(atribucion.Cifra, CultureInfo.InvariantCulture));
    }

    private static IEnumerable<decimal> ValoresAceptados(ResultadoAsignatura resultado, bool soloDefinitiva)
    {
        var definitiva = resultado.Estado == EstadoAsignatura.PerdidaPorInasistencia ? 0m : resultado.Definitiva;
        var propios = new[] { definitiva };
        var otros = new[] { resultado.Asignatura.Cortes.Corte1, resultado.Asignatura.Cortes.Corte2, resultado.Asignatura.Cortes.Corte3, resultado.Acumulada?.Valor };
        return (soloDefinitiva ? propios : propios.Concat(otros)).OfType<decimal>();
    }

    private static readonly HashSet<string> PalabrasDeReferencia = new(StringComparer.Ordinal)
    {
        "seccion", "secciones", "articulo", "articulos", "art"
    };

    [GeneratedRegex(@"(?:^| )necesitas(?: (?:una|la))? definitiva de\z", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: TiempoLimiteRegex.Milisegundos)]
    private static partial Regex RequisitoPrevio();

    [GeneratedRegex(@"^[\s:=(]*\z", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: TiempoLimiteRegex.Milisegundos)]
    private static partial Regex SeparadoresDePar();

    [GeneratedRegex(@"^\d\.\d{1,2}\z", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: TiempoLimiteRegex.Milisegundos)]
    private static partial Regex CifraDeNota();

    private sealed record AtribucionDeDefinitiva(string Clave, string Cifra, int InicioDeAsignatura, string Escrita, bool ConDefinitiva);
}
