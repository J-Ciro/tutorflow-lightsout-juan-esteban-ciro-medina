using TutorFlow.Core.Asistente;
using TutorFlow.Core.Autorizacion;
using static TutorFlow.Core.Tests.Asistente.EscenarioAsistente;
using static TutorFlow.Core.Tests.Asistente.SeccionesDeReferencia;

namespace TutorFlow.Core.Tests.Asistente;

public sealed class EncabezadoDeColumnasTests
{
    private const string PreguntaPerdidas = "¿Qué estudiantes de mi programa tienen alguna asignatura perdida?";
    private static readonly Usuario Coord02 = new(Rol.Coordinador, "COORD-02");

    private static EscenarioAsistente ConTabla(string texto) =>
        Con(Fijo(texto), Indice(Articulo5, Articulo6, Articulo20));

    [Fact]
    [Trait("CA", "CA-17")]
    [Trait("ESC", "ESC-199")]
    [Trait("D", "D-62")]
    [Trait("SEG", "SEG-10f")]
    public async Task ESC_199h1_Una_tabla_cuyo_unico_renglon_afirma_la_perdida_de_Mateo_es_un_bloque_sin_contexto_y_falla()
    {
        var escenario = ConTabla("| Mateo Vargas perdió Estadística |\n| --- |");

        var resultado = await escenario.PreguntarAsync(Coord02, PreguntaPerdidas);

        SalidasVerificadas.AfirmarEstadoNoVerificado(resultado, escenario, usoDatos: true);
    }

    [Fact]
    [Trait("CA", "CA-17")]
    [Trait("ESC", "ESC-199")]
    [Trait("D", "D-62")]
    [Trait("SEG", "SEG-10f")]
    public async Task ESC_199h2_La_misma_tabla_con_la_perdida_real_de_Valentina_se_entrega_sin_cambios()
    {
        const string texto = "| Valentina Castro perdió Microeconomía |\n| --- |";
        var escenario = ConTabla(texto);

        var resultado = await escenario.PreguntarAsync(Coord02, PreguntaPerdidas);

        SalidasVerificadas.AfirmarRespondida(resultado, escenario, texto);
    }
}
