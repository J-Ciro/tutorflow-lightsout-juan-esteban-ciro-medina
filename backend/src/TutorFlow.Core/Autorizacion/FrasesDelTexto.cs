namespace TutorFlow.Core.Autorizacion;

internal readonly record struct OcurrenciaDeFrase(int Inicio, int Fin);

internal sealed class FrasesDelTexto(string textoNormalizado)
{
    private readonly IReadOnlyList<string> _palabras = TokenizadorTexto.Valores(textoNormalizado);

    public bool Contiene(IEnumerable<string> frases) =>
        frases.Any(frase => Posiciones(frase).Any());

    public bool ContieneRaiz(string raiz) =>
        _palabras.Any(palabra => palabra.StartsWith(raiz, StringComparison.Ordinal));

    public bool ContieneSeguidaDe(IEnumerable<string> frases, IReadOnlyCollection<string> siguientes) =>
        frases.Any(frase => Posiciones(frase).Any(posicion => EmpiezaAlgunaEn(posicion + Palabras(frase).Length, siguientes)));

    public IReadOnlyList<OcurrenciaDeFrase> Ocurrencias(IEnumerable<string> frases) =>
        [.. frases.SelectMany(frase => Posiciones(frase).Select(posicion => new OcurrenciaDeFrase(posicion, posicion + Palabras(frase).Length)))];

    public string PalabraAntesDe(OcurrenciaDeFrase ocurrencia) =>
        Palabra(ocurrencia.Inicio - 1);

    public bool SeguidaDeRellenoY(IEnumerable<string> frases, IReadOnlyCollection<string> rellenos, int maximoDeRelleno, IReadOnlyCollection<string> destinos) =>
        Ocurrencias(frases).Any(ocurrencia => LlegaAlDestino(ocurrencia.Fin, rellenos, maximoDeRelleno, destinos));

    private bool LlegaAlDestino(int indice, IReadOnlyCollection<string> rellenos, int maximoDeRelleno, IReadOnlyCollection<string> destinos) =>
        Enumerable.Range(0, maximoDeRelleno + 1).Any(relleno =>
            Enumerable.Range(0, relleno).All(salto => rellenos.Contains(Palabra(indice + salto)))
            && EmpiezaAlgunaEn(indice + relleno, destinos));

    public IReadOnlyList<string> PalabrasDesde(int indice) =>
        [.. _palabras.Skip(Math.Max(indice, 0))];

    private string Palabra(int indice) =>
        indice >= 0 && indice < _palabras.Count ? _palabras[indice] : string.Empty;

    private static string[] Palabras(string frase) => frase.Split(' ');

    private bool EmpiezaAlgunaEn(int indice, IEnumerable<string> frases) =>
        frases.Any(frase => EmpiezaEn(indice, frase));

    private IEnumerable<int> Posiciones(string frase) =>
        Enumerable.Range(0, _palabras.Count).Where(indice => EmpiezaEn(indice, frase));

    private bool EmpiezaEn(int indice, string frase)
    {
        var palabras = Palabras(frase);
        return indice + palabras.Length <= _palabras.Count
            && palabras.Select((palabra, desplazamiento) => _palabras[indice + desplazamiento] == palabra).All(coincide => coincide);
    }
}
