using System.Text.Encodings.Web;
using System.Text.Json;
using TutorFlow.Api.Tests.Dobles;
using TutorFlow.Core.Asistente;
using TutorFlow.Core.Autorizacion;

namespace TutorFlow.Api.Tests.Operacion;

public sealed class AuditoriaApiTests
{
    private const string IpDeLaPrueba = "10.1.2.3";
    private static readonly JsonSerializerOptions SinEscapes = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
    private static readonly string[] ValoresDeNotas = ["4.39", "4.5", "4.8", "4.1", "4.0", "3.8", "2.6", "0.0"];

    public static TheoryData<string, string, string, ResultadoAuditoria, string> UnEventoPorPregunta => new()
    {
        { "estudiante", "EST-001", "¿Cuál es mi promedio ponderado del periodo?", ResultadoAuditoria.Respondida, "EST-001" },
        { "estudiante", "EST-001", "¿Qué dice el reglamento sobre parqueaderos?", ResultadoAuditoria.SinInformacion, string.Empty },
        { "estudiante", "EST-001", "¿Cuál es la nota de Bases de Datos de Andrés Martínez?", ResultadoAuditoria.Denegada, string.Empty },
        { "coordinador", "COORD-02", "¿Qué estudiantes de mi programa tienen materias perdidas?", ResultadoAuditoria.Respondida, "EST-004,EST-005,EST-006" }
    };

    private static ClienteLlmDoble LlmPara(ResultadoAuditoria resultado) => resultado switch
    {
        ResultadoAuditoria.SinInformacion => ClienteLlmDoble.Texto(Textos.SinInformacion),
        ResultadoAuditoria.Respondida => ClienteLlmDoble.ConsultaDatos(redactar: _ => "Resumen de tus datos académicos."),
        _ => ClienteLlmDoble.Texto("Respuesta de prueba.")
    };

