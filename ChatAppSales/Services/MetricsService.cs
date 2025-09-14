using ChatAppSales.Data;
using ChatAppSales.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ChatAppSales.Services;
public interface IMetricsService
{
    Task<YoYResponse> GetCustomerYoYByMonthAsync(string customer, int? year, int? yearLast, CancellationToken ct);
    Task<YoYTotalsResponse> GetCustomerYoYTotalsAsync(string customer, int? year, int? yearLast, CancellationToken ct);
    Task<IReadOnlyList<TopMaterialRow>> GetTopMaterialsAsync(
         string? customer, string? country, int? year, int? quarter, int topN, CancellationToken ct);
    Task<BudgetVarianceResponse> GetBudgetVsActualAsync(
    string? customer, string? country, int? year, int? quarter, CancellationToken ct);

    Task<ForecastAccuracyResponse> GetForecastAccuracyAsync(string? customer, string? country, int? year, int? quarter,
        CancellationToken ct);

    Task<IReadOnlyList<TopCustomerRow>> GetTopCustomersAsync(
    string? country, int? year, int? quarter, int topN, CancellationToken ct);
}


public sealed class MetricsService(AppDbContext db) : IMetricsService
{
    // Revenue = NetQty * NetSellingPrice; fallback to ConfirmedValue when needed
    private static decimal RevenueSelector(FactSales f) =>
        f.NetQty.HasValue && f.NetSellingPrice.HasValue
            ? f.NetQty.Value * f.NetSellingPrice.Value
            : f.ConfirmedValue ?? 0m;


    public async Task<YoYResponse> GetCustomerYoYByMonthAsync(string customer, int? yearThis, int? yearLast, CancellationToken ct)
    {
        // Anchor year: provided or latest in data
        var latestYearInData = await db.DimDates.MaxAsync(d => d.Year, ct);
        var thisYear = yearThis ?? latestYearInData;
        var lastYear = yearLast ?? thisYear - 1;

        const string sql = """
        WITH agg AS (
            SELECT 
                d.[Year]  AS [Year],
                d.[Month] AS [Month],
                SUM(COALESCE(f.NetQty * f.NetSellingPrice, f.ConfirmedValue)) AS Revenue
            FROM FactSales f
            JOIN DimDates d     ON d.[Date] = f.[Date]
            JOIN DimCustomers c ON c.CustomerId = f.CustomerId
            WHERE c.CustomerName = @customer
              AND d.[Year] IN (@thisYear, @lastYear)
            GROUP BY d.[Year], d.[Month]
        )
        SELECT [Year], [Month], Revenue
        FROM agg
        ORDER BY [Year], [Month];
        """;

        var rows = await db.Set<YearMonthRevenueRow>()
            .FromSqlRaw(sql,
                new SqlParameter("@customer", customer),
                new SqlParameter("@thisYear", thisYear),
                new SqlParameter("@lastYear", lastYear))
            .AsNoTracking()
            .ToListAsync(ct);

        // Build dictionaries for faster lookup
        var thisYearByMonth = rows.Where(r => r.Year == thisYear)
                                  .ToDictionary(r => r.Month, r => r.Revenue);
        var lastYearByMonth = rows.Where(r => r.Year == lastYear)
                                  .ToDictionary(r => r.Month, r => r.Revenue);

        // Generate 12 rows (Jan-Dec), compute YoY%
        var resultRows = Enumerable.Range(1, 12).Select(m =>
        {
            var cur = thisYearByMonth.GetValueOrDefault(m, 0m);
            var prev = lastYearByMonth.GetValueOrDefault(m, 0m);
            decimal? yoy = prev == 0 ? null : (cur - prev) / prev;
            return new MonthlyYoYRow(
                m,
                Math.Round(cur, 2),
                Math.Round(prev, 2),
                yoy is null ? null : Math.Round(yoy.Value, 4)
            );
        }).ToList();

        var dataAsOf = await db.DimDates.MaxAsync(d => d.Date, ct);

        return new YoYResponse(
            customer,
            thisYear,
            lastYear,
            resultRows,
            dataAsOf.ToString("yyyy-MM-dd")
        );
    }

