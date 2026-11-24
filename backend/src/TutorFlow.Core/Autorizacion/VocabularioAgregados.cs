namespace TutorFlow.Core.Autorizacion;

internal static class VocabularioAgregados
{
    public const string RaizDeComparar = "compar";

    public static readonly string[] FuertesSueltas =
        ["ranking", "top", "rank", "por encima de la media", "mas que yo", "percentil"];

    public static readonly string[] Posiciones = ["posicion", "puesto", "lugar"];

    public static readonly string[] VerbosDePosicion = ["estoy", "quede", "ocupo"];

    public static readonly string[] PromediosDeGrupo = ["promedio", "media"];

    public static readonly string[] RellenosDelPromedio = ["de", "del", "la", "mi"];

    public static readonly string[] Colectivos = ["curso", "clase", "grupo", "programa", "carrera"];

    public static readonly string[] PluralesSuperlativos = ["mejores", "peores"];

    public static readonly string[] NotasPlurales = ["notas", "promedios"];

    public static readonly string[] Notas = ["nota", "notas", "promedio", "promedios", "gpa"];

    public static readonly string[] Posesivos = ["mi", "mis"];

    public static readonly string[] PosesivoPlural = ["mis"];

    public static readonly string[] Grupo =
    [
        "estudiantes", "alumnos", "companeros", "companeras", "personas", "gente", "alguien", "quien",
        "quienes", "los demas", "otros programas", "mi clase", "mi curso", "mi grupo", "students",
        "classmates", "anyone", "who"
    ];

    public static readonly string[] Superlativos =
    [
        "mejor", "mejores", "peor", "peores", "mas alto", "mas altos", "mas alta", "mas altas",
        "mas bajo", "mas bajos", "mas baja", "mas bajas", "best", "worst", "highest", "lowest"
    ];

    public static readonly string[] Listadores = ["lista", "listar", "list", "nombres"];

    public static readonly string[] RellenosDelListado = ["de", "los", "las", "todos", "todas", "all", "the"];

    public static readonly string[] VerbosDeListadoYResultado =
    [
        "enumera", "muestra", "dame", "perdieron", "sacaron", "aprobaron", "reprobaron", "failed", "passed"
    ];
}
