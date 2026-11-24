using System.Text;

namespace TutorFlow.Api.Tests.Dobles;

public sealed class EscritorSincronizado : TextWriter
{
    private readonly StringBuilder _texto = new();
    private readonly Lock _candado = new();

    public override Encoding Encoding => Encoding.UTF8;

    public override void Write(char value)
    {
        lock (_candado)
        {
            _texto.Append(value);
        }
    }

    public override void Write(string? value)
    {
        lock (_candado)
        {
            _texto.Append(value);
        }
    }

    public override string ToString()
    {
        lock (_candado)
        {
            return _texto.ToString();
        }
    }
}
