using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatAppSales.Entities;

public sealed class FactSales
{
    [Key] public long Id { get; set; }             // Surrogate key for the fact row
    [Column(TypeName = "date")]
    public DateTime Date { get; set; }             // FK -> DimDate

    // FKs
    public int CustomerId { get; set; }
    public int PlantId { get; set; }
    public int MaterialId { get; set; }

    // Measures (matching your SAP export)
    public decimal? ConfirmedQty { get; set; }
    public decimal? ConfirmedValue { get; set; }
    public decimal? NetQty { get; set; }
    public decimal? NetSellingPrice { get; set; }  // unit price
    public decimal? BudgetQty { get; set; }
    public decimal? BudgetValue { get; set; }
    public decimal? ForecastQty { get; set; }
    public decimal? ForecastValue { get; set; }

    // Navs
    public DimDate DateRef { get; set; } = default!;
    public DimCustomer Customer { get; set; } = default!;
    public DimPlant Plant { get; set; } = default!;
    public DimMaterial Material { get; set; } = default!;
}


