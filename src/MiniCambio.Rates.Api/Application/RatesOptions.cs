using System.ComponentModel.DataAnnotations;

namespace MiniCambio.Rates.Api.Application;

/// <summary>Seção <c>Rates</c> da configuração (ex.: <c>Rates__SpreadPercent</c>).</summary>
public sealed class RatesOptions
{
    public const string SectionName = "Rates";

    /// <summary>Spread em pontos percentuais aplicado sobre a taxa média (0.5 = 0,5%).</summary>
    [Range(typeof(decimal), "0", "50")]
    public decimal SpreadPercent { get; set; } = 0.5m;

    /// <summary>Validade de uma cotação, em segundos.</summary>
    [Range(1, 86400)]
    public int QuoteTtlSeconds { get; set; } = 60;
}
