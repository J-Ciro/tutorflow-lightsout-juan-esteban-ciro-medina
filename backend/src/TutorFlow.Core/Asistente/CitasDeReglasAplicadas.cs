using System.Globalization;
using System.Text.RegularExpressions;
using TutorFlow.Core.Academico;
using TutorFlow.Core.Autorizacion;
using TutorFlow.Core.Conocimiento;

namespace TutorFlow.Core.Asistente;

internal sealed partial class CitasDeReglasAplicadas
{
    private const int MaximoDeCitas = 3;
    private const string Reglamento = ReglasAplicadas.DocumentoReglamento;

    private static readonly Cita Definitiva = new(ReglasAplicadas.DocumentoEvaluacion, Textos.SeccionDefinitiva);
    private static readonly Cita Promedio = new(ReglasAplicadas.DocumentoEvaluacion, Textos.SeccionPromedio);

    private static readonly Dictionary<Cita, string[]> Terminos = new()
    {
        [Definitiva] = ["definitiva", "definitivas"],
        [Promedio] = ["promedio", "promedios"],
        [new Cita(Reglamento, Textos.SeccionPerdidaPorInasistencia)] = ["inasistencia", "inasistencias", "faltas"],
        [new Cita(ReglasAplicadas.DocumentoBienestar, Textos.SeccionBecaPorRendimiento)] = ["beca", "becas"],
        [new Cita(Reglamento, Textos.SeccionNotaMinima)] =
        [
            "aprobada", "aprobadas", "aprobado", "aprobados", "aprobar", "aprueba", "apruebas", "aprobo", "aprobaste", "perdida", "perdidas",
            "perdido", "perdidos", "perdio", "perdiste", "perdieron", "reprobada", "reprobadas", "reprobo", "reprobaron"
        ],
        [new Cita(Reglamento, Textos.SeccionHabilitacion)] = ["habilitacion", "habilitar", "habilitas", "habilitable", "habilitables"],
        [new Cita(Reglamento, Textos.SeccionNoHabilitables)] = ["no habilitable", "no habilitables", "no es habilitable", "no son habilitables"],
        [new Cita(Reglamento, Textos.SeccionPruebaAcademica)] = ["prueba academica"],
        [new Cita(Reglamento, Textos.SeccionAsistenciaSinRegistro)] = ["sin registro", "revision"]
    };

    private readonly IBaseConocimiento _baseConocimiento;
    private readonly DefinitivaPorAsignatura _definitivas;

    public CitasDeReglasAplicadas(IBaseConocimiento baseConocimiento, DefinitivaPorAsignatura definitivas)
    {
        _baseConocimiento = baseConocimiento;
        _definitivas = definitivas;
    }

    public IReadOnlyList<Cita> De(
        RegistroConsultas registro,
        string pregunta,
        string respuesta,
        IReadOnlyCollection<Cita> escritas,
        DirectorioAcademico directorio)
    {
        var texto = $" {NormalizadorTexto.Normalizar(respuesta)} ";
        var usadas = registro.Reglas.Where(regla => UsaElCampo(regla, texto, respuesta, registro, directorio)).ToList();
        IEnumerable<Cita> elegidas = usadas.Count > 0
            ? Ordenar(usadas, RaicesDeContenido.De(pregunta).ToList()).Take(MaximoDeCitas)
            : SinCampo(registro.Reglas, pregunta);
        return [.. elegidas.Where(regla => !escritas.Contains(regla))];
    }

    private IEnumerable<Cita> SinCampo(IReadOnlyList<Cita> bloque, string pregunta)
    {
        var raices = RaicesDeContenido.De(pregunta).ToList();
        return raices.Count == 0
            ? bloque.Take(MaximoDeCitas)
            : Ordenar(bloque, raices).Where(regla => Relevancia(regla, raices) > 0).Take(MaximoDeCitas);
    }

    private List<Cita> Ordenar(IReadOnlyList<Cita> reglas, List<string> raices) =>
        raices.Count == 0 ? [.. reglas] : [.. reglas.OrderByDescending(regla => Relevancia(regla, raices))];

    private int Relevancia(Cita regla, List<string> raices)
    {
        var seccion = _baseConocimiento.ObtenerSeccion(regla);
        var delaSeccion = seccion is null ? [] : RaicesDeContenido.De($"{seccion.Titulo} {seccion.Texto}").ToHashSet(StringComparer.Ordinal);
        return raices.Count(delaSeccion.Contains);
    }

    private bool UsaElCampo(Cita regla, string texto, string respuesta, RegistroConsultas registro, DirectorioAcademico directorio) =>
        UsaUnTermino(regla, texto) || UsaElValor(regla, respuesta, registro, directorio);

    private static bool UsaUnTermino(Cita regla, string texto) =>
        Terminos.TryGetValue(regla, out var palabras) && palabras.Any(palabra => texto.Contains($" {palabra} ", StringComparison.Ordinal));

    private bool UsaElValor(Cita regla, string respuesta, RegistroConsultas registro, DirectorioAcademico directorio) =>
        (regla == Definitiva && ParPorValor(respuesta, registro, directorio)) || (regla == Promedio && PromedioPorValor(respuesta, registro));

    private bool ParPorValor(string respuesta, RegistroConsultas registro, DirectorioAcademico directorio) =>
        registro.ReglasIncluyenNotas
        && _definitivas.ParesDe(respuesta, directorio).Any(par => registro.Resumenes.Values.Any(resumen => TieneDefinitiva(resumen, par)));

    private static bool TieneDefinitiva(ResumenEstudiante resumen, (string Clave, string Cifra) par) =>
        resumen.Asignaturas.Any(resultado =>
            AmbitoPersonal.Clave(resultado.Asignatura.Codigo) == par.Clave
            && DefinitivaDe(resultado) == decimal.Parse(par.Cifra, CultureInfo.InvariantCulture));

    private static decimal? DefinitivaDe(ResultadoAsignatura resultado) =>
        resultado.Estado == EstadoAsignatura.PerdidaPorInasistencia ? 0m : resultado.Definitiva;

    private static bool PromedioPorValor(string respuesta, RegistroConsultas registro)
    {
        var promedios = registro.Resumenes.Values.Where(resumen => resumen.Promedio is not null)
            .Select(resumen => resumen.Promedio!.Value.ToString("0.00", CultureInfo.InvariantCulture))
            .ToHashSet(StringComparer.Ordinal);
        return CifraConDosDecimales().Matches(TokenizadorTexto.Plegado(respuesta)).Any(cifra => promedios.Contains(cifra.Value));
    }

    [GeneratedRegex(@"(?<![\d.])\d\.\d{2}(?!\d)", RegexOptions.CultureInvariant, TiempoLimiteRegex.Milisegundos)]
    private static partial Regex CifraConDosDecimales();
}
