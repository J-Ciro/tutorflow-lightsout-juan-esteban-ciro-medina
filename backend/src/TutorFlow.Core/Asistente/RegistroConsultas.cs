using TutorFlow.Core.Academico;
using TutorFlow.Core.Conocimiento;

namespace TutorFlow.Core.Asistente;

public sealed class RegistroConsultas
{
    private readonly List<string> _estudiantes = [];
    private readonly List<RecursoConsultado> _recursos = [];
    private readonly List<DatoConsultado> _datos = [];
    private readonly List<Cita> _citas = [];
    private readonly SortedSet<int> _entradasDeReglas = [];
    private readonly List<string> _fuentes = [];
    private bool _usoDatos;
    private readonly Dictionary<string, ResumenEstudiante> _resumenes = [];

    public IReadOnlyList<string> Estudiantes => _estudiantes;

    public IReadOnlyList<RecursoConsultado> Recursos => _recursos;

    public IReadOnlyList<DatoConsultado> DatosConsultados => _datos;

    public IReadOnlyList<Cita> Citas => _citas;


    public IReadOnlyList<Cita> Reglas => ReglasAplicadas.Citas(_entradasDeReglas);

    public IReadOnlyDictionary<string, ResumenEstudiante> Resumenes => _resumenes;

    public IReadOnlyList<string> Fuentes => _fuentes;

    public bool UsoDatos => _usoDatos;

    public void Registrar(string estudianteId, IEnumerable<RecursoConsultado> recursos, Cita? cita)
    {
        _usoDatos = true;
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

    public void RegistrarVerificacion(string estudianteId, IEnumerable<RecursoConsultado> recursos, IEnumerable<DatoConsultado> datos)
    {
        AgregarSiFalta(_estudiantes, estudianteId);
        foreach (var recurso in recursos)
        {
            AgregarSiFalta(_recursos, recurso);
        }

        RegistrarDatos(datos);
    }

    public void RegistrarDatos(IEnumerable<DatoConsultado> datos)
    {
        foreach (var dato in datos)
        {
            AgregarSiFalta(_datos, dato);
        }
    }

    public void RegistrarResumen(ResumenEstudiante resumen) => _resumenes[resumen.EstudianteId] = resumen;

    public void RegistrarReglas(IEnumerable<int> entradas) => _entradasDeReglas.UnionWith(entradas);

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
