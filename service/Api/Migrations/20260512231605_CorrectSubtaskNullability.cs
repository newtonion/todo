using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class CorrectSubtaskNullability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ListItems_ListItems_ParentListItemId",
                table: "ListItems");

            migrationBuilder.AlterColumn<Guid>(
                name: "ParentListItemId",
                table: "ListItems",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AddForeignKey(
                name: "FK_ListItems_ListItems_ParentListItemId",
                table: "ListItems",
                column: "ParentListItemId",
                principalTable: "ListItems",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ListItems_ListItems_ParentListItemId",
                table: "ListItems");

            migrationBuilder.AlterColumn<Guid>(
                name: "ParentListItemId",
                table: "ListItems",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ListItems_ListItems_ParentListItemId",
                table: "ListItems",
                column: "ParentListItemId",
                principalTable: "ListItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
