namespace TutorFlow.Core.Autorizacion;

internal sealed class FrasesDelTexto(string textoNormalizado)
{
    private readonly IReadOnlyList<string> _palabras = TokenizadorTexto.Valores(textoNormalizado);

    public bool Contiene(IEnumerable<string> frases) =>
        frases.Any(frase => Posiciones(frase).Any());

    public bool ContieneSeguidaDe(IEnumerable<string> frases, IReadOnlyCollection<string> siguientes) =>
        frases.Any(frase => Posiciones(frase).Any(posicion => EmpiezaAlgunaEn(posicion + Palabras(frase).Length, siguientes)));

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
