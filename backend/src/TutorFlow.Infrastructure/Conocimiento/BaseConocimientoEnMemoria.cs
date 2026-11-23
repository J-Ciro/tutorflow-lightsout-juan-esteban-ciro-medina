using TutorFlow.Core.Asistente;
using TutorFlow.Core.Autorizacion;
using TutorFlow.Core.Conocimiento;

namespace TutorFlow.Infrastructure.Conocimiento;

public sealed class BaseConocimientoEnMemoria : IBaseConocimiento
{
    private const int LongitudMaximaDeRaiz = ConsultaDeRecuperacion.LongitudDeRaiz;
    private const int LongitudMinimaDeTermino = 3;
    private const double ParametroK1 = 1.5;
    private const double ParametroB = 0.75;
    private const double PesoDelTitulo = 4.0;

    private readonly List<SeccionIndexada> _secciones;
    private readonly Dictionary<string, int> _frecuenciaDeDocumento;
    private readonly double _longitudPromedio;

    public BaseConocimientoEnMemoria(IEnumerable<SeccionDocumento> secciones)
    {
        _secciones = secciones.Select(seccion => new SeccionIndexada(seccion, ExtraerRaices(seccion.Titulo + " " + seccion.Texto))).ToList();
        _frecuenciaDeDocumento = _secciones
            .SelectMany(indexada => indexada.Frecuencias.Keys)
            .GroupBy(raiz => raiz)
            .ToDictionary(grupo => grupo.Key, grupo => grupo.Count());
        _longitudPromedio = _secciones.Count == 0 ? 1 : _secciones.Average(indexada => (double)indexada.Longitud);
    }

    public IReadOnlyList<SeccionDocumento> BuscarSecciones(string consulta, int maximo)
    {
        var terminos = ExpansionDeTerminos.Expandir(ExtraerRaices(consulta).Frecuencias.Keys);
        return _secciones
            .Select(indexada => (indexada.Seccion, Puntaje: Puntuar(indexada, terminos)))
            .Where(par => par.Puntaje > 0)
            .OrderByDescending(par => par.Puntaje)
            .Take(maximo)
            .Select(par => par.Seccion)
            .ToList();
    }

    public bool ExisteCita(Cita cita) =>
        _secciones.Any(indexada => indexada.Seccion.Documento == cita.Documento && indexada.Seccion.Seccion == cita.Seccion);

    private static Raices ExtraerRaices(string texto)
    {
        var frecuencias = NormalizadorTexto.Normalizar(texto)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(palabra => palabra.Length >= LongitudMinimaDeTermino && !PalabrasVacias.Contiene(palabra))
            .Select(palabra => palabra.Length > LongitudMaximaDeRaiz ? palabra[..LongitudMaximaDeRaiz] : palabra)
            .GroupBy(raiz => raiz)
            .ToDictionary(grupo => grupo.Key, grupo => grupo.Count());
        return new Raices(frecuencias);
    }

    private double Puntuar(SeccionIndexada indexada, IReadOnlyList<string> terminos)
    {
        var puntaje = terminos.Sum(termino => PuntajeDelTermino(indexada, termino));
        return puntaje + PuntajeDeTitulo(indexada.Seccion, terminos);
    }

    private double PuntajeDelTermino(SeccionIndexada indexada, string termino)
    {
        if (!indexada.Frecuencias.TryGetValue(termino, out var frecuencia))
        {
            return 0;
        }

        var documentos = _frecuenciaDeDocumento[termino];
        var idf = Math.Log(1 + (_secciones.Count - documentos + 0.5) / (documentos + 0.5));
        var normalizacion = 1 - ParametroB + ParametroB * indexada.Longitud / _longitudPromedio;
        return idf * frecuencia * (ParametroK1 + 1) / (frecuencia + ParametroK1 * normalizacion);
    }

    private static double PuntajeDeTitulo(SeccionDocumento seccion, IReadOnlyList<string> terminos)
    {
        var titulo = ExtraerRaices(seccion.Titulo).Frecuencias.Keys;
        var coincidencias = terminos.Count(titulo.Contains);
        return coincidencias * PesoDelTitulo;
    }

    private sealed record Raices(IReadOnlyDictionary<string, int> Frecuencias);

    private sealed record SeccionIndexada(SeccionDocumento Seccion, Raices Raices)
    {
        public IReadOnlyDictionary<string, int> Frecuencias => Raices.Frecuencias;

        public int Longitud => Raices.Frecuencias.Values.Sum();
    }
}
