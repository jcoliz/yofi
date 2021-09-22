using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace YoFi.AspNet.Data.Migrations
{
    [ExcludeFromCodeCoverage]
    public partial class SplitsV2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Split_Transactions_TransactionID",
                table: "Split");

            migrationBuilder.AlterColumn<int>(
                name: "TransactionID",
                table: "Split",
                nullable: false,
                oldClrType: typeof(int),
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Split_Transactions_TransactionID",
                table: "Split",
                column: "TransactionID",
                principalTable: "Transactions",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Split_Transactions_TransactionID",
                table: "Split");

            migrationBuilder.AlterColumn<int>(
                name: "TransactionID",
                table: "Split",
                nullable: true,
                oldClrType: typeof(int));

            migrationBuilder.AddForeignKey(
                name: "FK_Split_Transactions_TransactionID",
                table: "Split",
                column: "TransactionID",
                principalTable: "Transactions",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
