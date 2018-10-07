using Microsoft.EntityFrameworkCore.Migrations;

namespace OfxWeb.Asp.Data.Migrations
{
    public partial class TransactionHidden : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Hidden",
                table: "Transactions",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Hidden",
                table: "Transactions");
        }
    }
}
