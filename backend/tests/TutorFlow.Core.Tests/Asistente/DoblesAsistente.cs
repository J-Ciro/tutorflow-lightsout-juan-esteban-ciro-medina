using System.Text.RegularExpressions;
using TutorFlow.Core.Academico;
using TutorFlow.Core.Asistente;
using TutorFlow.Core.Autorizacion;
using TutorFlow.Core.Conocimiento;
using TutorFlow.Pruebas.Soporte;

namespace TutorFlow.Core.Tests.Asistente;

internal sealed class ApiAcademicaDoble(DatosAcademicos datos) : IApiAcademica
{
    public List<string> LecturasNotas { get; } = [];

    public List<string> LecturasAsistencia { get; } = [];

    public Task<Estudiante?> ObtenerEstudianteAsync(string id, CancellationToken cancellationToken) =>
        Task.FromResult(datos.Estudiantes.FirstOrDefault(e => e.Id == id));

    public Task<Coordinador?> ObtenerCoordinadorAsync(string id, CancellationToken cancellationToken) =>
        Task.FromResult(datos.Coordinadores.FirstOrDefault(c => c.Id == id));

    public Task<IReadOnlyList<NotasAsignatura>> ObtenerNotasAsync(string estudianteId, CancellationToken cancellationToken)
    {
        LecturasNotas.Add(estudianteId);
        return Task.FromResult(datos.NotasDe(estudianteId));
    }

    public Task<IReadOnlyList<AsistenciaAsignatura>> ObtenerAsistenciaAsync(string estudianteId, CancellationToken cancellationToken)
    {
        LecturasAsistencia.Add(estudianteId);
        return Task.FromResult(datos.AsistenciaDe(estudianteId));
    }

    public Task<IReadOnlyList<Estudiante>> ObtenerEstudiantesDeProgramaAsync(string programaId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Estudiante>>([.. datos.Estudiantes.Where(e => e.ProgramaId == programaId)]);

    public Task<IReadOnlyList<Programa>> ObtenerProgramasAsync(CancellationToken cancellationToken) =>
        Task.FromResult(datos.Programas);
}

internal sealed class LlmDoble(Func<SolicitudLlm, int, RespuestaLlm> guion) : IClienteLlm
{
    public List<SolicitudLlm> Solicitudes { get; } = [];

    public Task<RespuestaLlm> CompletarAsync(SolicitudLlm solicitud, CancellationToken cancellationToken)
    {
        Solicitudes.Add(solicitud);
        var respuesta = guion(solicitud, Solicitudes.Count - 1);
        return Task.FromResult(respuesta with { Uso = respuesta.Uso with { Entrada = ConsumoRealista.Reportados(solicitud) } });
    }

    public string TextoDeLlamada(int indice) =>
        string.Join("\n", Solicitudes[indice].Mensajes.Select(m => m.Contenido));
}

internal sealed class IndiceFijo(IReadOnlyList<SeccionDocumento> indice, IReadOnlyList<SeccionDocumento> entregadas) : IBaseConocimiento
{
    public IReadOnlyList<SeccionDocumento> BuscarSecciones(string consulta, int maximo) => entregadas;


    public SeccionDocumento? ObtenerSeccion(Cita cita) =>
        indice.FirstOrDefault(s => s.Documento == cita.Documento && s.Seccion == cita.Seccion);
}

internal sealed class IndiceRelevante : IBaseConocimiento
{
    public IReadOnlyList<SeccionDocumento> BuscarSecciones(string consulta, int maximo) =>
        [new SeccionDocumento("reglamento_academico_2025.md", "Artículo 99", "Ley", consulta)];


    public SeccionDocumento? ObtenerSeccion(Cita cita) => null;
}

internal sealed class AuditoriaDoble : IAuditoria
{
    public List<EventoAuditoria> Eventos { get; } = [];

    public void Registrar(EventoAuditoria evento) => Eventos.Add(evento);

    public void RegistrarMultiplesIdentidades(EventoMultiplesIdentidades evento)
    {
    }
}

internal sealed class RelojFijo : IReloj
{
    public DateTimeOffset AhoraUtc { get; } = new(2026, 11, 22, 12, 0, 0, TimeSpan.Zero);
}

internal sealed class EscenarioAsistente
{
    public const string PreguntaPromedio = "¿Cuál es mi promedio?";

    private readonly PreguntarAsistente _asistente;

    private EscenarioAsistente(Func<SolicitudLlm, int, RespuestaLlm> guion, IBaseConocimiento baseConocimiento, string? canario, DatosAcademicos datos)
    {
        var reloj = new RelojFijo();
        var limites = new LimitesAsistente();
        Api = new ApiAcademicaDoble(datos);
        Llm = new LlmDoble(guion);
        Auditoria = new AuditoriaDoble();
        _asistente = new PreguntarAsistente(
            Api,
            Llm,
            baseConocimiento,
            new AuditorPeticiones(Auditoria, reloj),
            new PoliticaAcceso(),
            new VerificadorSalida(),
            limites,
            new ConfiguracionAsistente(null, canario, datos.Directorio.CodigosAsignatura, catalogo: CatalogoDePrueba.Asignaturas),
            new PresupuestoDiario(reloj, limites));
    }

    public ApiAcademicaDoble Api { get; }

    public LlmDoble Llm { get; }

    public AuditoriaDoble Auditoria { get; }

    public static EscenarioAsistente Con(
        Func<SolicitudLlm, int, RespuestaLlm> guion,
        IBaseConocimiento? baseConocimiento = null,
        string? canario = null,
        DatosAcademicos? datos = null) =>
        new(guion, baseConocimiento ?? new IndiceRelevante(), canario, datos ?? DatosReferencia.Cargar());

    public static RespuestaLlm Texto(string texto) => new(texto, [], new UsoTokens(10, 5), false);

    public static RespuestaLlm Herramienta(string nombre, string argumentosJson = "{}") =>
        new(null, [new LlamadaHerramienta("llamada-1", nombre, argumentosJson)], new UsoTokens(10, 5), false);

    public static Func<SolicitudLlm, int, RespuestaLlm> Fijo(string texto) => (_, _) => Texto(texto);

    public static string FragmentoDelPrompt(string prompt, int palabras)
    {
        var tokens = prompt.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        var fijos = TextosFijosNormalizados();
        for (var inicio = 0; inicio + palabras <= tokens.Length; inicio++)
        {
            var ventana = tokens[inicio..(inicio + palabras)];
            if (ventana.All(SoloLetras) && !fijos.Contains(NormalizadorTexto.Normalizar(string.Join(' ', ventana)), StringComparison.Ordinal))
            {
                return string.Join(' ', ventana);
            }
        }

        throw new InvalidOperationException("El prompt no tiene un fragmento de solo letras fuera de los textos fijos.");
    }

    public Task<ResultadoPregunta> PreguntarAsync(Usuario usuario, string pregunta) =>
        _asistente.EjecutarAsync(new PreguntaEntrada(pregunta, usuario), "10.0.0.1", Guid.CreateVersion7().ToString(), CancellationToken.None);

    private static string TextosFijosNormalizados() =>
        NormalizadorTexto.Normalizar(string.Join(' ', Textos.SinInformacion, Textos.NegativaEstudiante, Textos.NegativaCoordinador, Textos.NoVerificable));

    private static bool SoloLetras(string palabra) => Regex.IsMatch(palabra, @"^\p{L}+$");
}
