using TutorFlow.Core.Conocimiento;

namespace TutorFlow.Core.Asistente;

public sealed class RegistroConsultas
{
    private readonly List<string> _estudiantes = [];
    private readonly List<RecursoConsultado> _recursos = [];
    private readonly List<DatoConsultado> _datos = [];
    private readonly List<Cita> _citas = [];
    private readonly List<Cita> _reglas = [];
    private readonly List<string> _fuentes = [];

    public IReadOnlyList<string> Estudiantes => _estudiantes;

    public IReadOnlyList<RecursoConsultado> Recursos => _recursos;

    public IReadOnlyList<DatoConsultado> DatosConsultados => _datos;

    public IReadOnlyList<Cita> Citas => _citas;

    public IReadOnlyList<Cita> Reglas => _reglas;

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

    public void RegistrarDatos(IEnumerable<DatoConsultado> datos)
    {
        foreach (var dato in datos)
        {
            AgregarSiFalta(_datos, dato);
        }
    }

    public void RegistrarReglas(IEnumerable<Cita> reglas)
    {
        foreach (var regla in reglas)
        {
            AgregarSiFalta(_reglas, regla);
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
