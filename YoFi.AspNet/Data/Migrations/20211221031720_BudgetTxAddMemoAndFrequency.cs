using Microsoft.EntityFrameworkCore.Migrations;

namespace YoFi.AspNet.Data.Migrations
{
    public partial class BudgetTxAddMemoAndFrequency : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Frequency",
                table: "BudgetTxs",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Memo",
                table: "BudgetTxs",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Frequency",
                table: "BudgetTxs");

            migrationBuilder.DropColumn(
                name: "Memo",
                table: "BudgetTxs");
        }
    }
}
