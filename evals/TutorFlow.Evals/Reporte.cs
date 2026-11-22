using System.Text.Json;

namespace TutorFlow.Evals;

public sealed class Reporte
{
    private const double Percentil = 0.95;

    private readonly IReadOnlyList<ResultadoCaso> _resultados;
    private readonly OpcionesEvals _opciones;

    public Reporte(IReadOnlyList<ResultadoCaso> resultados, OpcionesEvals opciones)
    {
        _resultados = resultados;
        _opciones = opciones;
    }

    public int Aprobados => _resultados.Count(r => r.Aprobado);

    public int Fallidos => _resultados.Count(r => !r.Omitido && !r.Aprobado);

    public int Omitidos => _resultados.Count(r => r.Omitido);

    public double P95FiltroPrevioMs => Percentil95(_resultados.Where(r => r.Caso.FiltroPrevio && r.Caso.Http == 403 && r.Ejecucion is not null).Select(r => r.Ejecucion!.Milisegundos).ToList());

    public bool Exitoso => Fallidos == 0 && Aprobados > 0 && P95FiltroPrevioMs < _opciones.UmbralP95Ms;

    public void Imprimir(TextWriter salida)
    {
        foreach (var fallo in _resultados.Where(r => r.Fallos.Count > 0))
        {
            salida.WriteLine($"FALLA corrida {fallo.Corrida} {fallo.Caso.Etiqueta}");
            foreach (var motivo in fallo.Fallos)
            {
                salida.WriteLine($"    - {motivo}");
            }
        }

        salida.WriteLine($"Modo: {(_opciones.Offline ? "offline (LLM doble)" : _opciones.Url)}  Corridas: {_opciones.Corridas}");
        salida.WriteLine($"Aprobados: {Aprobados}  Fallidos: {Fallidos}  Omitidos: {Omitidos}");
        salida.WriteLine($"Tokens medios: {TokensMedios():F1}  Costo medio USD: {CostoMedio():F6}");
        salida.WriteLine($"p95 de las denegaciones del filtro previo: {P95FiltroPrevioMs:F1} ms (umbral {_opciones.UmbralP95Ms} ms)");
        salida.WriteLine(Exitoso ? "RESULTADO: OK" : "RESULTADO: FALLO");
    }

    private double TokensMedios() => Respuestas().Select(s => (double)s.Tokens).DefaultIfEmpty(0).Average();

    private double CostoMedio() => Respuestas().Select(s => (double)s.CostoUsd).DefaultIfEmpty(0).Average();

    private IEnumerable<SalidaEval> Respuestas() =>
        _resultados
            .Where(r => r.Ejecucion is { Estado: 200 })
            .Select(r => Json.LeerSalida(r.Ejecucion!.Cuerpo))
            .Where(s => s is { Tokens: > 0 })
            .Select(s => s!);


    private static double Percentil95(List<double> muestras)
    {
        if (muestras.Count == 0)
        {
            return 0;
        }

        var ordenadas = muestras.Order().ToList();
        var indice = (int)Math.Ceiling(Percentil * ordenadas.Count) - 1;
        return ordenadas[Math.Max(indice, 0)];
    }

}
