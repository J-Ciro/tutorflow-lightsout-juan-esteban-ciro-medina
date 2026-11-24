namespace TutorFlow.Core.Asistente;

internal enum CampoDePersona
{
    Definitiva,
    Notas,
    Estado,
    Promedio,
    PruebaAcademica,
    Beca,
    Faltas
}

internal static class CamposDePersona
{
    private static readonly (CampoDePersona Campo, string[] Terminos)[] Tabla =
    [
        (CampoDePersona.Definitiva, ["definitiva", "definitivas", "nota final", "calificacion final", "nota"]),
        (CampoDePersona.Notas, ["notas", "corte", "cortes"]),
        (CampoDePersona.Estado, ["perdi", "perdida", "perdidas", "perdido", "aprobe", "aprobada", "gane", "reprobe", "pase", "habilitar", "habilitable", "estado", "voy"]),
        (CampoDePersona.Promedio, ["promedio", "ponderado"]),
        (CampoDePersona.PruebaAcademica, ["prueba academica"]),
        (CampoDePersona.Beca, ["beca", "becas"]),
        (CampoDePersona.Faltas, ["falta", "faltas", "inasistencia", "inasistencias", "asistencia"])
    ];

    public static IReadOnlyList<CampoDePersona> De(IReadOnlyList<string> palabras)
    {
        var texto = $" {string.Join(' ', palabras)} ";
        var pedidos = Tabla
            .Where(fila => fila.Terminos.Any(termino => texto.Contains($" {termino} ", StringComparison.Ordinal)))
            .Select(fila => fila.Campo)
            .ToList();
        return pedidos.Count == 0 ? [.. Enum.GetValues<CampoDePersona>()] : pedidos;
    }
}
