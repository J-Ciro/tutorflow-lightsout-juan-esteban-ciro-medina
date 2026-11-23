using System.Globalization;
using System.Text;

namespace TutorFlow.Core.Autorizacion;

internal static class PlegadoCompatibilidad
{
    private const int LetrasPorAlfabeto = 26;
    private const int LetrasPorEstilo = 52;
    private const int DigitosPorEstilo = 10;
    private const int InicioAlfabetosMatematicos = 0x1D400;
    private const int FinAlfabetosMatematicos = 0x1D6A3;
    private const int InicioDigitosMatematicos = 0x1D7CE;
    private const int FinDigitosMatematicos = 0x1D7FF;
    private const int InicioCirculadosDigitos = 0x2460;
    private const int FinCirculadosDigitos = 0x2468;
    private const int InicioCirculadasMayusculas = 0x24B6;
    private const int FinCirculadasMayusculas = 0x24CF;
    private const int InicioCirculadasMinusculas = 0x24D0;
    private const int FinCirculadasMinusculas = 0x24E9;
    private const int InicioSubindices = 0x2080;
    private const int FinSubindices = 0x2089;
    private const int InicioSuperindicesCuatroANueve = 0x2074;
    private const int FinSuperindicesCuatroANueve = 0x2079;
    private const int CuatroComoDigito = 4;

    private static readonly RangoPlegado[] Rangos =
    [
        new(InicioAlfabetosMatematicos, FinAlfabetosMatematicos, LetraMatematica),
        new(InicioDigitosMatematicos, FinDigitosMatematicos, DigitoMatematico),
        new(InicioCirculadosDigitos, FinCirculadosDigitos, valor => Digito(valor - InicioCirculadosDigitos + 1)),
        new(InicioCirculadasMayusculas, FinCirculadasMayusculas, valor => Letra('A', valor - InicioCirculadasMayusculas)),
        new(InicioCirculadasMinusculas, FinCirculadasMinusculas, valor => Letra('a', valor - InicioCirculadasMinusculas)),
        new(InicioSubindices, FinSubindices, valor => Digito(valor - InicioSubindices)),
        new(InicioSuperindicesCuatroANueve, FinSuperindicesCuatroANueve, valor => Digito(valor - InicioSuperindicesCuatroANueve + CuatroComoDigito))
    ];

    private static readonly Dictionary<int, string> Equivalencias = new()
    {
        [0x00AA] = "a",
        [0x00BA] = "o",
        [0x00B2] = "2",
        [0x00B3] = "3",
        [0x00B9] = "1",
        [0x0132] = "IJ",
        [0x0133] = "ij",
        [0x017F] = "s",
        [0x2070] = "0",
        [0x2071] = "i",
        [0x207F] = "n",
        [0x24EA] = "0",
        [0xFB00] = "ff",
        [0xFB01] = "fi",
        [0xFB02] = "fl",
        [0xFB03] = "ffi",
        [0xFB04] = "ffl",
        [0xFB05] = "st",
        [0xFB06] = "st"
    };

    public static string De(Rune rune) =>
        Rangos.FirstOrDefault(rango => rango.Contiene(rune.Value))?.Plegar(rune.Value)
        ?? Equivalencias.GetValueOrDefault(rune.Value)
        ?? rune.ToString();

    private static string LetraMatematica(int valor)
    {
        var posicion = (valor - InicioAlfabetosMatematicos) % LetrasPorEstilo;
        return posicion < LetrasPorAlfabeto ? Letra('A', posicion) : Letra('a', posicion - LetrasPorAlfabeto);
    }

    private static string DigitoMatematico(int valor) =>
        Digito((valor - InicioDigitosMatematicos) % DigitosPorEstilo);

    private static string Letra(char inicio, int desplazamiento) =>
        ((char)(inicio + desplazamiento)).ToString(CultureInfo.InvariantCulture);

    private static string Digito(int valor) =>
        valor.ToString(CultureInfo.InvariantCulture);

    private sealed record RangoPlegado(int Inicio, int Fin, Func<int, string> Plegar)
    {
        public bool Contiene(int valor) => valor >= Inicio && valor <= Fin;
    }
}
