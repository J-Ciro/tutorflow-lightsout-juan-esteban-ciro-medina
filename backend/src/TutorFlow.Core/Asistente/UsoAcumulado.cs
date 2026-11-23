namespace TutorFlow.Core.Asistente;

internal sealed class UsoAcumulado
{
    private UsoTokens _valor = new(0, 0);

    public UsoTokens Valor => _valor;

    public void Sumar(UsoTokens uso) =>
        _valor = new UsoTokens(_valor.Entrada + uso.Entrada, _valor.Salida + uso.Salida);
}
