using System.ComponentModel.DataAnnotations;

namespace ChatAppSales.Entities;

public sealed class DimPlant
{
    [Key] public int PlantId { get; set; }
    [MaxLength(50)] public string PlantCode { get; set; } = "";     // e.g., "SL-CMB-01"
    [MaxLength(120)] public string PlantName { get; set; } = "";    // e.g., "Colombo Main Plant"
    [MaxLength(100)] public string Country { get; set; } = "";
    [MaxLength(100)] public string City { get; set; } = "";
}


