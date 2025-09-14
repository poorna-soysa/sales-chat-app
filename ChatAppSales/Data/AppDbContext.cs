using ChatAppSales.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChatAppSales.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<DimDate> DimDates => Set<DimDate>();
    public DbSet<DimCustomer> DimCustomers => Set<DimCustomer>();
    public DbSet<DimPlant> DimPlants => Set<DimPlant>();
    public DbSet<DimMaterial> DimMaterials => Set<DimMaterial>();
    public DbSet<FactSales> FactSales => Set<FactSales>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<DimDate>().HasKey(x => x.Date);
        b.Entity<DimCustomer>().HasKey(x => x.CustomerId);
        b.Entity<DimPlant>().HasKey(x => x.PlantId);
        b.Entity<DimMaterial>().HasKey(x => x.MaterialId);

        b.Entity<FactSales>().HasKey(x => x.Id);
        b.Entity<FactSales>()
            .HasOne(x => x.DateRef).WithMany().HasForeignKey(x => x.Date);
        b.Entity<FactSales>()
            .HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId);
        b.Entity<FactSales>()
            .HasOne(x => x.Plant).WithMany().HasForeignKey(x => x.PlantId);
        b.Entity<FactSales>()
            .HasOne(x => x.Material).WithMany().HasForeignKey(x => x.MaterialId);

        // Money/qty precision
        b.Entity<FactSales>().Property(x => x.ConfirmedQty).HasPrecision(18, 3);
        b.Entity<FactSales>().Property(x => x.ConfirmedValue).HasPrecision(18, 2);
        b.Entity<FactSales>().Property(x => x.NetQty).HasPrecision(18, 3);
        b.Entity<FactSales>().Property(x => x.NetSellingPrice).HasPrecision(18, 2);
        b.Entity<FactSales>().Property(x => x.BudgetQty).HasPrecision(18, 3);
        b.Entity<FactSales>().Property(x => x.BudgetValue).HasPrecision(18, 2);
        b.Entity<FactSales>().Property(x => x.ForecastQty).HasPrecision(18, 3);
        b.Entity<FactSales>().Property(x => x.ForecastValue).HasPrecision(18, 2);

        // Helpful indexes for analytics
        b.Entity<FactSales>().HasIndex(x => new { x.Date, x.CustomerId });
        b.Entity<FactSales>().HasIndex(x => new { x.Date, x.MaterialId });
        b.Entity<FactSales>().HasIndex(x => new { x.Date, x.PlantId });

        b.Entity<YearRevenueRow>().HasNoKey();
        b.Entity<YearMonthRevenueRow>().HasNoKey();
        b.Entity<TopMaterialRow>().HasNoKey();
        b.Entity<BudgetVarianceRow>().HasNoKey();
        b.Entity<ForecastAccuracyRow>().HasNoKey();
        b.Entity<TopCustomerRow>().HasNoKey();

    }
}