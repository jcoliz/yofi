using Microsoft.EntityFrameworkCore.Migrations;

namespace YoFi.Data.Migrations
{
    public partial class Transactions_Index : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Category",
                table: "Transactions",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_Timestamp_Category",
                table: "Transactions",
                columns: new[] { "Timestamp", "Category" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Transactions_Timestamp_Category",
                table: "Transactions");

            migrationBuilder.AlterColumn<string>(
                name: "Category",
                table: "Transactions",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldNullable: true);
        }
    }
}
