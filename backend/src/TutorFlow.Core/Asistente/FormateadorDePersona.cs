using System.Globalization;
using TutorFlow.Core.Academico;
using TutorFlow.Core.Autorizacion;

namespace TutorFlow.Core.Asistente;

internal sealed record SeleccionDelBloque(
    IReadOnlyList<CampoDePersona> Campos,
    IReadOnlyCollection<string> ClavesNombradas,
    IReadOnlyList<string> NombresNombrados,
    bool Habilitacion,
    bool TerceraPersona);

internal static class FormateadorDePersona
{
    public static string Bloque(Estudiante estudiante, ResumenEstudiante resumen, SeleccionDelBloque seleccion)
    {
        var elegidas = resumen.Asignaturas
            .Where(resultado => seleccion.ClavesNombradas.Count == 0 || seleccion.ClavesNombradas.Contains(AmbitoPersonal.Clave(resultado.Asignatura.Codigo)))
            .ToList();
        var lineas = new List<string> { $"Datos de {estudiante.Nombre} ({estudiante.Id})" };
        lineas.AddRange(seleccion.Campos.SelectMany(campo => LineasDe(campo, resumen, elegidas, seleccion)).Distinct());
        lineas.AddRange(SinMatricula(seleccion, elegidas));
        return string.Join("\n", lineas);
    }

    private static IEnumerable<string> SinMatricula(SeleccionDelBloque seleccion, List<ResultadoAsignatura> elegidas) =>
        seleccion.ClavesNombradas.Count > 0 && elegidas.Count == 0 && seleccion.NombresNombrados.Count > 0
            ? [$"Sin matrícula en el periodo {Textos.Periodo}: {string.Join(", ", seleccion.NombresNombrados)}"]
            : [];

    private static IEnumerable<string> LineasDe(CampoDePersona campo, ResumenEstudiante resumen, List<ResultadoAsignatura> elegidas, SeleccionDelBloque seleccion) => campo switch
    {
        CampoDePersona.Definitiva => LineasDeDefinitiva(elegidas),
        CampoDePersona.Notas => elegidas.Select(LineaDeCortes).Concat(LineasDeDefinitiva(elegidas)),
        CampoDePersona.Estado => ParaEstado(resumen, elegidas, seleccion).Select(resultado => $"Estado de {resultado.Asignatura.Nombre}: {Estado(resultado)}"),
        CampoDePersona.Promedio => LineaDePromedio(resumen).Concat(LineasDeDefinitiva(resumen.Asignaturas)),
        CampoDePersona.PruebaAcademica => LineaDePromedio(resumen).Append($"Prueba académica: {(resumen.PruebaAcademica ? "sí" : "no")}"),
        CampoDePersona.Beca => LineaDePromedio(resumen).Append($"Beca por rendimiento académico: {Beca(resumen)}"),
        _ => TextoDelDesglose.Lineas(elegidas, seleccion.TerceraPersona)
    };

    private static IEnumerable<ResultadoAsignatura> ParaEstado(ResumenEstudiante resumen, List<ResultadoAsignatura> elegidas, SeleccionDelBloque seleccion) =>
        seleccion.Habilitacion && elegidas.Exists(resultado => resultado.Estado == EstadoAsignatura.PerdidaPorInasistencia)
            ? resumen.Asignaturas.Where(resultado => elegidas.Contains(resultado) || resultado.Estado == EstadoAsignatura.Perdida)
            : elegidas;

    private static IEnumerable<string> LineaDePromedio(ResumenEstudiante resumen) =>
        resumen.Promedio is null ? [] : [$"Promedio ponderado del periodo: {FormateadorDatos.Promedio(resumen.Promedio)}"];

    private static IEnumerable<string> LineasDeDefinitiva(IReadOnlyList<ResultadoAsignatura> resultados)
    {
        var conDefinitiva = resultados.Where(resultado => resultado.Estado != EstadoAsignatura.SinDefinitiva).ToList();
        var sinDefinitiva = resultados
            .Where(resultado => resultado.Estado == EstadoAsignatura.SinDefinitiva)
            .SelectMany(resultado => FormateadorDatos.LineaDeEquivalente(resultado).Prepend(FormateadorDatos.LineaSinDefinitiva(resultado)));
        var definitivas = conDefinitiva.Count == 0 ? [] : new[] { $"Definitivas: {string.Join(", ", conDefinitiva.Select(FormateadorDatos.DefinitivaEnLinea))}" };
        return definitivas.Concat(sinDefinitiva);
    }

    private static string LineaDeCortes(ResultadoAsignatura resultado)
    {
        var cortes = resultado.Asignatura.Cortes;
        return $"Notas de {resultado.Asignatura.Nombre}: corte 1 {FormateadorDatos.Nota(cortes.Corte1)}, corte 2 {FormateadorDatos.Nota(cortes.Corte2)}, corte 3 {FormateadorDatos.Nota(cortes.Corte3)}";
    }

    private static string Estado(ResultadoAsignatura resultado) => resultado.Estado switch
    {
        EstadoAsignatura.Aprobada => $"aprobada con definitiva {FormateadorDatos.Nota(resultado.Definitiva)}",
        EstadoAsignatura.Perdida => $"perdida con definitiva {FormateadorDatos.Nota(resultado.Definitiva)}, {(resultado.Habilitable ? "habilitable" : "no habilitable")}",
        EstadoAsignatura.PerdidaPorInasistencia => "perdida por inasistencia, no habilitable",
        _ => "sin definitiva (falta calificar un corte)"
    };

    private static string Beca(ResumenEstudiante resumen) => resumen.Beca switch
    {
        ElegibilidadBeca.Elegible => "elegible",
        ElegibilidadBeca.NoElegible => $"no elegible ({MotivoDeNoElegibilidad(resumen)})",
        _ => $"no determinable aún (asignatura sin definitiva: {string.Join(", ", resumen.Asignaturas.Where(r => r.Estado == EstadoAsignatura.SinDefinitiva).Select(r => r.Asignatura.Nombre))})"
    };

    private static string MotivoDeNoElegibilidad(ResumenEstudiante resumen)
    {
        if (resumen.Promedio < CalculosAcademicos.PromedioMinimoBeca)
        {
            return $"promedio menor a {CalculosAcademicos.PromedioMinimoBeca.ToString("0.0", CultureInfo.InvariantCulture)}";
        }

        return resumen.Asignaturas.Any(r => r.Estado is EstadoAsignatura.Perdida or EstadoAsignatura.PerdidaPorInasistencia)
            ? "asignatura perdida"
            : $"menos de {CalculosAcademicos.CreditosMinimosBeca} créditos";
    }
}
