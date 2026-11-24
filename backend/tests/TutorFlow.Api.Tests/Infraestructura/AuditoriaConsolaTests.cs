using System.Text.Json;
using TutorFlow.Core.Asistente;
using TutorFlow.Core.Autorizacion;
using TutorFlow.Core.Conocimiento;
using TutorFlow.Infrastructure.Auditoria;
using TutorFlow.Pruebas.Soporte;

namespace TutorFlow.Api.Tests.Infraestructura;

[Collection("Consola")]
public sealed class AuditoriaConsolaTests
{
    private static readonly DateTimeOffset Instante = new(2026, 11, 22, 12, 0, 0, TimeSpan.Zero);

    public static TheoryData<ResultadoAuditoria, string> Resultados => new()
    {
        { ResultadoAuditoria.Respondida, "respondida" },
        { ResultadoAuditoria.SinInformacion, "sin_informacion" },
        { ResultadoAuditoria.Denegada, "denegada" },
        { ResultadoAuditoria.Invalida, "invalida" },
        { ResultadoAuditoria.FugaBloqueada, "fuga_bloqueada" },
        { ResultadoAuditoria.NumeroNoVerificado, "numero_no_verificado" },
        { ResultadoAuditoria.LimiteHerramientas, "limite_herramientas" },
        { ResultadoAuditoria.Error, "error" }
    };

    public static TheoryData<MotivoDenegacion, string> Motivos => new()
    {
        { MotivoDenegacion.IdentidadInvalida, "identidad_invalida" },
        { MotivoDenegacion.TerceroMencionado, "tercero_mencionado" },
        { MotivoDenegacion.ProgramaAjeno, "programa_ajeno" },
        { MotivoDenegacion.Agregado, "agregado" },
        { MotivoDenegacion.HerramientaIdAjeno, "herramienta_id_ajeno" }
    };

    private static EventoAuditoria Evento(string correlacion, string pregunta = "¿Cuál es mi promedio?", ResultadoAuditoria resultado = ResultadoAuditoria.Respondida, MotivoDenegacion? motivo = null) =>
        new(
            Instante,
            correlacion,
            "10.0.0.5",
            Rol.Estudiante,
            "EST-001",
            pregunta,
            resultado,
            motivo,
            ["EST-001"],
            [RecursoConsultado.DatosBasicos, RecursoConsultado.Notas, RecursoConsultado.Asistencia, RecursoConsultado.Programa],
            [new Cita("Datos del estudiante", "Notas")],
            1200,
            0.002m,
            35,
            false);

    private static string Capturar(Action accion)
    {
        var original = Console.Out;
        using var escritor = new StringWriter();
        Console.SetOut(escritor);
        try
        {
            accion();
        }
        finally
        {
            Console.SetOut(original);
        }

        return escritor.ToString();
    }

    private static JsonElement UnicaLinea(string salida, string marca)
    {
        var lineas = salida.Split('\n', StringSplitOptions.RemoveEmptyEntries).Where(l => l.Contains(marca, StringComparison.Ordinal)).ToList();
        var linea = Assert.Single(lineas);
        return JsonElement.Parse(linea);
    }

    [Fact]
    [Trait("CA", "CA-08")]
    [Trait("SEG", "11.3")]
    public void El_evento_es_una_sola_linea_json_con_los_campos_de_la_spec()
    {
        var salida = Capturar(() => new AuditoriaJsonConsola().Registrar(Evento("corr-campos-1")));

        var json = UnicaLinea(salida, "corr-campos-1");
        Assert.True(DateTimeOffset.TryParse(json.GetProperty("timestamp").GetString(), out var momento) && momento == Instante);
        Assert.Equal("corr-campos-1", json.GetProperty("correlationId").GetString());
        Assert.Equal("10.0.0.5", json.GetProperty("ipOrigen").GetString());
        Assert.Equal("estudiante", json.GetProperty("usuario").GetProperty("rol").GetString());
        Assert.Equal("EST-001", json.GetProperty("usuario").GetProperty("id").GetString());
        Assert.Equal("¿Cuál es mi promedio?", json.GetProperty("pregunta").GetString());
        Assert.Equal("respondida", json.GetProperty("resultado").GetString());
        Assert.Equal(["EST-001"], json.GetProperty("estudiantesConsultados").EnumerateArray().Select(e => e.GetString()!).ToArray());
        Assert.Equal(["datos_basicos", "notas", "asistencia", "programa"], json.GetProperty("recursosConsultados").EnumerateArray().Select(e => e.GetString()!).ToArray());
        Assert.Equal("Datos del estudiante", json.GetProperty("citas")[0].GetProperty("documento").GetString());
        Assert.Equal(1200, json.GetProperty("tokens").GetInt32());
        Assert.Equal(0.002m, json.GetProperty("costoUsd").GetDecimal());
        Assert.Equal(35, json.GetProperty("duracionMs").GetInt64());
    }

