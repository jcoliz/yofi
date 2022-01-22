using Microsoft.EntityFrameworkCore.Migrations;

namespace YoFi.AspNet.Data.Migrations
{
    public partial class Splits_RemoveSubcategory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SubCategory",
                table: "Split");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SubCategory",
                table: "Split",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
