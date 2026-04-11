using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace RentGrid.Api.Migrations
{
    /// <inheritdoc />
    public partial class SeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "ExtraServices",
                columns: new[] { "Id", "Name", "Price" },
                values: new object[,]
                {
                    { 1, "GPS", 2000m },
                    { 2, "Gyerekülés", 1500m },
                    { 3, "Extra biztosítás", 5000m }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "Email", "FullName", "PasswordHash", "Role" },
                values: new object[] { 1, new DateTime(2026, 4, 11, 0, 0, 0, 0, DateTimeKind.Utc), "admin@rentgrid.local", "Admin User", "$2a$12$OMuczbfgm9D4Ix.5EhxeKeDmqYFbW464rY2k9TT2yHoYeMiy8K/ja", "Admin" });

            migrationBuilder.InsertData(
                table: "Vehicles",
                columns: new[] { "Id", "Brand", "DailyPrice", "IsAvailable", "Model", "MongoImageId" },
                values: new object[,]
                {
                    { 1, "Toyota", 17500m, true, "Corolla", null },
                    { 2, "Ford", 16500m, true, "Focus", null },
                    { 3, "BMW", 32000m, true, "320i", null },
                    { 4, "Škoda", 18500m, true, "Octavia", null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ExtraServices",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "ExtraServices",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "ExtraServices",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Vehicles",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Vehicles",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Vehicles",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Vehicles",
                keyColumn: "Id",
                keyValue: 4);
        }
    }
}
