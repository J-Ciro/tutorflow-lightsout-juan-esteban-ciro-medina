using TutorFlow.Core.Academico;

namespace TutorFlow.Core.Asistente;

internal enum ListadoDePrograma
{
    Perdidas,
    PruebaAcademica,
    SinDefinitiva,
    SinRegistro,
    Promedios,
    Numero
}

internal static class ListadosDePrograma
{
    private const int ListadosPorDefecto = 4;

    private static readonly (ListadoDePrograma Listado, Func<IReadOnlyList<string>, string, bool> Pide)[] Tabla =
    [
        (ListadoDePrograma.Perdidas, (palabras, _) => palabras.Any(palabra => palabra.StartsWith("perd", StringComparison.Ordinal) || palabra.StartsWith("reprob", StringComparison.Ordinal))),
        (ListadoDePrograma.PruebaAcademica, (_, texto) => Contiene(texto, "prueba academica")),
        (ListadoDePrograma.SinDefinitiva, (_, texto) => ContieneAlguno(texto, "sin definitiva", "pendiente", "pendientes", "falta calificar")),
        (ListadoDePrograma.SinRegistro, (_, texto) => ContieneAlguno(texto, "sin registro", "asistencia")),
        (ListadoDePrograma.Promedios, (_, texto) => ContieneAlguno(texto, "promedio", "promedios")),
        (ListadoDePrograma.Numero, (_, texto) => ContieneAlguno(texto, "cuantos", "cuantas") && Contiene(texto, "estudiantes"))
    ];

    public static IReadOnlyList<ListadoDePrograma> Pedidos(IReadOnlyList<string> palabras)
    {
        var texto = $" {string.Join(' ', palabras)} ";
        var pedidos = Tabla.Where(fila => fila.Pide(palabras, texto)).Select(fila => fila.Listado).ToList();
        return pedidos.Count == 0 ? [.. Tabla.Take(ListadosPorDefecto).Select(fila => fila.Listado)] : pedidos;
    }

    public static string Bloque(IReadOnlyList<(Programa Programa, IReadOnlyList<EstudianteDePrograma> Estudiantes)> programas, IReadOnlyList<ListadoDePrograma> pedidos)
    {
        var grupos = programas.Select(programa => Grupo(programa.Programa, programa.Estudiantes, pedidos, programas.Count > 1));
        return string.Join("\n\n", grupos.Select(lineas => string.Join("\n", lineas.Distinct())));
    }

    private static IEnumerable<string> Grupo(Programa programa, IReadOnlyList<EstudianteDePrograma> estudiantes, IReadOnlyList<ListadoDePrograma> pedidos, bool variosProgramas)
    {
        var encabezado = variosProgramas ? [FormateadorPrograma.LineaDeNumero(programa, estudiantes)] : Array.Empty<string>();
        var listados = pedidos.Select(listado => FormateadorPrograma.Listado(listado, programa, estudiantes));
        return encabezado.Concat(listados).Concat(Cantidades(pedidos, estudiantes));
    }

    private static IEnumerable<string> Cantidades(IReadOnlyList<ListadoDePrograma> pedidos, IReadOnlyList<EstudianteDePrograma> estudiantes) =>
        pedidos.Contains(ListadoDePrograma.Numero)
            ? pedidos.Select(listado => FormateadorPrograma.Cantidad(listado, estudiantes)).OfType<string>()
            : [];

    private static bool Contiene(string texto, string termino) => texto.Contains($" {termino} ", StringComparison.Ordinal);

    private static bool ContieneAlguno(string texto, params string[] terminos) => terminos.Any(termino => Contiene(texto, termino));
}
