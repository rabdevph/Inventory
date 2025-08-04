using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCancelledByUserToInventoryTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "TransactionDate",
                table: "InventoryTransactions",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "InventoryTransactions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAt",
                table: "InventoryTransactions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancelledByUserId",
                table: "InventoryTransactions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "InventoryTransactions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_CancelledByUserId",
                table: "InventoryTransactions",
                column: "CancelledByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_Status",
                table: "InventoryTransactions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_TransactionType_Status",
                table: "InventoryTransactions",
                columns: new[] { "TransactionType", "Status" });

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryTransactions_Users_CancelledByUserId",
                table: "InventoryTransactions",
                column: "CancelledByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryTransactions_Users_CancelledByUserId",
                table: "InventoryTransactions");

            migrationBuilder.DropIndex(
                name: "IX_InventoryTransactions_CancelledByUserId",
                table: "InventoryTransactions");

            migrationBuilder.DropIndex(
                name: "IX_InventoryTransactions_Status",
                table: "InventoryTransactions");

            migrationBuilder.DropIndex(
                name: "IX_InventoryTransactions_TransactionType_Status",
                table: "InventoryTransactions");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                table: "InventoryTransactions");

            migrationBuilder.DropColumn(
                name: "CancelledByUserId",
                table: "InventoryTransactions");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "InventoryTransactions");

            migrationBuilder.AlterColumn<DateTime>(
                name: "TransactionDate",
                table: "InventoryTransactions",
                type: "timestamp with time zone",
                nullable: true,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "InventoryTransactions",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");
        }
    }
}
