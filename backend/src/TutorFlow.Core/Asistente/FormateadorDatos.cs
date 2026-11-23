using System.Globalization;
using TutorFlow.Core.Academico;
using TutorFlow.Core.Autorizacion;

namespace TutorFlow.Core.Asistente;

public static class FormateadorDatos
{
    private const string FormatoNota = "0.0";
    private const string FormatoPromedio = "0.00";
    private const string SinCalificar = "sin calificar";
    private const string Ninguna = "ninguna";
    private const string MarcaDelimitadorOriginal = "DATOS_NO_CONFIABLES";
    private const string MarcaDelimitadorNeutra = "DATOS NO CONFIABLES";

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

    public static string Notas(ResumenEstudiante resumen)
    {
        var lineas = resumen.Asignaturas.Select(LineaDeNotas).ToList();
        lineas.Add($"Promedio ponderado del periodo: {Promedio(resumen.Promedio)}.");
        lineas.Add($"Créditos matriculados: {resumen.CreditosMatriculados}.");
        lineas.Add($"Prueba académica: {(resumen.PruebaAcademica ? "sí" : "no")}.");
        lineas.Add($"Beca por rendimiento: {TextoBeca(resumen.Beca)}.");
        return string.Join("\n", lineas);
    }

    public static string Asistencia(ResumenEstudiante resumen) =>
        string.Join("\n", resumen.Asignaturas.Select(LineaDeAsistencia));

    public static string LineaDePrograma(Estudiante estudiante, ResumenEstudiante resumen) =>
        $"{estudiante.Nombre} (ID {estudiante.Id}): promedio {Promedio(resumen.Promedio)}; "
        + $"prueba académica: {(resumen.PruebaAcademica ? "sí" : "no")}; "
        + $"asignaturas perdidas: {Lista(resumen.Asignaturas.Where(EstaPerdida).Select(DescribirPerdida))}; "
        + $"asignaturas sin definitiva: {Lista(resumen.Asignaturas.Where(SinDefinitiva).Select(a => a.Asignatura.Nombre))}; "
        + $"asistencia sin registro: {Lista(resumen.Asignaturas.Where(SinRegistro).Select(a => a.Asignatura.Nombre))}.";

    public static string EncabezadoPrograma(Programa programa, int cantidadEstudiantes) =>
        $"Programa {programa.Id} ({programa.Nombre}): {cantidadEstudiantes} estudiantes.";

    private static string LineaDeNotas(ResultadoAsignatura resultado)
    {
        var asignatura = resultado.Asignatura;
        return $"{asignatura.Nombre} ({asignatura.Codigo}, {asignatura.Creditos} créditos): {DescribirEstado(resultado)}.";
    }

    private static string DescribirEstado(ResultadoAsignatura resultado) => resultado.Estado switch
    {
        EstadoAsignatura.Aprobada => $"definitiva {Nota(resultado.Definitiva)}, aprobada",
        EstadoAsignatura.Perdida => $"definitiva {Nota(resultado.Definitiva)}, perdida, {DescribirHabilitacion(resultado)}",
        EstadoAsignatura.PerdidaPorInasistencia => "perdida por inasistencia con definitiva 0.0, no habilitable",
        _ => DescribirSinDefinitiva(resultado)
    };

    private static string DescribirHabilitacion(ResultadoAsignatura resultado) =>
        resultado.Habilitable
            ? "puede presentar examen de habilitación"
            : "no es habilitable porque la definitiva es menor a 2.0";

    private static string DescribirSinDefinitiva(ResultadoAsignatura resultado) =>
        resultado.Acumulada is { } acumulada
            ? $"sin definitiva porque falta calificar un corte (calificados: {CortesCalificados(resultado.Asignatura.Cortes)}); nota acumulada {Nota(acumulada.Valor)} sobre el {acumulada.PorcentajeEvaluado.ToString("0", CultureInfo.InvariantCulture)}% evaluado (equivale a {Nota(acumulada.EquivalenteSobreEvaluado)})"
            : "sin definitiva porque no hay cortes calificados";

