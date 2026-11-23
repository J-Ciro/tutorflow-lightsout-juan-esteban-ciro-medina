using TutorFlow.Api.Tests.Dobles;
using TutorFlow.Core.Asistente;
using static TutorFlow.Api.Tests.Dobles.ConfiguracionDePrueba;

namespace TutorFlow.Api.Tests.Operacion;

public sealed class SinTopePorDefectoApiTests
{
    private const string Pregunta = "¿Cuál es mi promedio?";
    private const string ClaveTasa = "Limites:PreguntasPorMinutoPorIp";
    private const string ClavePresupuesto = "Limites:PresupuestoDiarioUsd";
    private const string LineaSinTopeTasa = "Límite sin tope: Limites:PreguntasPorMinutoPorIp";
    private const string LineaSinTopePresupuesto = "Límite sin tope: Limites:PresupuestoDiarioUsd";
    private const string IpDelEvaluador = "172.17.0.1";
    private const int TotalDePeticiones = 2000;
    private const int Concurrencia = 8;
    private const int LlegadasAlLlm = 600;
    private const int PeticionesPorCiclo = 20;
    private const string Q1 = "¿Cuál es la nota mínima para aprobar una asignatura?";
    private const string Q2 = "¿Cuál es mi promedio ponderado del periodo?";
    private const string Q6 = "¿Qué estudiantes de mi programa tienen alguna asignatura perdida?";
    private const string Q10 = "¿Cuál es la nota de Bases de Datos de Andrés Martínez?";
    private const string Ajena = "¿Cuáles son las notas de EST-001?";
    private const string OtroCoordinador = "¿Qué puede ver otro coordinador?";
    private const string FueraDeCorpus = "¿Cuál es la capital de Francia?";

    private static readonly (string Rol, string Id, string Pregunta, int Esperado)[] Ciclo =
    [
        ("estudiante", "EST-001", Q1, 200),
        ("estudiante", "EST-001", Q2, 200),
        ("coordinador", "COORD-02", Q6, 200),
        ("estudiante", "EST-001", Q10, 403),
        ("estudiante", "EST-002", Ajena, 403),
        ("estudiante", "EST-003", Ajena, 403),
        ("estudiante", "EST-004", FueraDeCorpus, 200),
        ("estudiante", "EST-005", Ajena, 403),
        ("estudiante", "EST-006", Ajena, 403),
        ("coordinador", "COORD-01", OtroCoordinador, 403),
        ("coordinador", "COORD-02", OtroCoordinador, 403),
        ("estudiante", "EST-999", Pregunta, 403),
        ("coordinador", "COORD-99", Q6, 403),
        ("estudiante", "EST-001", Q1, 200),
        ("estudiante", "EST-001", Q2, 200),
        ("coordinador", "COORD-02", Q6, 200),
        ("estudiante", "EST-999", Ajena, 403),
        ("coordinador", "COORD-99", OtroCoordinador, 403),
        ("estudiante", "EST-004", FueraDeCorpus, 200),
        ("estudiante", "EST-001", "¿Cuáles son las notas de EST-999?", 403)
    ];

    public static TheoryData<string?> ValoresSinTope => new() { null, "", "   " };

    private static ClienteLlmDoble LlmDeLaRafaga() =>
        GuionesLlm.SinHerramientas(solicitud =>
        {
            var texto = string.Join("\n", solicitud.Mensajes.Select(m => m.Contenido));
            if (texto.Contains("estudiantes de mi programa", StringComparison.Ordinal))
            {
                return "Valentina Castro: Microeconomía (1.8) y Estadística (2.3).";
            }

            return texto.Contains("nota mínima para aprobar una asignatura", StringComparison.Ordinal)
                ? "Se aprueba con 3.0 en una escala de 0.0 a 5.0 (reglamento_academico_2025.md · Artículo 5)."
                : "Tu promedio es 4.39.";
        });

    private static async Task<RespuestaApi[]> EnviarRafagaAsync(FabricaApi fabrica)
    {
        var respuestas = new RespuestaApi[TotalDePeticiones];
        var opciones = new ParallelOptions { MaxDegreeOfParallelism = Concurrencia };
        await Parallel.ForEachAsync(Enumerable.Range(0, TotalDePeticiones), opciones, async (i, _) =>
        {
            var (rol, id, pregunta, _) = Ciclo[i % PeticionesPorCiclo];
            respuestas[i] = await fabrica.PreguntarAsync(rol, id, pregunta, IpDelEvaluador);
        });
        return respuestas;
    }

    [Fact]
    [Trait("CA", "CA-12")]
    [Trait("ESC", "ESC-084")]
    [Trait("SEG", "SEG-13")]
    [Trait("D", "D-70")]
    public async Task ESC_084d_Dos_mil_peticiones_en_rafaga_con_los_valores_por_defecto_no_reciben_429_ni_503()
    {
        var llm = LlmDeLaRafaga();
        using var fabrica = new FabricaApi(llm: llm, variablesEntorno: EntornoSinPrecio());
        await fabrica.EnviarAsync(HttpMethod.Get, "/api/salud", null);

        var respuestas = await EnviarRafagaAsync(fabrica);

        Assert.Equal(0, respuestas.Count(r => r.Estado == 429));
        Assert.Equal(0, respuestas.Count(r => r.Estado == 503));
        Assert.Equal(LlegadasAlLlm, llm.Llamadas);
        for (var i = 0; i < TotalDePeticiones; i++)
        {
            Assert.Equal(Ciclo[i % PeticionesPorCiclo].Esperado, respuestas[i].Estado);
        }

        Assert.NotEmpty(fabrica.Auditoria.MultiplesIdentidades);
        Assert.All(fabrica.Auditoria.MultiplesIdentidades, e => Assert.False(e.Bloqueada));
    }

