using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LOSTBOOKS.Migrations
{
    /// <inheritdoc />
    public partial class UpdateConsignorRemovePaymentFieldsAddIsActive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccountName",
                table: "Consignors");

            migrationBuilder.DropColumn(
                name: "BankAccountNumber",
                table: "Consignors");

            migrationBuilder.DropColumn(
                name: "BankName",
                table: "Consignors");

            migrationBuilder.DropColumn(
                name: "GcashNumber",
                table: "Consignors");

            migrationBuilder.DropColumn(
                name: "HomeAddress",
                table: "Consignors");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Consignors",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Consignors");

            migrationBuilder.AddColumn<string>(
                name: "AccountName",
                table: "Consignors",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankAccountNumber",
                table: "Consignors",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankName",
                table: "Consignors",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GcashNumber",
                table: "Consignors",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "HomeAddress",
                table: "Consignors",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
