using ChatAppSales.Data;

namespace ChatAppSales.Entities;

public static class DemoSeeder
{
    public static async Task SeedAsync(AppDbContext db, int yearsBack)
    {
        var rng = new Random(42); // deterministic

        // 1) DimDate: last `yearsBack` full years up to yesterday
        var today = DateTime.UtcNow.Date;
        var start = new DateTime(today.Year - yearsBack, 1, 1);
        var end = today.AddDays(-1);

        var dates = Enumerable.Range(0, (end - start).Days + 1)
            .Select(i => start.AddDays(i))
            .Select(d => new DimDate
            {
                Date = d,
                Year = (short)d.Year,
                Month = (short)d.Month,
                MonthName = d.ToString("MMM"),
                Quarter = (short)((d.Month - 1) / 3 + 1)
            })
            .ToArray();

        await db.DimDates.AddRangeAsync(dates);

        // 2) Customers (realistic names + countries; attach KAM/AAM)
        var customers = RealisticCustomers()
            .Select((c, i) => new DimCustomer
            {
                //CustomerId = i + 1,
                CustomerName = c.Name,
                Country = c.Country,
                KAM = c.KAM,
                AAM = c.AAM
            }).ToArray();
        await db.DimCustomers.AddRangeAsync(customers);

        // 3) Plants (realistic)
        var plants = RealisticPlants()
            .Select((p, i) => new DimPlant
            {
                // PlantId = i + 1,
                PlantCode = p.Code,
                PlantName = p.Name,
                Country = p.Country,
                City = p.City
            }).ToArray();
        await db.DimPlants.AddRangeAsync(plants);

        // 4) Materials (groups & type)
        var materials = GenerateMaterials(120); // 120 SKUs
        await db.DimMaterials.AddRangeAsync(materials);

        await db.SaveChangesAsync();

        // 5) FactSales
        // Strategy: for each day, pick a subset of (customer, plant, ~15 random materials)
        // Apply seasonality (Q4 up, weekends down), regional mix, and price bands by material group.
        var factBuffer = new List<FactSales>(capacity: 200_000);

        foreach (var day in dates)
        {
            var isWeekend = day.Date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
            var seasonal = Seasonality(day.Date) * (isWeekend ? 0.6m : 1.0m);

            // choose 8 random customers & 2 plants per day
            var dayCustomers = customers.OrderBy(_ => rng.Next()).Take(8).ToArray();
            var dayPlants = plants.OrderBy(_ => rng.Next()).Take(2).ToArray();
            var dayMaterials = materials.OrderBy(_ => rng.Next()).Take(15).ToArray();

            foreach (var cust in dayCustomers)
                foreach (var plant in dayPlants)
                    foreach (var mat in dayMaterials)
                    {
                        var baseQty = BaseQtyFor(mat) * seasonal * CountryDemandFactor(cust.Country);
                        var qty = Jitter(rng, baseQty, 0.35m); // ±35% noise
                        if (qty < 1m) continue;                // skip tiny

                        var price = BasePriceFor(mat);
                        var netValue = qty * price;

                        // Forecast/Budget: start-of-year planning with noise
                        var budgetFactor = 1.05m; // plan a bit higher than last year
                        var forecastFactor = 1.02m;

                        var budgetQty = Jitter(rng, qty * budgetFactor, 0.25m);
                        var forecastQty = Jitter(rng, qty * forecastFactor, 0.20m);

                        factBuffer.Add(new FactSales
                        {
                            Date = day.Date,
                            CustomerId = cust.CustomerId,
                            PlantId = plant.PlantId,
                            MaterialId = mat.MaterialId,

                            NetQty = DecimalRound(qty, 3),
                            NetSellingPrice = DecimalRound(price, 2),
                            ConfirmedQty = DecimalRound(qty, 3), // keep same for demo
                            ConfirmedValue = DecimalRound(netValue, 2),

                            BudgetQty = DecimalRound(budgetQty, 3),
                            BudgetValue = DecimalRound(budgetQty * price, 2),

                            ForecastQty = DecimalRound(forecastQty, 3),
                            ForecastValue = DecimalRound(forecastQty * price, 2),
                        });
                    }

            // Batch insert per ~7 days to keep memory in check
            if (factBuffer.Count >= 25_000)
            {
                await db.FactSales.AddRangeAsync(factBuffer);
                await db.SaveChangesAsync();
                factBuffer.Clear();
            }
        }

        if (factBuffer.Count > 0)
        {
            await db.FactSales.AddRangeAsync(factBuffer);
            await db.SaveChangesAsync();
            factBuffer.Clear();
        }
    }

    // ---------- Helpers ----------

    private static decimal Seasonality(DateTime d)
    {
        // Q4 bump, Q1 softer; small monthly wave
        var q = (d.Month - 1) / 3 + 1;
        decimal qFactor = q switch { 1 => 0.92m, 2 => 1.00m, 3 => 1.05m, 4 => 1.12m, _ => 1.0m };
        decimal monthly = 0.95m + (decimal)(0.1 * Math.Sin(d.Month / 12.0 * 2 * Math.PI));
        return qFactor * monthly;
    }

