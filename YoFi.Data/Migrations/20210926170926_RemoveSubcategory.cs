using Microsoft.EntityFrameworkCore.Migrations;

namespace YoFi.Data.Migrations
{
    public partial class RemoveSubcategory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SubCategory",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "SubCategory",
                table: "Payees");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SubCategory",
                table: "Transactions",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubCategory",
                table: "Payees",
                nullable: true);
        }
    }
}
