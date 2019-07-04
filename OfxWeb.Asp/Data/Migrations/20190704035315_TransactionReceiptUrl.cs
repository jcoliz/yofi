using Microsoft.EntityFrameworkCore.Migrations;

namespace OfxWeb.Asp.Data.Migrations
{
    public partial class TransactionReceiptUrl : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReceiptUrl",
                table: "Transactions",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReceiptUrl",
                table: "Transactions");
        }
    }
}