    private static decimal CountryDemandFactor(string country) => country switch
    {
        "USA" => 1.20m,
        "Germany" => 1.10m,
        "UK" => 1.05m,
        "UAE" => 1.00m,
        "India" => 1.15m,
        "Sri Lanka" => 0.95m,
        "Vietnam" => 1.00m,
        "Japan" => 1.05m,
        _ => 1.00m
    };

    private static decimal BaseQtyFor(DimMaterial m)
    {
        // Use group/type to influence volume
        var baseQty = m.MaterialType switch
        {
            "Finished Goods" => 120m,
            "Components" => 60m,
            _ => 40m
        };
        if (m.Group1.Contains("Premium")) baseQty *= 0.6m;
        if (m.Group1.Contains("Economy")) baseQty *= 1.2m;
        return baseQty;
    }

    private static decimal BasePriceFor(DimMaterial m)
    {
        // Simple bands by group/type
        var basePrice = m.MaterialType switch
        {
            "Finished Goods" => 25m,
            "Components" => 12m,
            _ => 8m
        };
        if (m.Group1.Contains("Premium")) basePrice *= 1.6m;
        if (m.Group1.Contains("Economy")) basePrice *= 0.75m;
        return basePrice;
    }

    private static decimal Jitter(Random rng, decimal value, decimal pct)
    {
        var delta = (decimal)rng.NextDouble() * 2 * pct - pct; // -pct..+pct
        return value * (1 + delta);
    }

    private static decimal DecimalRound(decimal v, int digits) => Math.Round(v, digits, MidpointRounding.AwayFromZero);

    private static IReadOnlyList<(string Name, string Country, string KAM, string AAM)> RealisticCustomers() => new[]
    {
        ("Atlas Retail Group", "USA", "Priyanka N.", "Kavishka D."),
        ("Harbor & Co.", "UK", "Sahan P.", "Amaya J."),
        ("Bavaria Trading GmbH", "Germany", "Tharindu M.", "Ishara S."),
        ("Emirates Wholesale LLC", "UAE", "Nipun K.", "Ruwangi R."),
        ("Shinoda Distribution", "Japan", "Dilhani W.", "Pasindu L."),
        ("Indus Mercantile", "India", "Chathura G.", "Harini T."),
        ("Colombo Superstores", "Sri Lanka", "Kasun R.", "Dilushi F."),
        ("Saigon Partners", "Vietnam", "Ruchira P.", "Asmika V."),
        ("Blue Ridge Outlets", "USA", "Suresh A.", "Gayani E."),
        ("Crown Markets", "UK", "Heshan C.", "Rasangi D."),
        ("Rhine Retail AG", "Germany", "Dineth M.", "Anushka P."),
        ("Desert Line Traders", "UAE", "Nadun B.", "Nadeesha S."),
        ("Kansai Supply", "Japan", "Yasiru T.", "Sithmi K."),
        ("Deccan Bazaar", "India", "Isuru J.", "Sonali R."),
        ("Lanka Value Mart", "Sri Lanka", "Chalana D.", "Sewwandi H."),
        ("Mekong Commerce", "Vietnam", "Nuwan I.", "Anjana P.")
    };

    private static IReadOnlyList<(string Code, string Name, string Country, string City)> RealisticPlants() => new[]
    {
        ("SL-CMB-01", "Colombo Main Plant", "Sri Lanka", "Colombo"),
        ("SL-KTN-02", "Katuwana Processing", "Sri Lanka", "Homagama"),
        ("IN-PUN-01", "Pune Assembly", "India", "Pune"),
        ("IN-CHE-02", "Chennai Fabrication", "India", "Chennai"),
        ("AE-DXB-01", "Dubai Hub", "UAE", "Dubai"),
        ("DE-HAM-01", "Hamburg Finishing", "Germany", "Hamburg"),
        ("US-HOU-01", "Houston Distribution", "USA", "Houston"),
        ("UK-MAN-01", "Manchester Packaging", "UK", "Manchester"),
    };

    private static DimMaterial[] GenerateMaterials(int count)
    {
        var types = new[] { "Finished Goods", "Components", "Accessories" };
        var g1 = new[] { "Premium Line", "Standard Line", "Economy Line" };
        var g2 = new[] { "Industrial", "Consumer", "Healthcare", "Automotive" };
        var g3 = new[] { "North", "South", "East", "West" };
        var g4 = new[] { "Bulk", "Pack", "Single" };
        var g5 = new[] { "Online", "Retail", "Wholesale" };

        var list = new List<DimMaterial>(count);
        for (int i = 1; i <= count; i++)
        {
            list.Add(new DimMaterial
            {
                //MaterialId = i,
                MaterialCode = $"MAT-{i:0000}",
                MaterialType = types[i % types.Length],
                Group1 = g1[i % g1.Length],
                Group2 = g2[i % g2.Length],
                Group3 = g3[i % g3.Length],
                Group4 = g4[i % g4.Length],
                Group5 = g5[i % g5.Length],
                CustomerMaterial = i % 7 == 0 ? $"CUS-MAT-{i:0000}" : null
            });
        }
        return list.ToArray();
    }
}


