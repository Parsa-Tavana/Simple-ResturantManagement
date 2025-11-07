using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddStripeFieldsToOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PaidAt",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripeSessionId",
                table: "Orders",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_PaidAt",
                table: "Orders",
                column: "PaidAt");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_StripeSessionId",
                table: "Orders",
                column: "StripeSessionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_PaidAt",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_StripeSessionId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PaidAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "StripeSessionId",
                table: "Orders");
        }
    }
}
