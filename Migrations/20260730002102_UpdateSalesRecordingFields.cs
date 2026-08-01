using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LOSTBOOKS.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSalesRecordingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentType",
                table: "SalesRecordings");

            migrationBuilder.DropColumn(
                name: "TotalAmount",
                table: "SalesRecordings");

            migrationBuilder.RenameColumn(
                name: "UnitPrice",
                table: "SalesRecordings",
                newName: "SellingPrice");

            migrationBuilder.RenameColumn(
                name: "Quantity",
                table: "SalesRecordings",
                newName: "QuantitySold");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SellingPrice",
                table: "SalesRecordings",
                newName: "UnitPrice");

            migrationBuilder.RenameColumn(
                name: "QuantitySold",
                table: "SalesRecordings",
                newName: "Quantity");

            migrationBuilder.AddColumn<string>(
                name: "PaymentType",
                table: "SalesRecordings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "TotalAmount",
                table: "SalesRecordings",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