    [Theory]
    [MemberData(nameof(Resultados))]
    [Trait("CA", "CA-08")]
    public void El_resultado_se_serializa_en_snake_case_de_la_spec(ResultadoAuditoria resultado, string esperado)
    {
        var salida = Capturar(() => new AuditoriaJsonConsola().Registrar(Evento("corr-resultado", resultado: resultado)));

        Assert.Equal(esperado, UnicaLinea(salida, "corr-resultado").GetProperty("resultado").GetString());
    }

    [Fact]
    [Trait("CA", "CA-06")]
    [Trait("ESC", "ESC-175")]
    public void El_resultado_estado_no_verificado_se_serializa_en_snake_case_de_la_spec()
    {
        var resultado = ResultadosNuevosDeAuditoria.EstadoNoVerificado;

        var salida = Capturar(() => new AuditoriaJsonConsola().Registrar(Evento("corr-estado", resultado: resultado)));

        Assert.Equal("estado_no_verificado", UnicaLinea(salida, "corr-estado").GetProperty("resultado").GetString());
    }

    [Theory]
    [MemberData(nameof(Motivos))]
    [Trait("CA", "CA-08")]
    public void El_motivo_de_denegacion_se_serializa_en_snake_case_de_la_spec(MotivoDenegacion motivo, string esperado)
    {
        var salida = Capturar(() => new AuditoriaJsonConsola().Registrar(Evento("corr-motivo", resultado: ResultadoAuditoria.Denegada, motivo: motivo)));

        Assert.Equal(esperado, UnicaLinea(salida, "corr-motivo").GetProperty("motivoDenegacion").GetString());
    }

    [Fact]
    [Trait("CA", "CA-08")]
    [Trait("ESC", "ESC-083")]
    public void ESC_083_Una_pregunta_con_saltos_de_linea_sigue_siendo_una_sola_linea_json_valida()
    {
        var pregunta = "hola\n{\"resultado\":\"respondida\",\"usuario\":\"COORD-01\"}\r\n\tfin";

        var salida = Capturar(() => new AuditoriaJsonConsola().Registrar(Evento("corr-inyeccion", pregunta)));

        Assert.DoesNotContain('\n', salida.TrimEnd('\r', '\n'));
        var json = UnicaLinea(salida, "corr-inyeccion");
        Assert.Equal("respondida", json.GetProperty("resultado").GetString());
        Assert.Equal("EST-001", json.GetProperty("usuario").GetProperty("id").GetString());
    }

    [Fact]
    [Trait("CA", "CA-13")]
    [Trait("SEG", "SEG-16")]
    public void El_evento_multiples_identidades_es_una_linea_json_aparte_con_la_ip_y_los_ids()
    {
        var evento = new EventoMultiplesIdentidades(Instante, "10.0.0.5", ["EST-001", "EST-002", "EST-003", "EST-004"]);

        var salida = Capturar(() => new AuditoriaJsonConsola().RegistrarMultiplesIdentidades(evento));

        var json = UnicaLinea(salida, "multiples_identidades");
        Assert.Contains("10.0.0.5", json.GetRawText(), StringComparison.Ordinal);
        Assert.Contains("EST-004", json.GetRawText(), StringComparison.Ordinal);
        Assert.False(json.TryGetProperty("bloqueada", out _));
        Assert.DoesNotContain("bloqueada", json.GetRawText(), StringComparison.OrdinalIgnoreCase);
    }
}
