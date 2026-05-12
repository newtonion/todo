using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class Subtasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ParentListItemId",
                table: "ListItems",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_ListItems_ParentListItemId",
                table: "ListItems",
                column: "ParentListItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_ListItems_ListItems_ParentListItemId",
                table: "ListItems",
                column: "ParentListItemId",
                principalTable: "ListItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ListItems_ListItems_ParentListItemId",
                table: "ListItems");

            migrationBuilder.DropIndex(
                name: "IX_ListItems_ParentListItemId",
                table: "ListItems");

            migrationBuilder.DropColumn(
                name: "ParentListItemId",
                table: "ListItems");
        }
    }
}
