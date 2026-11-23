using System.Globalization;
using TutorFlow.Core.Academico;
using TutorFlow.Core.Autorizacion;

namespace TutorFlow.Core.Asistente;

public static class FormateadorDatos
{
    private const string FormatoNota = "0.0";
    private const string FormatoPromedio = "0.00";
    private const string SinCalificar = "sin calificar";
    private const string MarcaDelimitadorOriginal = "DATOS_NO_CONFIABLES";
    private const string MarcaDelimitadorNeutra = "DATOS NO CONFIABLES";
    private const string SinDefinitivaEncabezado = "Sin definitiva (falta calificar un corte)";

    private static readonly VozDeLosDatos Segunda = new("Solicita la revisión a tu coordinador", "superaste", "superas", "pierdes");
    private static readonly VozDeLosDatos Tercera = new("Solicite la revisión al coordinador del programa", "superó", "supera", "pierde");

    public static string Delimitar(string fuente, string contenido) =>
        $"<<<{MarcaDelimitadorOriginal} fuente=\"{fuente}\">>>\n"
        + contenido.Replace(MarcaDelimitadorOriginal, MarcaDelimitadorNeutra, StringComparison.Ordinal)
        + $"\nFIN_{MarcaDelimitadorOriginal}>>>";

    public static string DatosBasicos(Estudiante estudiante, DirectorioAcademico directorio)
    {
        var programa = directorio.Programas.FirstOrDefault(p => p.Id == estudiante.ProgramaId);
        var coordinadores = directorio.Coordinadores
            .Where(c => c.ProgramasIds.Contains(estudiante.ProgramaId))
            .Select(c => c.Nombre)
            .ToList();
        return $"Estudiante: {estudiante.Nombre} (ID {estudiante.Id}). "
            + $"Programa: {estudiante.ProgramaId} ({programa?.Nombre ?? estudiante.ProgramaId}). "
            + $"Coordinador del programa: {(coordinadores.Count == 0 ? "sin coordinador asignado" : string.Join(", ", coordinadores))}.";
    }

    public static string Notas(ResumenEstudiante resumen) => string.Join("\n", LineasDeNotas(resumen));

    public static string Asistencia(ResumenEstudiante resumen, bool terceraPersona = false) =>
        string.Join("\n", resumen.Asignaturas.Select(resultado => LineaDeAsistencia(resultado, terceraPersona)));

    public static string Cortes(IReadOnlyList<NotasAsignatura> notas, string consulta)
    {
        var coincidencias = BuscarAsignaturas(notas, consulta).Select(LineaDeCortes).ToList();
        return coincidencias.Count == 0 ? Textos.AsignaturaNoEncontrada : string.Join(Environment.NewLine, coincidencias);
    }

    public static IReadOnlyList<NotasAsignatura> BuscarAsignaturas(IReadOnlyList<NotasAsignatura> notas, string consulta)
    {
        var buscada = NormalizadorTexto.Normalizar(consulta);
        return [.. notas.Where(nota => NormalizadorTexto.Normalizar(nota.Nombre).Contains(buscada, StringComparison.Ordinal)
            || NormalizadorTexto.Normalizar(nota.Codigo) == buscada)];
    }

    internal static IEnumerable<string> LineasDeNotas(ResumenEstudiante resumen)
    {
        var conDefinitiva = resumen.Asignaturas.Where(resultado => resultado.Estado != EstadoAsignatura.SinDefinitiva).ToList();
        var lineas = new List<string>();
        lineas.AddRange(conDefinitiva.Count == 0 ? [] : [$"Definitivas: {string.Join(", ", conDefinitiva.Select(DefinitivaEnLinea))}"]);
        lineas.AddRange(resumen.Asignaturas.SelectMany(LineasDeEstadoDeLaAsignatura));
        lineas.AddRange(conDefinitiva.Count == 0 ? [] : [$"Promedio ponderado del periodo: {Promedio(resumen.Promedio)}"]);
        lineas.Add($"Créditos matriculados: {resumen.CreditosMatriculados}");
        lineas.Add($"Prueba académica: {(resumen.PruebaAcademica ? "sí" : "no")}");
        lineas.Add($"Beca por rendimiento: {TextoBeca(resumen.Beca)}");
        return lineas;
    }

    internal static string Promedio(decimal? valor) =>
        valor is { } promedio ? promedio.ToString(FormatoPromedio, CultureInfo.InvariantCulture) : "no disponible";