    public static string Cortes(IReadOnlyList<NotasAsignatura> notas, string consulta)
    {
        var buscada = NormalizadorTexto.Normalizar(consulta);
        var coincidencias = notas
            .Where(nota => NormalizadorTexto.Normalizar(nota.Nombre).Contains(buscada, StringComparison.Ordinal)
                || NormalizadorTexto.Normalizar(nota.Codigo) == buscada)
            .Select(LineaDeCortes)
            .ToList();
        return coincidencias.Count == 0 ? "No hay una asignatura con ese nombre o código." : string.Join(Environment.NewLine, coincidencias);
    }

    private static string LineaDeCortes(NotasAsignatura nota) =>
        $"{nota.Nombre} ({nota.Codigo}): corte 1 {Nota(nota.Cortes.Corte1)} (30%), corte 2 {Nota(nota.Cortes.Corte2)} (30%), corte 3 {Nota(nota.Cortes.Corte3)} (40%).";

    private static string CortesCalificados(NotasCorte cortes)
    {
        var calificados = new[] { ("corte 1", cortes.Corte1), ("corte 2", cortes.Corte2), ("corte 3", cortes.Corte3) }
            .Where(corte => corte.Item2.HasValue)
            .Select(corte => $"{corte.Item1} {Nota(corte.Item2)}");
        return Lista(calificados);
    }

    private static string LineaDeAsistencia(ResultadoAsignatura resultado)
    {
        var asignatura = resultado.Asignatura;
        var clases = asignatura.Asistencia.ClasesProgramadas;
        return asignatura.Asistencia.Inasistencias is { } faltas
            ? $"{asignatura.Nombre}: {faltas} inasistencias de {clases} clases programadas; máximo permitido sin perder: {resultado.FaltasPermitidas}; faltas restantes: {resultado.FaltasRestantes}{(resultado.Estado == EstadoAsignatura.PerdidaPorInasistencia ? "; perdida por inasistencia" : string.Empty)}."
            : $"{asignatura.Nombre}: sin registro de inasistencias en {clases} clases programadas; no se declara pérdida y se debe solicitar la revisión al coordinador (Artículo 12).";
    }

    private static string DescribirPerdida(ResultadoAsignatura resultado) =>
        resultado.Estado == EstadoAsignatura.PerdidaPorInasistencia
            ? $"{resultado.Asignatura.Nombre} (0.0, por inasistencia)"
            : $"{resultado.Asignatura.Nombre} ({Nota(resultado.Definitiva)})";

    private static bool EstaPerdida(ResultadoAsignatura resultado) =>
        resultado.Estado is EstadoAsignatura.Perdida or EstadoAsignatura.PerdidaPorInasistencia;

    private static bool SinDefinitiva(ResultadoAsignatura resultado) =>
        resultado.Estado == EstadoAsignatura.SinDefinitiva;

    private static bool SinRegistro(ResultadoAsignatura resultado) =>
        resultado.EstadoAsistencia == EstadoAsistencia.SinRegistro;

    private static string TextoBeca(ElegibilidadBeca beca) => beca switch
    {
        ElegibilidadBeca.Elegible => "elegible",
        ElegibilidadBeca.NoElegible => "no elegible",
        _ => "no determinable aún porque hay asignaturas sin definitiva"
    };

    private static string Lista(IEnumerable<string> elementos)
    {
        var lista = elementos.ToList();
        return lista.Count == 0 ? Ninguna : string.Join(", ", lista);
    }

    private static string Nota(decimal? valor) =>
        valor is { } nota ? nota.ToString(FormatoNota, CultureInfo.InvariantCulture) : SinCalificar;

    private static string Promedio(decimal? valor) =>
        valor is { } promedio ? promedio.ToString(FormatoPromedio, CultureInfo.InvariantCulture) : "no disponible";
}
