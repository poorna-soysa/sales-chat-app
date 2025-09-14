using System.ComponentModel.DataAnnotations;

namespace ChatAppSales.Entities;

public sealed class DimCustomer
{
    [Key] public int CustomerId { get; set; }
    [MaxLength(200)] public string CustomerName { get; set; } = "";
    [MaxLength(100)] public string Country { get; set; } = "";
    [MaxLength(120)] public string KAM { get; set; } = "";   // Key Account Manager
    [MaxLength(120)] public string AAM { get; set; } = "";   // Area Account Manager
}


