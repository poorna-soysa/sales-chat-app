using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatAppSales.Entities;

public sealed class DimDate
{
    [Key, Column(TypeName = "date")]
    public DateTime Date { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }          // 1–12
    public string MonthName { get; set; } = "";
    public int Quarter { get; set; }        // 1–4
    // (Add FiscalYear/FiscalMonth later if needed)
}



