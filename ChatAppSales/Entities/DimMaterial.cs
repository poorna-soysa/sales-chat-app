using System.ComponentModel.DataAnnotations;

namespace ChatAppSales.Entities;

public sealed class DimMaterial
{
    [Key] public int MaterialId { get; set; }
    [MaxLength(60)] public string MaterialCode { get; set; } = "";  // e.g., "MAT-000123"
    [MaxLength(120)] public string MaterialType { get; set; } = ""; // e.g., "Finished Goods"
    [MaxLength(120)] public string Group1 { get; set; } = "";
    [MaxLength(120)] public string Group2 { get; set; } = "";
    [MaxLength(120)] public string Group3 { get; set; } = "";
    [MaxLength(120)] public string Group4 { get; set; } = "";
    [MaxLength(120)] public string Group5 { get; set; } = "";
    [MaxLength(120)] public string? CustomerMaterial { get; set; }
}


