using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LOSTBOOKS.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentDetailToHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PaymentDetail",
                table: "Histories",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentDetail",
                table: "Histories");
        }
    }
}
