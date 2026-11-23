using TutorFlow.Api.Tests.Dobles;
using TutorFlow.Core.Asistente;
using static TutorFlow.Api.Tests.Dobles.ConfiguracionDePrueba;

namespace TutorFlow.Api.Tests.Operacion;

public sealed class SinTopePorDefectoApiTests
{
    private const string Pregunta = "¿Cuál es mi promedio?";
    private const string ClaveTasa = "Limites:PreguntasPorMinutoPorIp";
    private const string ClavePresupuesto = "Limites:PresupuestoDiarioUsd";
    private const string LineaTasaPorDefecto = "Tasa por IP: 600 preguntas por minuto";
    private const string LineaTasaMaxima = "Tasa por IP: 10000 preguntas por minuto";
    private const string LineaSinTopePresupuesto = "Límite sin tope: Limites:PresupuestoDiarioUsd";
    private const string IpDelEvaluador = "172.17.0.1";
    private const int Tandas = 4;
    private const int PeticionesPorTanda = 500;
    private const int SegundosEntreTandas = 60;
    private const int Concurrencia = 8;
    private const int LlegadasAlLlm = 600;
    private const int PeticionesPorCiclo = 20;
    private const int PeticionesPorMinutoPorDefecto = 600;
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

    private static async Task<RespuestaApi[]> EnviarTandaAsync(FabricaApi fabrica, DateTimeOffset origen, int tanda)
    {
        fabrica.Reloj.Fijar(origen + TimeSpan.FromSeconds(tanda * SegundosEntreTandas));
        var respuestas = new RespuestaApi[PeticionesPorTanda];
        var opciones = new ParallelOptions { MaxDegreeOfParallelism = Concurrencia };
        await Parallel.ForEachAsync(Enumerable.Range(0, PeticionesPorTanda), opciones, async (i, _) =>
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
    [Trait("D", "D-72")]
    public async Task ESC_084d_Cuatro_tandas_de_500_con_los_valores_por_defecto_no_reciben_429_ni_503()
    {
        var llm = LlmDeLaRafaga();
        using var fabrica = new FabricaApi(llm: llm, variablesEntorno: EntornoSinPrecio());
        await fabrica.EnviarAsync(HttpMethod.Get, "/api/salud", null);
        var origen = fabrica.Reloj.AhoraUtc;
        var respuestas = new List<RespuestaApi>();

        for (var tanda = 0; tanda < Tandas; tanda++)
        {
            respuestas.AddRange(await EnviarTandaAsync(fabrica, origen, tanda));
        }

        Assert.Equal(0, respuestas.Count(r => r.Estado == 429));
        Assert.Equal(0, respuestas.Count(r => r.Estado == 503));
        Assert.Equal(LlegadasAlLlm, llm.Llamadas);
        for (var i = 0; i < respuestas.Count; i++)
        {
            Assert.Equal(Ciclo[i % PeticionesPorCiclo].Esperado, respuestas[i].Estado);
        }

        Assert.NotEmpty(fabrica.Auditoria.MultiplesIdentidades);
    }

    [Fact]
    [Trait("CA", "CA-12")]
    [Trait("ESC", "ESC-084")]
    [Trait("RF", "RNF-05")]
    [Trait("D", "D-72")]
    public async Task ESC_084d_El_log_de_arranque_registra_la_tasa_de_600_y_el_presupuesto_sin_tope()
    {
        using var fabrica = new FabricaApi(variablesEntorno: EntornoSinPrecio());

        await fabrica.EnviarAsync(HttpMethod.Get, "/api/salud", null);

        Assert.Contains(LineaTasaPorDefecto, fabrica.Logs.Mensajes);
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
    [Trait("D", "D-72")]
    public async Task ESC_181ag_Una_tasa_vacia_o_ausente_vale_600_las_600_primeras_dan_200_y_la_601_da_429_con_30_s(string? tasa)
    {
        var configuracion = new Dictionary<string, string?> { [ClaveTasa] = tasa };
        using var fabrica = new FabricaApi(configuracion: configuracion);
        await fabrica.EnviarAsync(HttpMethod.Get, "/api/salud", null);
        var respuestas = new List<RespuestaApi>();

        for (var i = 0; i <= PeticionesPorMinutoPorDefecto; i++)
        {
            respuestas.Add(await fabrica.PreguntarEstudianteAsync("EST-001", Pregunta, "10.0.0.5"));
        }

        Assert.All(respuestas.Take(PeticionesPorMinutoPorDefecto), r => Assert.Equal(200, r.Estado));
        Assert.Equal(429, respuestas[^1].Estado);
        Assert.Equal("30", respuestas[^1].Encabezado("Retry-After"));
        Assert.Contains(LineaTasaPorDefecto, fabrica.Logs.Mensajes);
        Assert.DoesNotContain(fabrica.Logs.Mensajes, m => m.Contains("Límite sin tope: Limites:PreguntasPorMinutoPorIp", StringComparison.Ordinal));
        Assert.DoesNotContain(fabrica.Logs.Mensajes, m => m.Contains(ResultadoArranque.PrefijoIgnorada, StringComparison.Ordinal));
    }

    [Fact]
    [Trait("CA", "CA-15")]
    [Trait("ESC", "ESC-181")]
    [Trait("SEG", "SEG-13")]
    [Trait("D", "D-72")]
    public async Task ESC_181ah_Con_tasa_10000_y_presupuesto_5_ambos_se_aplican_y_no_hay_linea_de_sin_tope()
    {
        var configuracion = new Dictionary<string, string?> { [ClaveTasa] = "10000", [ClavePresupuesto] = "5", ["Llm:ReservaSinPrecioUsd"] = "3" };
        using var fabrica = new FabricaApi(configuracion: configuracion, variablesEntorno: EntornoSinPrecio());
        await fabrica.EnviarAsync(HttpMethod.Get, "/api/salud", null);

        var primera = await fabrica.PreguntarEstudianteAsync("EST-001", Pregunta);
        var segunda = await fabrica.PreguntarEstudianteAsync("EST-001", Pregunta);

        Assert.Equal(200, primera.Estado);
        Assert.Equal(503, segunda.Estado);
        Assert.Contains(LineaTasaMaxima, fabrica.Logs.Mensajes);
        Assert.DoesNotContain(fabrica.Logs.Mensajes, m => m.Contains("Límite sin tope", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("CA", "CA-15")]
    [Trait("ESC", "ESC-181")]
    [Trait("SEG", "SEG-13")]
    [Trait("D", "D-72")]
    public async Task ESC_181ah_Con_tasa_10000_la_pregunta_601_en_un_minuto_sigue_dando_200()
    {
        using var fabrica = new FabricaApi(configuracion: TasaPorMinuto(10000));
        RespuestaApi ultima = null!;

        for (var i = 0; i <= PeticionesPorMinutoPorDefecto; i++)
        {
            ultima = await fabrica.PreguntarEstudianteAsync("EST-001", Pregunta, "10.0.0.5");
        }

        Assert.Equal(200, ultima.Estado);
    }

    [Fact]
    [Trait("CA", "CA-15")]
    [Trait("ESC", "ESC-181")]
    [Trait("SEG", "SEG-16")]
    [Trait("D", "D-72")]
    public async Task ESC_181ai_Las_claves_retiradas_se_ignoran_sin_advertencia_ni_429_por_identidades()
    {
        var configuracion = new Dictionary<string, string?>
        {
            ["Limites:BloquearMultiplesIdentidades"] = "true",
            ["Proxy:IpsConfiables:0"] = "abc"
        };
        using var fabrica = new FabricaApi(configuracion: configuracion);
        await fabrica.EnviarAsync(HttpMethod.Get, "/api/salud", null);
        var encabezados = new Dictionary<string, string> { ["X-Forwarded-For"] = "203.0.113.7" };
        var respuestas = new List<RespuestaApi>();

        foreach (var id in new[] { "EST-001", "EST-002", "EST-003", "EST-004" })
        {
            respuestas.Add(await fabrica.PreguntarAsync("estudiante", id, Pregunta, "10.0.0.5", encabezados));
        }

        Assert.All(respuestas, r => Assert.Equal(200, r.Estado));
        Assert.All(fabrica.Auditoria.Eventos, e => Assert.Equal("10.0.0.5", e.IpOrigen));
        Assert.DoesNotContain(fabrica.Logs.Mensajes, m => m.Contains(ResultadoArranque.PrefijoInvalida, StringComparison.Ordinal));
        Assert.DoesNotContain(fabrica.Logs.Mensajes, m => m.Contains(ResultadoArranque.PrefijoIgnorada, StringComparison.Ordinal));
    }
}
