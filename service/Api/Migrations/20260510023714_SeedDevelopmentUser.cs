using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class SeedDevelopmentUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Seed development test user - matches TestUserId in appsettings.Development.json
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "AuthId", "Name" },
                values: new object[] 
                { 
                    Guid.Parse("00000000-0000-0000-0000-000000000001"), 
                    "dev-user", 
                    "Development User"
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: Guid.Parse("00000000-0000-0000-0000-000000000001"));
        }
    }
}