    internal static string Nota(decimal? valor) =>
        valor is { } nota ? nota.ToString(FormatoNota, CultureInfo.InvariantCulture) : SinCalificar;

    internal static string TextoBeca(ElegibilidadBeca beca) => beca switch
    {
        ElegibilidadBeca.Elegible => "elegible",
        ElegibilidadBeca.NoElegible => "no elegible",
        _ => "no determinable aún porque hay asignaturas sin definitiva"
    };

    private static string DefinitivaEnLinea(ResultadoAsignatura resultado) =>
        resultado.Estado == EstadoAsignatura.PerdidaPorInasistencia
            ? $"{resultado.Asignatura.Nombre} {Nota(0m)} (por inasistencia)"
            : $"{resultado.Asignatura.Nombre} {Nota(resultado.Definitiva)}";

    private static IEnumerable<string> LineasDeEstadoDeLaAsignatura(ResultadoAsignatura resultado) => resultado.Estado switch
    {
        EstadoAsignatura.Perdida =>
        [
            $"Perdida por nota: {resultado.Asignatura.Nombre} (definitiva {Nota(resultado.Definitiva)})",
            resultado.Habilitable ? $"Habilitable: {resultado.Asignatura.Nombre}" : $"No habilitable: {resultado.Asignatura.Nombre}"
        ],
        EstadoAsignatura.PerdidaPorInasistencia =>
        [
            $"Perdida por inasistencia: {resultado.Asignatura.Nombre} (definitiva {Nota(0m)})",
            $"No habilitable: {resultado.Asignatura.Nombre}"
        ],
        EstadoAsignatura.SinDefinitiva => [LineaSinDefinitiva(resultado), .. LineaDeEquivalente(resultado)],
        _ => []
    };

    private static IEnumerable<string> LineaDeEquivalente(ResultadoAsignatura resultado) =>
        resultado.Acumulada is { } acumulada
            ? [$"Equivalente de la acumulada de {resultado.Asignatura.Nombre} sobre la nota completa: {Nota(acumulada.EquivalenteSobreEvaluado)}"]
            : [];

    private static string LineaSinDefinitiva(ResultadoAsignatura resultado) =>
        resultado.Acumulada is { } acumulada
            ? $"{SinDefinitivaEncabezado}: {resultado.Asignatura.Nombre} (acumulada {Nota(acumulada.Valor)} sobre el {acumulada.PorcentajeEvaluado.ToString("0", CultureInfo.InvariantCulture)}% evaluado)"
            : $"{SinDefinitivaEncabezado}: {resultado.Asignatura.Nombre}";

    private static string LineaDeCortes(NotasAsignatura nota) =>
        $"{nota.Nombre} ({nota.Codigo}): corte 1 {Nota(nota.Cortes.Corte1)} (30%), corte 2 {Nota(nota.Cortes.Corte2)} (30%), corte 3 {Nota(nota.Cortes.Corte3)} (40%).";


    private static string LineaDeAsistencia(ResultadoAsignatura resultado, bool terceraPersona)
    {
        var voz = terceraPersona ? Tercera : Segunda;
        var nombre = resultado.Asignatura.Nombre;
        var clases = resultado.Asignatura.Asistencia.ClasesProgramadas;
        var maximo = resultado.FaltasPermitidas;
        if (resultado.Asignatura.Asistencia.Inasistencias is not { } faltas)
        {
            return $"{nombre}: sin registro de inasistencias en {clases} clases programadas (máximo sin perder: {maximo}). {voz.Revision} ({Textos.SeccionAsistenciaSinRegistro}).";
        }

        var restantes = resultado.Estado == EstadoAsignatura.PerdidaPorInasistencia ? 0 : Math.Max(maximo - faltas, 0);
        var consecuencia = resultado.Estado == EstadoAsignatura.PerdidaPorInasistencia
            ? $"Perdida por inasistencia: {voz.Superaste} {maximo} inasistencias."
            : $"Si {voz.Supera} {maximo} inasistencias {voz.Pierde} la asignatura por inasistencia.";
        return $"{nombre}: faltas {faltas} de {clases} clases programadas (faltas restantes: {restantes}; máximo sin perder: {maximo}). {consecuencia}";
    }

    private sealed record VozDeLosDatos(string Revision, string Superaste, string Supera, string Pierde);
}
