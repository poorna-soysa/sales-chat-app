using Microsoft.EntityFrameworkCore;

namespace ChatAppSales.Entities;




[Keyless]
public sealed class YearRevenueRow
{
    public int Year { get; set; }
    public decimal Revenue { get; set; }
}

public sealed record BudgetVarianceResponse(
    string? Customer, string? Country, int Year, int? Quarter,
    decimal Actual, decimal Budget, decimal VarianceValue, decimal? VariancePct,
    string DataAsOf);

public sealed record ForecastAccuracyResponse(
    string? Customer, string? Country, int Year, int? Quarter,
    decimal MAPE, decimal Bias,
    string DataAsOf);


[Keyless]
public sealed class YearMonthRevenueRow
{
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal Revenue { get; set; }
}

[Keyless]
public sealed class TopMaterialRow
{
    public string MaterialCode { get; set; } = "";
    public string? Group1 { get; set; }
    public decimal Revenue { get; set; }
    public decimal Units { get; set; }
    public decimal AvgPrice { get; set; }
}

[Keyless]
public sealed class BudgetVarianceRow
{
    public decimal Actual { get; set; }
    public decimal Budget { get; set; }
    public decimal VarianceValue { get; set; }
    public decimal? VariancePct { get; set; }
}

[Keyless]
public sealed class ForecastAccuracyRow
{
    public decimal MAPE { get; set; }
    public decimal Bias { get; set; }
}

[Keyless]
public sealed class TopCustomerRow
{
    public string CustomerName { get; set; } = "";
    public string Country { get; set; } = "";
    public decimal Revenue { get; set; }
    public decimal Units { get; set; }
    public decimal ContributionPct { get; set; } // share of total revenue
}
