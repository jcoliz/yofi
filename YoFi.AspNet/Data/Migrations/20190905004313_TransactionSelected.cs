using Microsoft.EntityFrameworkCore.Migrations;

namespace OfxWeb.Asp.Data.Migrations
{
    public partial class TransactionSelected : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Selected",
                table: "Transactions",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Selected",
                table: "Transactions");
        }
    }
}
