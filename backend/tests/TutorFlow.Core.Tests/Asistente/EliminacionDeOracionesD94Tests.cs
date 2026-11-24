using TutorFlow.Core.Asistente;
using TutorFlow.Core.Autorizacion;
using TutorFlow.Core.Conocimiento;
using static TutorFlow.Core.Tests.Asistente.EscenarioAsistente;
using static TutorFlow.Core.Tests.Asistente.SalidasVerificadas;
using static TutorFlow.Core.Tests.Asistente.SeccionesDeReferencia;

namespace TutorFlow.Core.Tests.Asistente;

public sealed class EliminacionDeOracionesD94Tests
{
    private const string PreguntaQ10 = "¿Cuántas faltas tengo en Bases de Datos?";
    private const string PreguntaSinDesglose = "¿Cómo voy en Bases de Datos?";
    private const string RestoSuelto = "máximo sin perder: 7).";
    private const string LineaDeBases = "Bases de Datos: faltas 2 de 48 clases programadas (faltas restantes: 5; máximo sin perder: 7). Si superas 7 inasistencias pierdes la asignatura por inasistencia.";
    private const string OracionConArchivo = "Las inasistencias se rigen por reglamento_academico_2025.md · Artículo 11.";
    private static readonly Usuario Est001 = new(Rol.Estudiante, "EST-001");

    private static IndiceFijo IndiceDeFaltas() => Indice(Articulo11, Articulo12);

    private static string[] Lineas(string respuesta) =>
        respuesta.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n', StringSplitOptions.RemoveEmptyEntries);

    private static int Apariciones(string texto, string fragmento) =>
        texto.Split(fragmento, StringSplitOptions.None).Length - 1;

    [Fact]
    [Trait("CA", "CA-27")]
    [Trait("ESC", "ESC-154")]
    [Trait("D", "D-94")]
    public async Task ESC_154_La_copia_de_la_linea_se_elimina_entera_y_no_deja_el_resto_tras_el_punto_y_coma()
    {
        var escenario = Con(Fijo(LineaDeBases), IndiceDeFaltas());

        var resultado = await escenario.PreguntarAsync(Est001, PreguntaQ10);

        var respuesta = resultado.Respuesta!.Respuesta;
        Assert.Single(escenario.Llm.Solicitudes);
        Assert.Equal(["Datos de Laura Gómez (EST-001)", LineaDeBases, "Total: 2 faltas"], Lineas(respuesta));
        Assert.Equal(1, Apariciones(respuesta, RestoSuelto));
    }

    [Fact]
    [Trait("CA", "CA-27")]
    [Trait("ESC", "ESC-189")]
    [Trait("D", "D-94")]
    public async Task ESC_189_o1_El_nombre_del_archivo_con_el_anio_no_es_cifra_de_faltas_y_la_oracion_se_conserva()
    {
        var escenario = Con(Fijo(OracionConArchivo), IndiceDeFaltas());

        var resultado = await escenario.PreguntarAsync(Est001, PreguntaQ10);

        Assert.Single(escenario.Llm.Solicitudes);
        Assert.Equal([OracionConArchivo, "Datos de Laura Gómez (EST-001)", LineaDeBases, "Total: 2 faltas"], Lineas(resultado.Respuesta!.Respuesta));
        Assert.Contains(new Cita(Reglamento, "Artículo 11"), resultado.Respuesta.Citas);
        Assert.Contains(new Cita("Datos del estudiante", "Asistencia"), resultado.Respuesta.Citas);
    }

    [Fact]
    [Trait("CA", "CA-26")]
    [Trait("ESC", "ESC-179")]
    [Trait("D", "D-94")]
    public async Task ESC_179_ab_El_anio_pegado_al_guion_bajo_del_archivo_no_bloquea_si_la_cifra_de_faltas_cuadra()
    {
        await ClasesVerificadoras.AfirmarPasaAsync("EST-001", "Llevas 2 inasistencias en Bases de Datos.", "EST-001");
    }

    [Fact]
    [Trait("CA", "CA-26")]
    [Trait("ESC", "ESC-179")]
    [Trait("D", "D-94")]
    public async Task ESC_179_ab_El_2025_suelto_como_cifra_de_faltas_da_estado_no_verificable()
    {
        await ClasesVerificadoras.AfirmarFallaAsync("EST-001", "Llevas 2025 faltas.", "EST-001");
    }
}
