namespace TutorFlow.Core.Tests.Asistente;

public sealed class EncabezadoDeColumnasTests
{

    [Fact]
    [Trait("CA", "CA-17")]
    [Trait("ESC", "ESC-199")]
    [Trait("D", "D-62")]
    [Trait("SEG", "SEG-10f")]
    public async Task ESC_199h1_Una_tabla_cuyo_unico_renglon_afirma_la_perdida_de_Mateo_es_un_bloque_sin_contexto_y_falla()
    {
        await ClasesVerificadoras.AfirmarFallaAsync("COORD-02", "| Mateo Vargas perdió Estadística |\n| --- |", "EST-004", "EST-005", "EST-006");
    }

    [Fact]
    [Trait("CA", "CA-17")]
    [Trait("ESC", "ESC-199")]
    [Trait("D", "D-62")]
    [Trait("SEG", "SEG-10f")]
    public async Task ESC_199h2_La_misma_tabla_con_la_perdida_real_de_Valentina_se_entrega_sin_cambios()
    {
        await ClasesVerificadoras.AfirmarPasaAsync("COORD-02", "| Valentina Castro perdió Microeconomía |\n| --- |", "EST-004", "EST-005", "EST-006");
    }
}
