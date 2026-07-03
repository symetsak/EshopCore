using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Eshop.Infrastructure.Migrations.ApplicationDb
{
    /// <inheritdoc />
    public partial class AddpaymentMethodAndIban : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PaymentMethod",
                table: "Orders",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "CashOnDelivery");

            migrationBuilder.AddColumn<string>(
                name: "Iban",
                table: "OrderReturns",
                type: "character varying(34)",
                maxLength: 34,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Iban",
                table: "OrderReturns");
        }
    }
}
