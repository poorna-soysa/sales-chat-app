namespace ChatAppSales.Entities;

public sealed record MonthlyYoYRow(int Month, decimal ThisYear, decimal LastYear, decimal? YoY);
public sealed record YoYTotals(decimal LastYear, decimal ThisYear, decimal? YoY);
public sealed record YoYResponse(
    string Customer,
    int YearThis,
    int YearLast,
    IReadOnlyList<MonthlyYoYRow> Rows,
    string DataAsOf
);
public sealed record YoYTotalsResponse(
    string Customer,
    int YearThis,
    int YearLast,
    YoYTotals Totals,
    string DataAsOf
);

public sealed record YoYMonthlyToolArgs(string Customer, int? Year = null);
public sealed record YoYTotalsToolArgs(string Customer, int? Year = null);

public sealed record CustomerListRow(
    int CustomerId,
    string CustomerName,
    string Country,
    string? KAM,
    string? AAM
);
public sealed record BudgetVarianceResponse(
    string? Customer, string? Country, int Year, int? Quarter,
    decimal Actual, decimal Budget, decimal VarianceValue, decimal? VariancePct,
    string DataAsOf);

public sealed record ForecastAccuracyResponse(
    string? Customer, string? Country, int Year, int? Quarter,
    decimal MAPE, decimal Bias,
    string DataAsOf);