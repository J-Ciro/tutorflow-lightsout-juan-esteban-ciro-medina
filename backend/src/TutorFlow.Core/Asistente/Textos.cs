using TutorFlow.Core.Autorizacion;
using TutorFlow.Core.Conocimiento;

namespace TutorFlow.Core.Asistente;

public static class Textos
{
    public const string NegativaEstudiante = "No puedo compartir esa información: solo puedes consultar tus propios datos académicos.";
    public const string NegativaCoordinador = "No puedo compartir esa información: solo puedes consultar los datos de los estudiantes de tu programa.";
    public const string SinInformacion = "No encuentro esa información en los documentos disponibles.";
    public const string NoVerificable = "No pude verificar los datos de la respuesta. Intenta de nuevo o reformula la pregunta.";
    public const string DocumentoReglamentoVigente = "reglamento_academico_2025.md";
    public const string SeccionPrivacidad = "Artículo 22";
    public const string DocumentoDatosEstudiante = "Datos del estudiante";
    public const string DocumentoDatosPrograma = "Datos del programa";
    public const string SeccionNotas = "Notas";
    public const string SeccionAsistencia = "Asistencia";
    public const string SeccionDatosBasicos = "Datos básicos";
    public const string Periodo = "2026-2";
    public const string SeccionDefinitiva = "Sección 3";
    public const string SeccionPromedio = "Sección 4";
    public const string SeccionBecaPorRendimiento = "Sección 3.1";
    public const string SeccionNotaMinima = "Artículo 5";
    public const string SeccionHabilitacion = "Artículo 6";
    public const string SeccionNoHabilitables = "Artículo 7";
    public const string SeccionPerdidaPorInasistencia = "Artículo 11";
    public const string SeccionAsistenciaSinRegistro = "Artículo 12";
    public const string SeccionPruebaAcademica = "Artículo 20";
    public const string SeccionDerogacion = "Artículo 3";
    public const string HerramientaNoDisponible = "Herramienta no disponible.";
    public const string FaltaAsignatura = "Indica el nombre o el código de la asignatura.";
    public const string FaltaIdentificadorEstudiante = "Falta el identificador del estudiante.";
    public const string AsignaturaNoEncontrada = "No hay una asignatura con ese nombre o código.";
    public const string SinProgramasAsignados = "No tienes programas asignados, por lo que no hay datos de estudiantes para consultar.";
    public const string FormatoInvalido = "[FORMATO_INVALIDO]";
    public const string FuenteResumenPropio = "Resumen académico propio";

    public static string Negativa(Rol rol) => rol == Rol.Coordinador ? NegativaCoordinador : NegativaEstudiante;

    public static IReadOnlyList<Cita>? CitasDeTextoFijo(string texto) => texto.Trim() switch
    {
        NegativaEstudiante or NegativaCoordinador => [CitaNegativa()],
        NoVerificable => [],
        _ => null
    };

    public static Cita CitaNegativa() => new(DocumentoReglamentoVigente, SeccionPrivacidad);
}
