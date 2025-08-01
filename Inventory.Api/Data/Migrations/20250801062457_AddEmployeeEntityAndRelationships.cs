using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Inventory.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeEntityAndRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryTransactions_Users_RequestedByUserId",
                table: "InventoryTransactions");

            migrationBuilder.DropIndex(
                name: "IX_InventoryTransactions_RequestedByUserId",
                table: "InventoryTransactions");

            migrationBuilder.DropColumn(
                name: "RequestedByUserId",
                table: "InventoryTransactions");

            migrationBuilder.AddColumn<int>(
                name: "RequestedByEmployeeId",
                table: "InventoryTransactions",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Employees",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Position = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Department = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employees", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_RequestedByEmployeeId",
                table: "InventoryTransactions",
                column: "RequestedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_FirstName_LastName",
                table: "Employees",
                columns: new[] { "FirstName", "LastName" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryTransactions_Employees_RequestedByEmployeeId",
                table: "InventoryTransactions",
                column: "RequestedByEmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryTransactions_Employees_RequestedByEmployeeId",
                table: "InventoryTransactions");

            migrationBuilder.DropTable(
                name: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_InventoryTransactions_RequestedByEmployeeId",
                table: "InventoryTransactions");

            migrationBuilder.DropColumn(
                name: "RequestedByEmployeeId",
                table: "InventoryTransactions");

            migrationBuilder.AddColumn<string>(
                name: "RequestedByUserId",
                table: "InventoryTransactions",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_RequestedByUserId",
                table: "InventoryTransactions",
                column: "RequestedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryTransactions_Users_RequestedByUserId",
                table: "InventoryTransactions",
                column: "RequestedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
