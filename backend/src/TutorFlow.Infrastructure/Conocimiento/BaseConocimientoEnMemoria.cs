using TutorFlow.Core.Asistente;
using TutorFlow.Core.Autorizacion;
using TutorFlow.Core.Conocimiento;

namespace TutorFlow.Infrastructure.Conocimiento;

public sealed record UmbralesDeEntrega(int PuntajeRelativoMinimo = 55, int PuntajeRelativoRelevante = 40, int RelevanciaMinima = 2);

public sealed class BaseConocimientoEnMemoria : IBaseConocimiento
{
    private const int LongitudMaximaDeRaiz = ConsultaDeRecuperacion.LongitudDeRaiz;
    private const int LongitudMinimaDeTermino = 3;
    private const double ParametroK1 = 1.5;
    private const double ParametroB = 0.75;
    private const double PesoDelTitulo = 4.0;

    private const int MinimoDeRaicesDeLaRegla = 2;

    private readonly UmbralesDeEntrega _umbrales;
    private readonly List<SeccionIndexada> _secciones;
    private readonly Dictionary<string, int> _frecuenciaDeDocumento;
    private readonly double _longitudPromedio;

    public BaseConocimientoEnMemoria(IEnumerable<SeccionDocumento> secciones, UmbralesDeEntrega? umbrales = null)
    {
        _umbrales = umbrales ?? new UmbralesDeEntrega();
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
        var recuperadas = _secciones
            .Select(indexada => new Recuperada(indexada.Seccion, Puntuar(indexada, terminos)))
            .Where(recuperada => recuperada.Puntaje > 0)
            .OrderByDescending(recuperada => recuperada.Puntaje)
            .Take(maximo)
            .ToList();
        return Entregadas(recuperadas, RaicesDeContenido.De(consulta).ToList());
    }

    private IReadOnlyList<SeccionDocumento> Entregadas(List<Recuperada> recuperadas, List<string> raicesDeLaPregunta)
    {
        if (recuperadas.Count == 0)
        {
            return [];
        }

        var puntajeMaximo = recuperadas[0].Puntaje;
        var coincidencias = recuperadas.Select(recuperada => CoincidenciasDe(recuperada.Seccion, raicesDeLaPregunta)).ToList();
        var minimoRelevante = Math.Min(_umbrales.RelevanciaMinima, (raicesDeLaPregunta.Count + 1) / 2);
        var relevantes = Enumerable.Range(0, recuperadas.Count).Where(i => raicesDeLaPregunta.Count > 0 && coincidencias[i] >= minimoRelevante).ToList();
        var entregadas = new HashSet<int> { 0 };
        entregadas.UnionWith(relevantes.Take(1));
        entregadas.UnionWith(Enumerable.Range(0, recuperadas.Count).Where(i => Porcentaje(recuperadas[i], puntajeMaximo) >= _umbrales.PuntajeRelativoMinimo));
        entregadas.UnionWith(relevantes.Where(i => coincidencias[i] >= MinimoDeRaicesDeLaRegla && Porcentaje(recuperadas[i], puntajeMaximo) >= _umbrales.PuntajeRelativoRelevante));
        return [.. entregadas.Order().Select(indice => recuperadas[indice].Seccion)];
    }

    private static double Porcentaje(Recuperada recuperada, double puntajeMaximo) => recuperada.Puntaje / puntajeMaximo * 100.0;

    private static int CoincidenciasDe(SeccionDocumento seccion, List<string> raicesDeLaPregunta)
    {
        var delaSeccion = RaicesDeContenido.De($"{seccion.Titulo} {seccion.Texto}").ToHashSet(StringComparer.Ordinal);
        return raicesDeLaPregunta.Count(delaSeccion.Contains);
    }

    private sealed record Recuperada(SeccionDocumento Seccion, double Puntaje);

    public SeccionDocumento? ObtenerSeccion(Cita cita) =>
        _secciones.Select(indexada => indexada.Seccion).FirstOrDefault(seccion => seccion.Documento == cita.Documento && seccion.Seccion == cita.Seccion);

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