    [Theory]
    [MemberData(nameof(UnEventoPorPregunta))]
    [Trait("CA", "CA-08")]
    [Trait("ESC", "ESC-113")]
    public async Task Cada_pregunta_emite_exactamente_un_evento_con_resultado_y_consultados(string rol, string id, string pregunta, ResultadoAuditoria resultado, string consultados)
    {
        using var fabrica = new FabricaApi(llm: LlmPara(resultado));

        await fabrica.PreguntarAsync(rol, id, pregunta, IpDeLaPrueba);

        var evento = Assert.Single(fabrica.Auditoria.Eventos);
        Assert.Equal(resultado, evento.Resultado);
        Assert.Equal(consultados.Length == 0 ? [] : consultados.Split(','), evento.EstudiantesConsultados.Order().ToArray());
        var serializado = JsonSerializer.Serialize(evento, SinEscapes);
        Assert.All(ValoresDeNotas, valor => Assert.DoesNotContain($"\"{valor}\"", serializado, StringComparison.Ordinal));
        Assert.DoesNotContain("Resumen de tus datos", serializado, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("CA", "CA-08")]
    [Trait("ESC", "ESC-113")]
    public async Task ESC_113_Una_pregunta_invalida_emite_un_evento_invalida()
    {
        using var fabrica = new FabricaApi();

        await fabrica.PreguntarEstudianteAsync("EST-001", "   ", IpDeLaPrueba);

        var evento = Assert.Single(fabrica.Auditoria.Eventos);
        Assert.Equal(ResultadoAuditoria.Invalida, evento.Resultado);
        Assert.Equal(IpDeLaPrueba, evento.IpOrigen);
    }

    [Fact]
    [Trait("CA", "CA-08")]
    [Trait("ESC", "ESC-113")]
    public async Task ESC_113_Un_cuerpo_demasiado_grande_emite_un_evento_invalida()
    {
        using var fabrica = new FabricaApi();
        var cuerpo = $$"""{"pregunta":"hola","usuario":{"rol":"estudiante","id":"EST-001"},"relleno":"{{new string('x', 17 * 1024)}}"}""";

        await fabrica.EnviarAsync(HttpMethod.Post, "/api/asistente/preguntar", cuerpo, IpDeLaPrueba);

        Assert.Equal(ResultadoAuditoria.Invalida, Assert.Single(fabrica.Auditoria.Eventos).Resultado);
    }

    [Fact]
    [Trait("CA", "CA-08")]
    [Trait("ESC", "ESC-114")]
    public async Task ESC_114_El_evento_registra_ids_y_recursos_pero_no_valores_de_notas()
    {
        using var fabrica = new FabricaApi(llm: ClienteLlmDoble.ConsultaDatos());

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", "¿Cuál es mi promedio ponderado del periodo?", IpDeLaPrueba);

        Assert.Contains("4.39", respuesta.Respuesta, StringComparison.Ordinal);
        var evento = Assert.Single(fabrica.Auditoria.Eventos);
        Assert.Equal(["EST-001"], evento.EstudiantesConsultados.ToArray());
        Assert.Contains(RecursoConsultado.Notas, evento.RecursosConsultados);
        var serializado = JsonSerializer.Serialize(evento, SinEscapes);
        Assert.All(ValoresDeNotas, valor => Assert.DoesNotContain($"\"{valor}\"", serializado, StringComparison.Ordinal));
        Assert.DoesNotContain("Cálculo Integral", serializado, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("CA", "CA-08")]
    [Trait("ESC", "ESC-118")]
    public async Task ESC_118_Los_terceros_se_enmascaran_en_la_pregunta_auditada_y_la_ip_esta_presente()
    {
        using var fabrica = new FabricaApi();

        await fabrica.PreguntarEstudianteAsync("EST-001", "¿Cuál es la nota de Bases de Datos de Andrés Martínez?", IpDeLaPrueba);

        var evento = Assert.Single(fabrica.Auditoria.Eventos);
        Assert.Contains("[TERCERO]", evento.Pregunta, StringComparison.Ordinal);
        Assert.DoesNotContain("Andrés Martínez", evento.Pregunta, StringComparison.Ordinal);
        Assert.DoesNotContain("Martinez", evento.Pregunta, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(IpDeLaPrueba, evento.IpOrigen);
    }

    [Fact]
    [Trait("CA", "CA-08")]
    [Trait("ESC", "ESC-118")]
    public async Task Los_ids_de_terceros_tambien_se_enmascaran_en_la_pregunta_auditada()
    {
        using var fabrica = new FabricaApi();

        await fabrica.PreguntarEstudianteAsync("EST-001", "¿Cuáles son las notas de EST-002?", IpDeLaPrueba);

        var evento = Assert.Single(fabrica.Auditoria.Eventos);
        Assert.Contains("[TERCERO]", evento.Pregunta, StringComparison.Ordinal);
        Assert.DoesNotContain("EST-002", evento.Pregunta, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("CA", "CA-08")]
    [Trait("SEG", "11.3")]
    public async Task El_evento_trae_los_campos_de_la_spec_con_la_hora_del_reloj_y_un_correlation_id()
    {
        using var fabrica = new FabricaApi(llm: ClienteLlmDoble.ConsultaDatos(redactar: _ => "Listo."));

        await fabrica.PreguntarEstudianteAsync("EST-001", "¿Cuál es mi promedio?", IpDeLaPrueba);

        var evento = Assert.Single(fabrica.Auditoria.Eventos);
        Assert.Equal(fabrica.Reloj.AhoraUtc, evento.Timestamp);
        Assert.False(string.IsNullOrWhiteSpace(evento.CorrelationId));
        Assert.Equal(Rol.Estudiante, evento.Rol);
        Assert.Equal("EST-001", evento.UsuarioId);
        Assert.Equal(IpDeLaPrueba, evento.IpOrigen);
        Assert.True(evento.Tokens > 0);
        Assert.True(evento.DuracionMs >= 0);
    }

    [Fact]
    [Trait("CA", "CA-08")]
    [Trait("ESC", "ESC-083")]
    public async Task ESC_083_El_evento_no_contiene_caracteres_de_control_de_la_pregunta()
    {
        using var fabrica = new FabricaApi();

        await fabrica.PreguntarEstudianteAsync("EST-001", "hola\n{\"resultado\":\"respondida\",\"usuario\":\"COORD-01\"}\r\ncierre", IpDeLaPrueba);

        var evento = Assert.Single(fabrica.Auditoria.Eventos);
        Assert.DoesNotContain(evento.Pregunta, char.IsControl);
        Assert.Equal("EST-001", evento.UsuarioId);
    }

    [Fact]
    [Trait("CA", "CA-08")]
    [Trait("SEG", "11.3")]
    public async Task La_pregunta_auditada_se_trunca_a_mil_caracteres()
    {
        using var fabrica = new FabricaApi();

        await fabrica.PreguntarEstudianteAsync("EST-001", new string('a', 1000), IpDeLaPrueba);

        Assert.True(Assert.Single(fabrica.Auditoria.Eventos).Pregunta.Length <= 1000);
    }

    [Fact]
    [Trait("CA", "CA-08")]
    [Trait("ESC", "ESC-088")]
    public async Task Una_fuga_bloqueada_emite_un_solo_evento_fuga_bloqueada()
    {
        using var fabrica = new FabricaApi(llm: ClienteLlmDoble.Texto("Andres  Martinez sacó 2.6 en Cálculo"));

        await fabrica.PreguntarEstudianteAsync("EST-001", "¿Cuál es mi promedio?", IpDeLaPrueba);

        Assert.Equal(ResultadoAuditoria.FugaBloqueada, Assert.Single(fabrica.Auditoria.Eventos).Resultado);
    }

    [Fact]
    [Trait("CA", "CA-08")]
    [Trait("ESC", "ESC-108")]
    public async Task ESC_108_Una_api_caida_emite_un_solo_evento_error()
    {
        using var fabrica = new FabricaApi(api: ApiAcademicaDoble.Caida());

        await fabrica.PreguntarEstudianteAsync("EST-001", "¿Cuál es mi promedio?", IpDeLaPrueba);

        Assert.Equal(ResultadoAuditoria.Error, Assert.Single(fabrica.Auditoria.Eventos).Resultado);
    }
}
