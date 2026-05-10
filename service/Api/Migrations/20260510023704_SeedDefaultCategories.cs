using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class SeedDefaultCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var now = DateTime.UtcNow;
            
            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "Name", "OwnerId", "CreatedOn", "UpdatedOn" },
                values: new object[,]
                {
                    { Guid.CreateVersion7(), "General", null, now, now },
                    { Guid.CreateVersion7(), "Grocery", null, now, now },
                    { Guid.CreateVersion7(), "Errand", null, now, now },
                    { Guid.CreateVersion7(), "Kids", null, now, now },
                    { Guid.CreateVersion7(), "Work", null, now, now },
                    { Guid.CreateVersion7(), "Home", null, now, now },
                    { Guid.CreateVersion7(), "Personal", null, now, now }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM Categories WHERE OwnerId IS NULL");
        }
    }
}
