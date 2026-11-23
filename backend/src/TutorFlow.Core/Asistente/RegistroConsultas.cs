using TutorFlow.Core.Conocimiento;

namespace TutorFlow.Core.Asistente;

public sealed class RegistroConsultas
{
    private readonly List<string> _estudiantes = [];
    private readonly List<RecursoConsultado> _recursos = [];
    private readonly List<Cita> _citas = [];
    private readonly List<string> _fuentes = [];

    public IReadOnlyList<string> Estudiantes => _estudiantes;

    public IReadOnlyList<RecursoConsultado> Recursos => _recursos;

    public IReadOnlyList<Cita> Citas => _citas;

    public IReadOnlyList<string> Fuentes => _fuentes;

    public bool UsoDatos => _estudiantes.Count > 0;

    public void Registrar(string estudianteId, IEnumerable<RecursoConsultado> recursos, Cita? cita)
    {
        AgregarSiFalta(_estudiantes, estudianteId);
        foreach (var recurso in recursos)
        {
            AgregarSiFalta(_recursos, recurso);
        }

        if (cita is not null)
        {
            AgregarSiFalta(_citas, cita);
        }
    }

    public void RegistrarCita(Cita cita) => AgregarSiFalta(_citas, cita);

    public void AgregarFuente(string texto) => _fuentes.Add(texto);

    private static void AgregarSiFalta<T>(List<T> lista, T valor)
    {
        if (!lista.Contains(valor))
        {
            lista.Add(valor);
        }
    }
}
