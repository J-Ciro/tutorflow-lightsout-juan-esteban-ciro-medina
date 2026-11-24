using TutorFlow.Core.Academico;

namespace TutorFlow.Core.Asistente;

internal sealed record EstudianteDePrograma(Estudiante Estudiante, ResumenEstudiante Resumen);

internal static class FormateadorPrograma
{
    private const string Ninguno = "ninguno";

    public static string LineaDeNumero(Programa programa, IReadOnlyList<EstudianteDePrograma> estudiantes) =>
        $"Programa {programa.Id} ({programa.Nombre}): {estudiantes.Count} estudiantes.";

    public static string Listado(ListadoDePrograma listado, Programa programa, IReadOnlyList<EstudianteDePrograma> estudiantes) => listado switch
    {
        ListadoDePrograma.Perdidas => $"Estudiantes con asignaturas perdidas: {ListaOninguno(estudiantes.Where(HayPerdidas).Select(DePerdidas))}",
        ListadoDePrograma.PruebaAcademica => $"Estudiantes en prueba académica: {ListaOninguno(estudiantes.Where(e => e.Resumen.PruebaAcademica).Select(DePrueba))}",
        ListadoDePrograma.SinDefinitiva => $"Estudiantes con asignaturas sin definitiva: {ListaOninguno(estudiantes.Select(e => DeAsignaturas(e, EstadoAsignatura.SinDefinitiva)).OfType<string>(), "; ")}",
        ListadoDePrograma.SinRegistro => $"Estudiantes con asistencia sin registro: {ListaOninguno(estudiantes.Select(DeSinRegistro).OfType<string>(), "; ")}",
        ListadoDePrograma.Promedios => $"Promedios del periodo: {ListaOninguno(estudiantes.Where(e => e.Resumen.Promedio is not null).Select(DePromedio), "; ")}",
        _ => LineaDeNumero(programa, estudiantes)
    };

    public static string? Cantidad(ListadoDePrograma listado, IReadOnlyList<EstudianteDePrograma> estudiantes) => listado switch
    {
        ListadoDePrograma.Perdidas => $"Cantidad de estudiantes con asignaturas perdidas: {estudiantes.Count(HayPerdidas)}",
        ListadoDePrograma.PruebaAcademica => $"Cantidad de estudiantes en prueba académica: {estudiantes.Count(e => e.Resumen.PruebaAcademica)}",
        ListadoDePrograma.SinDefinitiva => $"Cantidad de estudiantes con asignaturas sin definitiva: {estudiantes.Count(e => DeAsignaturas(e, EstadoAsignatura.SinDefinitiva) is not null)}",
        ListadoDePrograma.SinRegistro => $"Cantidad de estudiantes con asistencia sin registro: {estudiantes.Count(e => DeSinRegistro(e) is not null)}",
        _ => null
    };

    private static string? DeAsignaturas(EstudianteDePrograma estudiante, EstadoAsignatura estado) =>
        Agrupar(estudiante, estudiante.Resumen.Asignaturas.Where(resultado => resultado.Estado == estado));

    private static string? DeSinRegistro(EstudianteDePrograma estudiante) =>
        Agrupar(estudiante, estudiante.Resumen.Asignaturas.Where(resultado => resultado.EstadoAsistencia == EstadoAsistencia.SinRegistro));

    private static string? Agrupar(EstudianteDePrograma estudiante, IEnumerable<ResultadoAsignatura> resultados)
    {
        var nombres = resultados.Select(resultado => resultado.Asignatura.Nombre).ToList();
        return nombres.Count == 0 ? null : $"{estudiante.Estudiante.Nombre} ({string.Join(", ", nombres)})";
    }

    private static string DePromedio(EstudianteDePrograma estudiante) =>
        $"{estudiante.Estudiante.Nombre} {FormateadorDatos.Promedio(estudiante.Resumen.Promedio)}";

    public static string Bloque(Programa programa, IReadOnlyList<EstudianteDePrograma> estudiantes)
    {
        var lineas = new List<string> { LineaDeNumero(programa, estudiantes) };
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
        var asistencia = resultado.EstadoAsistencia == EstadoAsistencia.SinRegistro ? $"Asistencia sin registro: {resultado.Asignatura.Nombre}" : null;
        return new[] { LineaDeEstado(resultado), asistencia }.OfType<string>();
    }

    private static string? LineaDeEstado(ResultadoAsignatura resultado)
    {
        var nombre = resultado.Asignatura.Nombre;
        return resultado.Estado switch
        {
            EstadoAsignatura.Perdida => $"Perdida por nota: {nombre} (definitiva {FormateadorDatos.Nota(resultado.Definitiva)})",
            EstadoAsignatura.PerdidaPorInasistencia => $"Perdida por inasistencia: {nombre} (definitiva {FormateadorDatos.Nota(0m)})",
            EstadoAsignatura.SinDefinitiva => $"Sin definitiva (falta calificar un corte): {nombre}",
            _ => null
        };
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

    private static string ListaOninguno(IEnumerable<string> elementos, string separador = ", ")
    {
        var lista = elementos.ToList();
        return lista.Count == 0 ? Ninguno : string.Join(separador, lista);
    }
}
