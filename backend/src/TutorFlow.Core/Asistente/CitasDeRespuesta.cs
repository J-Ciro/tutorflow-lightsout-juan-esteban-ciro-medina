using System.Text.RegularExpressions;
using TutorFlow.Core.Autorizacion;
using TutorFlow.Core.Conocimiento;

namespace TutorFlow.Core.Asistente;

internal sealed partial class CitasDeRespuesta
{
    private static readonly IReadOnlyList<TemaDeSeccion> Temas =
    [
        new(new Cita(ReglasAplicadas.DocumentoEvaluacion, "Sección 2"), ["peso", "pesos", "30%", "40%"]),
        new(new Cita(ReglasAplicadas.DocumentoEvaluacion, "Sección 3"), ["redondea", "redondean", "redondear", "redondeo", "redondeada", "redondeadas", "cifra decimal"])
    ];

    private readonly int _apoyoMinimo;

    public CitasDeRespuesta(int apoyoMinimo)
    {
        _apoyoMinimo = apoyoMinimo;
    }

    public IReadOnlyList<Cita> EscritasConApoyo(string texto, IReadOnlyList<SeccionDocumento> entregadas, IReadOnlyCollection<Cita> reglasDeDatos)
    {
        var apoyadas = SeccionesConApoyo(texto, entregadas).Select(apoyada => CitaDe(apoyada.Seccion)).ToHashSet();
        return [.. ExtractorCitas.De(texto).Where(cita => apoyadas.Contains(cita) || reglasDeDatos.Contains(cita))];
    }

    public IReadOnlyList<Cita> Respaldo(string texto, IReadOnlyList<SeccionDocumento> entregadas) =>
        [.. SeccionesConApoyo(texto, entregadas).OrderByDescending(apoyada => apoyada.Raices).Take(1).Select(apoyada => CitaDe(apoyada.Seccion))];

    public IReadOnlyList<Cita> PorTema(string texto, IReadOnlyList<SeccionDocumento> entregadas)
    {
        var conApoyo = SeccionesConApoyo(texto, entregadas).Select(apoyada => CitaDe(apoyada.Seccion)).ToHashSet();
        var plegado = EspacioAntesDelPorcentaje().Replace(TokenizadorTexto.Plegado(texto), "%");
        return [.. Temas.Where(tema => conApoyo.Contains(tema.Cita) && tema.Terminos.Any(termino => UsaElTermino(plegado, termino))).Select(tema => tema.Cita)];
    }

    private static bool UsaElTermino(string plegado, string termino) =>
        Regex.IsMatch(plegado, $@"(?<![\p{{L}}\p{{N}}.]){Regex.Escape(termino)}(?![\p{{L}}\p{{N}}])", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(TiempoLimiteRegex.Milisegundos));

    private List<SeccionConApoyo> SeccionesConApoyo(string texto, IReadOnlyList<SeccionDocumento> entregadas)
    {
        var raicesDeLaRespuesta = RaicesDeContenido.DeApoyo(ExtractorCitas.SinCitas(texto)).ToHashSet(StringComparer.Ordinal);
        return [.. entregadas
            .Select(seccion => new SeccionConApoyo(seccion, RaicesDeContenido.DeApoyo($"{seccion.Titulo} {seccion.Texto}").Count(raicesDeLaRespuesta.Contains)))
            .Where(apoyada => apoyada.Raices >= _apoyoMinimo)];
    }

    private static Cita CitaDe(SeccionDocumento seccion) => new(seccion.Documento, seccion.Seccion);

    [GeneratedRegex(@"\s+%", RegexOptions.CultureInvariant, TiempoLimiteRegex.Milisegundos)]
    private static partial Regex EspacioAntesDelPorcentaje();

    private sealed record TemaDeSeccion(Cita Cita, string[] Terminos);

    private sealed record SeccionConApoyo(SeccionDocumento Seccion, int Raices);
}
