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