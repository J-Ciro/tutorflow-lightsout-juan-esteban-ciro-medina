namespace TutorFlow.Api;

public sealed record CuerpoLeido(bool ExcedeElLimite, byte[] Bytes);

public static class LectorCuerpo
{
    private const int TamanoDeBloque = 4096;

    public static async Task<CuerpoLeido> LeerAsync(Stream cuerpo, int maximoBytes, CancellationToken cancellationToken)
    {
        using var acumulado = new MemoryStream();
        var bloque = new byte[TamanoDeBloque];
        int leidos;
        while ((leidos = await cuerpo.ReadAsync(bloque, cancellationToken)) > 0)
        {
            acumulado.Write(bloque, 0, leidos);
            if (acumulado.Length > maximoBytes)
            {
                return new CuerpoLeido(true, []);
            }
        }

        return new CuerpoLeido(false, acumulado.ToArray());
    }
}
