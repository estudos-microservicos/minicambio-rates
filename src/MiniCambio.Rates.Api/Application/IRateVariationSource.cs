namespace MiniCambio.Rates.Api.Application;

/// <summary>
/// Fonte da variação aleatória da taxa "ao vivo". Fica atrás de uma interface para que os testes
/// possam usar um valor fixo.
/// </summary>
public interface IRateVariationSource
{
    /// <summary>Retorna uma variação entre -0.01 (-1%) e +0.01 (+1%).</summary>
    decimal NextVariation();
}
