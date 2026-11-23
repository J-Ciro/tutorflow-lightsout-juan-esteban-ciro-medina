using TutorFlow.Core.Academico;

namespace TutorFlow.Core.Asistente;

internal sealed record EstudianteDePrograma(Estudiante Estudiante, ResumenEstudiante Resumen);

internal static class FormateadorPrograma
{
    private const string Ninguno = "ninguno";

    public static string Bloque(Programa programa, IReadOnlyList<EstudianteDePrograma> estudiantes)
    {
        var lineas = new List<string> { $"Programa {programa.Id} ({programa.Nombre}): {estudiantes.Count} estudiantes." };
        foreach (var estudiante in estudiantes)
        {
            lineas.Add(string.Empty);
            lineas.AddRange(LineasDelEstudiante(estudiante));
        }

        lineas.Add(string.Empty);
        lineas.Add($"Estudiantes con asignaturas perdidas: {ListaOninguno(estudiantes.Where(HayPerdidas).Select(DePerdidas))}");
        lineas.Add($"Estudiantes en prueba académica: {ListaOninguno(estudiantes.Where(e => e.Resumen.PruebaAcademica).Select(DePrueba))}");
        return string.Join("\n", lineas);
    }

    private static IEnumerable<string> LineasDelEstudiante(EstudianteDePrograma estudiante)
    {
        var resumen = estudiante.Resumen;
        yield return $"Estudiante: {estudiante.Estudiante.Nombre} (ID {estudiante.Estudiante.Id})";
        if (resumen.Promedio is not null)
        {
            yield return $"Promedio del periodo: {FormateadorDatos.Promedio(resumen.Promedio)}";
        }

        yield return $"Prueba académica: {(resumen.PruebaAcademica ? "sí" : "no")}";
        foreach (var linea in resumen.Asignaturas.SelectMany(LineasDeLaAsignatura))
        {
            yield return linea;
        }
    }

    private static IEnumerable<string> LineasDeLaAsignatura(ResultadoAsignatura resultado)
    {
        var nombre = resultado.Asignatura.Nombre;
        var estado = resultado.Estado switch
        {
            EstadoAsignatura.Perdida => $"Perdida por nota: {nombre} (definitiva {FormateadorDatos.Nota(resultado.Definitiva)})",
            EstadoAsignatura.PerdidaPorInasistencia => $"Perdida por inasistencia: {nombre} (definitiva {FormateadorDatos.Nota(0m)})",
            EstadoAsignatura.SinDefinitiva => $"Sin definitiva (falta calificar un corte): {nombre}",
            _ => null
        };
        return new[] { estado, resultado.EstadoAsistencia == EstadoAsistencia.SinRegistro ? $"Asistencia sin registro: {nombre}" : null }.OfType<string>();
    }

    private static bool HayPerdidas(EstudianteDePrograma estudiante) =>
        estudiante.Resumen.Asignaturas.Any(resultado => resultado.Estado is EstadoAsignatura.Perdida or EstadoAsignatura.PerdidaPorInasistencia);

    private static string DePerdidas(EstudianteDePrograma estudiante)
    {
        var perdidas = estudiante.Resumen.Asignaturas
            .Where(resultado => resultado.Estado is EstadoAsignatura.Perdida or EstadoAsignatura.PerdidaPorInasistencia)
            .Select(resultado => resultado.Estado == EstadoAsignatura.PerdidaPorInasistencia
                ? $"{resultado.Asignatura.Nombre} {FormateadorDatos.Nota(0m)} por inasistencia"
                : $"{resultado.Asignatura.Nombre} {FormateadorDatos.Nota(resultado.Definitiva)}");
        return $"{estudiante.Estudiante.Nombre} ({string.Join("; ", perdidas)})";
    }

    private static string DePrueba(EstudianteDePrograma estudiante) =>
        $"{estudiante.Estudiante.Nombre} (promedio {FormateadorDatos.Promedio(estudiante.Resumen.Promedio)})";

    private static string ListaOninguno(IEnumerable<string> elementos)
    {
        var lista = elementos.ToList();
        return lista.Count == 0 ? Ninguno : string.Join(", ", lista);
    }
}
