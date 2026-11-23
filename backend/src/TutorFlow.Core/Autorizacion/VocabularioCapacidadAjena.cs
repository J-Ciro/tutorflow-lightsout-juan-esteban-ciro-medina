namespace TutorFlow.Core.Autorizacion;

internal static class VocabularioCapacidadAjena
{
    public static readonly string[] ActoresDelEstudiante =
    [
        "coordinador", "coordinadora", "coordinadores", "coordinadoras", "coord", "coordinacion",
        "coordinator", "coordinators", "advisor", "director del programa", "directora del programa",
        "director de programa", "tutor del programa", "tutora del programa", "program director",
        "administrador", "administradores", "admin", "otro estudiante", "otra estudiante",
        "otro usuario", "otros usuarios", "companero", "companeros", "companera", "companeras"
    ];

    public static readonly string[] ActoresDelCoordinador =
    [
        "otro coordinador", "otra coordinadora", "otros coordinadores", "otras coordinadoras",
        "los coordinadores", "los demas coordinadores", "cada coordinador", "coordinador de",
        "coordinadora de", "other coordinator", "other coordinators", "coordinator of",
        "administrador", "administradores", "admin"
    ];

    public static readonly string[] ActoresDeRol =
    [
        "otro rol", "otros roles", "cada rol", "los roles", "rol estudiante", "rol de estudiante",
        "rol coordinador", "rol de coordinador"
    ];

    public static readonly string[] EstudiantesGenericos =
    [
        "estudiante", "estudiantes", "alumno", "alumnos", "student", "students"
    ];

    public static readonly string[] Capacidad =
    [
        "ver", "ve", "ven", "consultar", "acceder", "accede", "acceden", "acceso", "saber", "sabe",
        "saben", "conoce", "conocen", "see", "sees", "view", "access", "know", "knows"
    ];

    public static readonly string[] CapacidadDeTenencia = ["tiene", "tienen", "has", "have"];

    public static readonly string[] CapacidadDelCoordinador = ["hacer", "hace", "hacen", "do"];

    public static readonly string[] IntroductoresDeContenido =
    [
        "que", "cual", "cuales", "cuanto", "cuanta", "cuantos", "cuantas", "lo que", "hasta donde",
        "what", "which", "how much", "how many"
    ];

    public static readonly string[] ModalesQueSiguen =
        ["puede", "pueden", "podria", "podrian", "can", "could", "does", "would"];

    public static readonly string[] ContenidoQueSigue =
    [
        "dato", "datos", "informacion", "notas", "cosas", "estudiantes", "alumnos", "acceso", "permiso",
        "herramienta", "data", "information", "students", "things"
    ];

    public static readonly string[] PermisosYSimulacion =
    [
        "permisos", "herramientas", "privilegios", "configuracion", "visibilidad", "permissions", "tools",
        "privileges", "veria", "verian", "would see", "le respondes", "le responderias", "le contestas",
        "responde como", "responder como", "contesta como", "actua como", "habla como", "si fueras",
        "imagina que eres", "finge que eres", "finge ser", "simula ser", "haz de", "act as", "pretend",
        "if you were"
    ];

    public static readonly string[] AlPreguntar = ["si pregunta", "si preguntara", "cuando pregunta"];
}
