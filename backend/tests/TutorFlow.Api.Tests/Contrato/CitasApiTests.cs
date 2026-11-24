using TutorFlow.Api.Tests.Dobles;
using TutorFlow.Core.Asistente;
using TutorFlow.Core.Conocimiento;

namespace TutorFlow.Api.Tests.Contrato;

public sealed class CitasApiTests
{
    private const string CitaArticulo5 = "reglamento_academico_2025.md · Artículo 5";
    private const string CitaSeccion4 = "evaluacion_y_calificaciones.md · Sección 4";
    private const string ListadoDeCitas = $"({CitaArticulo5}; {CitaSeccion4})";
    private const string PreguntaDeCoordinador = "¿Qué dice el reglamento sobre la habilitación?";

    private static readonly SeccionDocumento Entregada1 = new("bienestar_y_becas.md", "Sección 1", "Horario de atención de Bienestar", "Bienestar atiende de lunes a viernes en horario de oficina.");
    private static readonly SeccionDocumento Entregada2 = new("reglamento_academico_2025.md", "Artículo 7", "Atención al estudiante", "El estudiante recibe atención en Bienestar según el horario publicado.");
    private static readonly SeccionDocumento NoEntregada1 = new("evaluacion_y_calificaciones.md", "Sección 2", "Cortes", "Los cortes evalúan el avance del periodo.");
    private static readonly SeccionDocumento NoEntregada2 = new("reglamento_academico_2025.md", "Artículo 9", "Matrícula", "La matrícula se renueva cada periodo.");

    private sealed class IndiceDePrueba : IBaseConocimiento
    {
        private static readonly SeccionDocumento[] Todas = [Entregada1, Entregada2, NoEntregada1, NoEntregada2];

        public IReadOnlyList<SeccionDocumento> BuscarSecciones(string consulta, int maximo) => [Entregada1, Entregada2];


        public SeccionDocumento? ObtenerSeccion(Cita cita) =>
            Todas.FirstOrDefault(s => s.Documento == cita.Documento && s.Seccion == cita.Seccion);
    }

    private static string Cita(SeccionDocumento seccion) => $"{seccion.Documento} · {seccion.Seccion}";

    [Fact]
    [Trait("CA", "CA-14")]
    [Trait("ESC", "ESC-156")]
    [Trait("RF", "COMP-02")]
    public async Task ESC_156_El_403_tardio_lleva_solo_la_cita_del_articulo_22()
    {
        using var fabrica = new FabricaApi(llm: ClienteLlmDoble.PideEstudiante("EST-004"));

        var respuesta = await fabrica.PreguntarCoordinadorAsync("COORD-01", PreguntaDeCoordinador);

        Assert.Equal(403, respuesta.Estado);
        Assert.Equal([Afirmaciones.CitaNegativa], respuesta.Citas);
    }

    [Fact]
    [Trait("CA", "CA-14")]
    [Trait("ESC", "ESC-156")]
    [Trait("RF", "COMP-02")]
    public async Task ESC_156_El_texto_de_comp_03_por_umbral_lleva_citas_vacias_y_no_invoca_al_modelo()
    {
        var llm = ClienteLlmDoble.Falla(new InvalidOperationException("llm"));
        using var fabrica = new FabricaApi(llm: llm, conocimientoReal: true);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", "¿Qué dice el reglamento sobre el uso de parqueaderos?");

        Afirmaciones.SinInformacionSinLlm(respuesta, fabrica);
    }

    [Fact]
    [Trait("CA", "CA-14")]
    [Trait("ESC", "ESC-156")]
    [Trait("RF", "COMP-02")]
    public async Task ESC_156_El_texto_de_comp_03_tras_el_modelo_lleva_citas_vacias_aunque_el_modelo_proponga_otras()
    {
        var llm = GuionesLlm.SinHerramientas(_ => $"Respuesta sin sustento. {ListadoDeCitas}");
        using var fabrica = new FabricaApi(llm: llm, conocimiento: new IndiceBiblioteca());

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", "¿Cuándo atiende la biblioteca?");

        Assert.Equal(Textos.SinInformacion, respuesta.Respuesta);
        Assert.Empty(respuesta.Citas);
    }

    [Fact]
    [Trait("CA", "CA-14")]
    [Trait("ESC", "ESC-156")]
    [Trait("RF", "COMP-02")]
    public async Task ESC_156_La_fuga_de_un_tercero_responde_la_negativa_con_solo_la_cita_del_articulo_22()
    {
        var llm = GuionesLlm.SinHerramientas(_ => $"Andres Martinez sacó 2.6 en Cálculo {ListadoDeCitas}");
        using var fabrica = new FabricaApi(llm: llm);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", "¿Cuál es mi promedio?");

        Assert.Equal(200, respuesta.Estado);
        Assert.Equal(Textos.NegativaEstudiante, respuesta.Respuesta);
        Assert.Equal([Afirmaciones.CitaNegativa], respuesta.Citas);
    }

    [Fact]
    [Trait("CA", "CA-14")]
    [Trait("ESC", "ESC-156")]
    [Trait("RF", "COMP-02")]
    public async Task ESC_156_Un_numero_no_verificable_responde_con_citas_vacias()
    {
        var llm = ClienteLlmDoble.LeeNotasYAsistencia(_ => $"Tu promedio es 4.77 {ListadoDeCitas}");
        using var fabrica = new FabricaApi(llm: llm);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", "¿Cómo se calcula el promedio ponderado del periodo?");

        Assert.Equal(Textos.NoVerificable, respuesta.Respuesta);
        Assert.Empty(respuesta.Citas);
    }

    [Fact]
    [Trait("CA", "CA-14")]
    [Trait("ESC", "ESC-162")]
    [Trait("RF", "COMP-02")]
    public async Task ESC_162_Las_citas_de_documentos_son_solo_de_secciones_entregadas_al_llm()
    {
        var propuesta = string.Join("; ", new[] { Entregada1, Entregada2, NoEntregada1, NoEntregada2 }.Select(Cita));
        using var fabrica = new FabricaApi(llm: GuionesLlm.SinHerramientas(_ => $"El horario de Bienestar está publicado ({propuesta})."), conocimiento: new IndiceDePrueba());

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", "¿Cuál es el horario de atención de Bienestar?");

        Assert.Equal(200, respuesta.Estado);
        var entregadas = GuionesLlm.Bloques(Assert.Single(fabrica.Llm.Solicitudes)).Select(b => b.Fuente).ToList();
        Assert.Equal([Cita(Entregada1), Cita(Entregada2)], entregadas.Order(StringComparer.Ordinal));
        Assert.NotEmpty(respuesta.Citas);
        Assert.All(respuesta.Citas, cita => Assert.Contains(cita, entregadas));
    }

    [Fact]
    [Trait("CA", "CA-14")]
    [Trait("ESC", "ESC-162")]
    [Trait("RF", "COMP-02")]
    public async Task ESC_162_Sin_lectura_de_datos_no_hay_citas_de_datos_del_estudiante()
    {
        using var fabrica = new FabricaApi(llm: GuionesLlm.SinHerramientas(_ => $"El horario de Bienestar está publicado ({Cita(Entregada1)})."), conocimiento: new IndiceDePrueba());

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", "¿Cuál es el horario de atención de Bienestar?");

        Assert.False(respuesta.UsoDatosEstudiante);
        Assert.DoesNotContain(respuesta.Citas, c => c.StartsWith("Datos del estudiante", StringComparison.Ordinal));
        Assert.Contains(Cita(Entregada1), respuesta.Citas);
    }
}
