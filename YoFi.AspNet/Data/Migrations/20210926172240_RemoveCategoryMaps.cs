using Microsoft.EntityFrameworkCore.Migrations;

namespace YoFi.Data.Migrations
{
    public partial class RemoveCategoryMaps : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CategoryMaps");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CategoryMaps",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Key1 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Key2 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Key3 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SubCategory = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoryMaps", x => x.ID);
                });
        }
    }
}
