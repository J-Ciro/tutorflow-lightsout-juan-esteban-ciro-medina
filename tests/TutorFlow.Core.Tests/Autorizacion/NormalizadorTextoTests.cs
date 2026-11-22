using TutorFlow.Core.Autorizacion;

namespace TutorFlow.Core.Tests.Autorizacion;

public sealed class NormalizadorTextoTests
{
    [Theory]
    [InlineData("Andrés Martínez", "andres martinez")]
    [InlineData("ANDRES MARTINEZ", "andres martinez")]
    [InlineData("Martínez, Andrés", "martinez andres")]
    [InlineData("Andres    Martinez", "andres martinez")]
    [InlineData("  hola  ", "hola")]
    [InlineData("¿Cuál es mi promedio?", "cual es mi promedio")]
    [InlineData("Andrés​ Martínez", "andres martinez")]
    [InlineData("Mar​tínez", "martinez")]
    [InlineData("Andrés﻿ Martínez", "andres martinez")]
    [InlineData("Ａndres Martínez", "andres martinez")]
    [Trait("CA", "CA-04")]
    [Trait("SEG", "SEG-04a")]
    public void Normaliza_a_minusculas_sin_tildes_sin_ancho_cero_y_con_espacios_colapsados(string entrada, string esperado)
    {
        Assert.Equal(esperado, NormalizadorTexto.Normalizar(entrada));
    }

    [Theory]
    [InlineData("EST-002", "EST‑002")]
    [InlineData("EST-002", "EST‐002")]
    [InlineData("EST-002", "EST–002")]
    [InlineData("EST-002", "EST−002")]
    [InlineData("EST-002", "EST-００２")]
    [InlineData("EST-002", "est-002")]
    [InlineData("EST-002", "EST​-002")]
    [Trait("ESC", "ESC-130")]
    [Trait("SEG", "SEG-04a")]
    public void Variantes_del_mismo_id_normalizan_igual(string canonico, string variante)
    {
        Assert.Equal(NormalizadorTexto.Normalizar(canonico), NormalizadorTexto.Normalizar(variante));
    }

    [Theory]
    [InlineData("¿Cuáles son las notas de EST_002?")]
    [InlineData("Martínez Andrés, ¡notas!")]
    [InlineData("hola\n\t mundo")]
    [Trait("SEG", "SEG-04a")]
    public void Normalizar_es_idempotente(string entrada)
    {
        var una = NormalizadorTexto.Normalizar(entrada);

        Assert.Equal(una, NormalizadorTexto.Normalizar(una));
    }

    [Fact]
    [Trait("SEG", "SEG-04a")]
    public void Texto_vacio_normaliza_a_vacio()
    {
        Assert.Equal(string.Empty, NormalizadorTexto.Normalizar(string.Empty));
    }
}
