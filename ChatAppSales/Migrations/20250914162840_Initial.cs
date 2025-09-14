using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChatAppSales.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DimCustomers",
                columns: table => new
                {
                    CustomerId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Country = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    KAM = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    AAM = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DimCustomers", x => x.CustomerId);
                });

            migrationBuilder.CreateTable(
                name: "DimDates",
                columns: table => new
                {
                    Date = table.Column<DateTime>(type: "date", nullable: false),
                    Year = table.Column<short>(type: "smallint", nullable: false),
                    Month = table.Column<short>(type: "smallint", nullable: false),
                    MonthName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Quarter = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DimDates", x => x.Date);
                });

            migrationBuilder.CreateTable(
                name: "DimMaterials",
                columns: table => new
                {
                    MaterialId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaterialCode = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    MaterialType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Group1 = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Group2 = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Group3 = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Group4 = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Group5 = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    CustomerMaterial = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DimMaterials", x => x.MaterialId);
                });

            migrationBuilder.CreateTable(
                name: "DimPlants",
                columns: table => new
                {
                    PlantId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlantCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PlantName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Country = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DimPlants", x => x.PlantId);
                });

            migrationBuilder.CreateTable(
                name: "FactSales",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateTime>(type: "date", nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    PlantId = table.Column<int>(type: "int", nullable: false),
                    MaterialId = table.Column<int>(type: "int", nullable: false),
                    ConfirmedQty = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true),
                    ConfirmedValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    NetQty = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true),
                    NetSellingPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    BudgetQty = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true),
                    BudgetValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ForecastQty = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true),
                    ForecastValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FactSales", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FactSales_DimCustomers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "DimCustomers",
                        principalColumn: "CustomerId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FactSales_DimDates_Date",
                        column: x => x.Date,
                        principalTable: "DimDates",
                        principalColumn: "Date",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FactSales_DimMaterials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "DimMaterials",
                        principalColumn: "MaterialId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FactSales_DimPlants_PlantId",
                        column: x => x.PlantId,
                        principalTable: "DimPlants",
                        principalColumn: "PlantId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FactSales_CustomerId",
                table: "FactSales",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_FactSales_Date_CustomerId",
                table: "FactSales",
                columns: new[] { "Date", "CustomerId" });

            migrationBuilder.CreateIndex(
                name: "IX_FactSales_Date_MaterialId",
                table: "FactSales",
                columns: new[] { "Date", "MaterialId" });

            migrationBuilder.CreateIndex(
                name: "IX_FactSales_Date_PlantId",
                table: "FactSales",
                columns: new[] { "Date", "PlantId" });

            migrationBuilder.CreateIndex(
                name: "IX_FactSales_MaterialId",
                table: "FactSales",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_FactSales_PlantId",
                table: "FactSales",
                column: "PlantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FactSales");

            migrationBuilder.DropTable(
                name: "DimCustomers");

            migrationBuilder.DropTable(
                name: "DimDates");

            migrationBuilder.DropTable(
                name: "DimMaterials");

            migrationBuilder.DropTable(
                name: "DimPlants");
        }
    }
}