    [Fact]
    [Trait("CA", "CA-12")]
    [Trait("ESC", "ESC-084")]
    [Trait("RF", "RNF-05")]
    [Trait("D", "D-70")]
    public async Task ESC_084d_El_log_de_arranque_registra_la_tasa_y_el_presupuesto_sin_tope()
    {
        using var fabrica = new FabricaApi(variablesEntorno: EntornoSinPrecio());

        await fabrica.EnviarAsync(HttpMethod.Get, "/api/salud", null);

        Assert.Contains(LineaSinTopeTasa, fabrica.Logs.Mensajes);
        Assert.Contains(LineaSinTopePresupuesto, fabrica.Logs.Mensajes);
    }

    [Theory]
    [MemberData(nameof(ValoresSinTope))]
    [Trait("CA", "CA-15")]
    [Trait("ESC", "ESC-181")]
    [Trait("SEG", "SEG-13")]
    [Trait("D", "D-70")]
    public async Task ESC_181v_Un_presupuesto_vacio_o_ausente_arranca_sin_tope_ni_503_por_presupuesto(string? presupuesto)
    {
        var configuracion = new Dictionary<string, string?> { [ClavePresupuesto] = presupuesto, ["Llm:ReservaSinPrecioUsd"] = "3" };
        var llm = ClienteLlmDoble.Texto("Tu promedio es 4.39.");
        using var fabrica = new FabricaApi(llm: llm, configuracion: configuracion, variablesEntorno: EntornoSinPrecio());
        await fabrica.EnviarAsync(HttpMethod.Get, "/api/salud", null);

        var respuestas = new List<RespuestaApi>();
        for (var i = 0; i < 3; i++)
        {
            respuestas.Add(await fabrica.PreguntarEstudianteAsync("EST-001", Pregunta));
        }

        Assert.All(respuestas, r => Assert.Equal(200, r.Estado));
        Assert.Equal(3, llm.Llamadas);
        Assert.Contains(LineaSinTopePresupuesto, fabrica.Logs.Mensajes);
        Assert.DoesNotContain(fabrica.Logs.Mensajes, m => m.Contains(ResultadoArranque.PrefijoIgnorada, StringComparison.Ordinal));
    }

    [Theory]
    [MemberData(nameof(ValoresSinTope))]
    [Trait("CA", "CA-15")]
    [Trait("ESC", "ESC-181")]
    [Trait("SEG", "SEG-13")]
    [Trait("D", "D-70")]
    public async Task ESC_181ag_Una_tasa_vacia_o_ausente_arranca_sin_tope_y_las_61_preguntas_dan_200(string? tasa)
    {
        var configuracion = new Dictionary<string, string?> { [ClaveTasa] = tasa };
        using var fabrica = new FabricaApi(configuracion: configuracion);
        await fabrica.EnviarAsync(HttpMethod.Get, "/api/salud", null);

        var respuestas = new List<RespuestaApi>();
        for (var i = 0; i < 61; i++)
        {
            respuestas.Add(await fabrica.PreguntarEstudianteAsync("EST-001", Pregunta, "10.0.0.5"));
        }

        Assert.All(respuestas, r => Assert.Equal(200, r.Estado));
        Assert.Contains(LineaSinTopeTasa, fabrica.Logs.Mensajes);
    }

    [Fact]
    [Trait("CA", "CA-15")]
    [Trait("ESC", "ESC-181")]
    [Trait("SEG", "SEG-13")]
    [Trait("D", "D-70")]
    public async Task ESC_181ah_Con_tasa_60_y_presupuesto_5_no_hay_linea_de_sin_tope_y_ambos_se_aplican()
    {
        var configuracion = new Dictionary<string, string?> { [ClaveTasa] = "60", [ClavePresupuesto] = "5", ["Llm:ReservaSinPrecioUsd"] = "3" };
        using var fabrica = new FabricaApi(configuracion: configuracion, variablesEntorno: EntornoSinPrecio());
        await fabrica.EnviarAsync(HttpMethod.Get, "/api/salud", null);

        var primera = await fabrica.PreguntarEstudianteAsync("EST-001", Pregunta);
        var segunda = await fabrica.PreguntarEstudianteAsync("EST-001", Pregunta);

        Assert.Equal(200, primera.Estado);
        Assert.Equal(503, segunda.Estado);
        Assert.DoesNotContain(fabrica.Logs.Mensajes, m => m.Contains("Límite sin tope", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("CA", "CA-15")]
    [Trait("ESC", "ESC-181")]
    [Trait("SEG", "SEG-13")]
    [Trait("D", "D-70")]
    public async Task ESC_181ah_Con_tasa_60_la_pregunta_61_da_429()
    {
        var configuracion = new Dictionary<string, string?> { [ClaveTasa] = "60", [ClavePresupuesto] = "5" };
        using var fabrica = new FabricaApi(configuracion: configuracion);
        RespuestaApi ultima = null!;

        for (var i = 0; i < 61; i++)
        {
            ultima = await fabrica.PreguntarEstudianteAsync("EST-001", Pregunta, "10.0.0.5");
        }

        Assert.Equal(429, ultima.Estado);
        Assert.DoesNotContain(fabrica.Logs.Mensajes, m => m.Contains("Límite sin tope", StringComparison.Ordinal));
    }
}