    public async Task<YoYTotalsResponse> GetCustomerYoYTotalsAsync(string customer, int? yearThis, int? yearLast, CancellationToken ct)
    {
        // anchor year to latest available in data when not provided
        var latestYearInData = await db.DimDates.MaxAsync(d => d.Year, ct);
        var thisYear = yearThis ?? latestYearInData;
        var lastYear = yearLast ?? thisYear - 1;

        const string sql = """
            WITH agg AS (
                SELECT d.[Year] AS [Year],
                       SUM(COALESCE(f.NetQty * f.NetSellingPrice, f.ConfirmedValue)) AS Revenue
                FROM FactSales f
                JOIN DimDates d     ON d.[Date] = f.[Date]
                JOIN DimCustomers c ON c.CustomerId = f.CustomerId
                WHERE c.CustomerName = @customer
                  AND d.[Year] IN (@thisYear, @lastYear)
                GROUP BY d.[Year]
            )
            SELECT [Year], Revenue FROM agg;
            """;
        try
        {
            var rows = await db.Set<YearRevenueRow>()
                .FromSqlRaw(sql,
                    new SqlParameter("@customer", customer),
                    new SqlParameter("@thisYear", thisYear),
                    new SqlParameter("@lastYear", lastYear))
                .AsNoTracking()
                .ToListAsync(ct);

            var revThis = rows.FirstOrDefault(r => r.Year == thisYear)?.Revenue ?? 0m;
            var revLast = rows.FirstOrDefault(r => r.Year == lastYear)?.Revenue ?? 0m;
            decimal? yoy = revLast == 0 ? null : (revThis - revLast) / revLast;

            var dataAsOf = await db.DimDates.MaxAsync(d => d.Date, ct);

            return new YoYTotalsResponse(
                customer,
                thisYear,
                lastYear,
                new YoYTotals(Math.Round(revLast, 2), Math.Round(revThis, 2), yoy is null ? null : Math.Round(yoy.Value, 4)),
                dataAsOf.ToString("yyyy-MM-dd")
            );
        }
        catch (Exception ex)
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<TopMaterialRow>> GetTopMaterialsAsync(
        string? customer, string? country, int? year, int? quarter, int topN, CancellationToken ct)
    {
        var latestYear = await db.DimDates.MaxAsync(d => d.Year, ct);
        var thisYear = year ?? latestYear;

        const string sql = """
        SELECT TOP (@topN)
            m.MaterialCode,
            m.Group1,
            SUM(COALESCE(f.NetQty * f.NetSellingPrice, f.ConfirmedValue)) AS Revenue,
            SUM(COALESCE(f.NetQty, f.ConfirmedQty)) AS Units,
            CASE WHEN SUM(COALESCE(f.NetQty, f.ConfirmedQty)) = 0 THEN 0
                 ELSE SUM(COALESCE(f.NetQty * f.NetSellingPrice, f.ConfirmedValue)) /
                      SUM(COALESCE(f.NetQty, f.ConfirmedQty)) END AS AvgPrice
        FROM FactSales f
        JOIN DimDates d ON d.[Date] = f.[Date]
        JOIN DimMaterials m ON m.MaterialId = f.MaterialId
        JOIN DimCustomers c ON c.CustomerId = f.CustomerId
        WHERE d.[Year] = @year
          AND (@customer IS NULL OR c.CustomerName = @customer)
          AND (@country IS NULL OR c.Country = @country)
          AND (@quarter IS NULL OR d.[Quarter] = @quarter)
        GROUP BY m.MaterialCode, m.Group1
        ORDER BY Revenue DESC;
        """;

        return await db.Set<TopMaterialRow>()
            .FromSqlRaw(sql,
                new SqlParameter("@year", thisYear),
                new SqlParameter("@customer", (object?)customer ?? DBNull.Value),
                new SqlParameter("@country", (object?)country ?? DBNull.Value),
                new SqlParameter("@quarter", (object?)quarter ?? DBNull.Value),
                new SqlParameter("@topN", topN))
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<BudgetVarianceResponse> GetBudgetVsActualAsync(
         string? customer, string? country, int? year, int? quarter, CancellationToken ct)
    {
        var latestYear = await db.DimDates.MaxAsync(d => d.Year, ct);
        var y = year ?? latestYear;

        const string sql = """
            SELECT
                SUM(COALESCE(f.NetQty * f.NetSellingPrice, f.ConfirmedValue)) AS Actual,
                SUM(f.BudgetValue) AS Budget,
                SUM(COALESCE(f.NetQty * f.NetSellingPrice, f.ConfirmedValue)) - SUM(f.BudgetValue) AS VarianceValue,
                CASE WHEN SUM(f.BudgetValue) = 0 THEN NULL
                     ELSE (SUM(COALESCE(f.NetQty * f.NetSellingPrice, f.ConfirmedValue)) - SUM(f.BudgetValue))
                          / NULLIF(SUM(f.BudgetValue), 0) END AS VariancePct
            FROM FactSales f
            JOIN DimDates d ON d.[Date] = f.[Date]
            JOIN DimCustomers c ON c.CustomerId = f.CustomerId
            WHERE d.[Year] = @year
              AND (@customer IS NULL OR c.CustomerName = @customer)
              AND (@country  IS NULL OR c.Country      = @country)
              AND (@quarter  IS NULL OR d.[Quarter]    = @quarter);
            """;

        var row = (await db.Set<BudgetVarianceRow>()
            .FromSqlRaw(sql,
                new SqlParameter("@year", y),
                new SqlParameter("@customer", (object?)customer ?? DBNull.Value),
                new SqlParameter("@country", (object?)country ?? DBNull.Value),
                new SqlParameter("@quarter", (object?)quarter ?? DBNull.Value))
            .AsNoTracking()
            .ToListAsync(ct))
            .FirstOrDefault() ?? new BudgetVarianceRow();

        var dataAsOf = await db.DimDates.MaxAsync(d => d.Date, ct);

        return new BudgetVarianceResponse(
            Customer: customer, Country: country, Year: y, Quarter: quarter,
            Actual: Math.Round(row.Actual, 2),
            Budget: Math.Round(row.Budget, 2),
            VarianceValue: Math.Round(row.VarianceValue, 2),
            VariancePct: row.VariancePct is null ? null : Math.Round(row.VariancePct.Value, 4),
            DataAsOf: dataAsOf.ToString("yyyy-MM-dd"));
    }

    public async Task<ForecastAccuracyResponse> GetForecastAccuracyAsync(
        string? customer, string? country, int? year, int? quarter, CancellationToken ct)
    {
        var latestYear = await db.DimDates.MaxAsync(d => d.Year, ct);
        var y = year ?? latestYear;

        const string sql = """
            SELECT
                CASE WHEN SUM(COALESCE(f.NetQty * f.NetSellingPrice, f.ConfirmedValue)) = 0 THEN 0
                     ELSE AVG(ABS(
                         (COALESCE(f.NetQty * f.NetSellingPrice, f.ConfirmedValue) - f.ForecastValue) /
                         NULLIF(COALESCE(f.NetQty * f.NetSellingPrice, f.ConfirmedValue), 0)
                     )) END AS MAPE,
                SUM(COALESCE(f.NetQty * f.NetSellingPrice, f.ConfirmedValue) - f.ForecastValue) AS Bias
            FROM FactSales f
            JOIN DimDates d ON d.[Date] = f.[Date]
            JOIN DimCustomers c ON c.CustomerId = f.CustomerId
            WHERE d.[Year] = @year
              AND (@customer IS NULL OR c.CustomerName = @customer)
              AND (@country  IS NULL OR c.Country      = @country)
              AND (@quarter  IS NULL OR d.[Quarter]    = @quarter);
            """;

        var row = (await db.Set<ForecastAccuracyRow>()
            .FromSqlRaw(sql,
                new SqlParameter("@year", y),
                new SqlParameter("@customer", (object?)customer ?? DBNull.Value),
                new SqlParameter("@country", (object?)country ?? DBNull.Value),
                new SqlParameter("@quarter", (object?)quarter ?? DBNull.Value))
            .AsNoTracking()
            .ToListAsync(ct))
            .FirstOrDefault() ?? new ForecastAccuracyRow();

        var dataAsOf = await db.DimDates.MaxAsync(d => d.Date, ct);

        return new ForecastAccuracyResponse(
            Customer: customer, Country: country, Year: y, Quarter: quarter,
            MAPE: Math.Round(row.MAPE, 4),      // e.g., 0.0842 (8.42%)
            Bias: Math.Round(row.Bias, 2),      // positive = over-forecast
            DataAsOf: dataAsOf.ToString("yyyy-MM-dd"));
    }

    public async Task<IReadOnlyList<TopCustomerRow>> GetTopCustomersAsync(
    string? country, int? year, int? quarter, int topN, CancellationToken ct)
    {
        var latestYear = await db.DimDates.MaxAsync(d => d.Year, ct);
        var y = year ?? latestYear;

        const string sql = """
        WITH base AS (
            SELECT 
                c.CustomerName,
                c.Country,
                SUM(COALESCE(f.NetQty * f.NetSellingPrice, f.ConfirmedValue)) AS Revenue,
                SUM(COALESCE(f.NetQty, f.ConfirmedQty)) AS Units
            FROM FactSales f
            JOIN DimDates d ON d.[Date] = f.[Date]
            JOIN DimCustomers c ON c.CustomerId = f.CustomerId
            WHERE d.[Year] = @year
              AND (@country IS NULL OR c.Country = @country)
              AND (@quarter IS NULL OR d.[Quarter] = @quarter)
            GROUP BY c.CustomerName, c.Country
        ),
        total AS (SELECT SUM(Revenue) AS TotalRevenue FROM base)
        SELECT TOP (@topN)
            b.CustomerName,
            b.Country,
            b.Revenue,
            b.Units,
            CASE WHEN t.TotalRevenue = 0 THEN 0
                 ELSE b.Revenue / t.TotalRevenue END AS ContributionPct
        FROM base b
        CROSS JOIN total t
        ORDER BY b.Revenue DESC;
        """;

        return await db.Set<TopCustomerRow>()
            .FromSqlRaw(sql,
                new SqlParameter("@year", y),
                new SqlParameter("@country", (object?)country ?? DBNull.Value),
                new SqlParameter("@quarter", (object?)quarter ?? DBNull.Value),
                new SqlParameter("@topN", topN))
            .AsNoTracking()
            .ToListAsync(ct);
    }

}