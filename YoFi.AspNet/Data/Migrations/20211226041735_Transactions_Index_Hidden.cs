using Microsoft.EntityFrameworkCore.Migrations;

namespace YoFi.AspNet.Data.Migrations
{
    public partial class Transactions_Index_Hidden : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Transactions_Timestamp_Category",
                table: "Transactions");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_Timestamp_Hidden_Category",
                table: "Transactions",
                columns: new[] { "Timestamp", "Hidden", "Category" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Transactions_Timestamp_Hidden_Category",
                table: "Transactions");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_Timestamp_Category",
                table: "Transactions",
                columns: new[] { "Timestamp", "Category" });
        }
    }
}
