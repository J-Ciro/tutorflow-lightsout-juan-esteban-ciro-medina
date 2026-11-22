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

    public static string Negativa(Rol rol) => throw new NotImplementedException();

    public static Cita CitaNegativa() => throw new NotImplementedException();
}
