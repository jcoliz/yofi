using Microsoft.EntityFrameworkCore.Migrations;

namespace YoFi.AspNet.Data.Migrations
{
    public partial class TransactionImported : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Imported",
                table: "Transactions",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Imported",
                table: "Transactions");
        }
    }
}
