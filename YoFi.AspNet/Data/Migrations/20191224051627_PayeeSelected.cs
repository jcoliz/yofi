using Microsoft.EntityFrameworkCore.Migrations;

namespace OfxWeb.Asp.Data.Migrations
{
    public partial class PayeeSelected : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Selected",
                table: "Payees",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Selected",
                table: "Payees");
        }
    }
}
