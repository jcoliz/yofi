using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace YoFi.Data.Migrations
{
    [ExcludeFromCodeCoverage]
    public partial class PayeeIDint : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Payees",
                table: "Payees");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Payees",
                nullable: true,
                oldClrType: typeof(string));

            migrationBuilder.AddColumn<int>(
                name: "ID",
                table: "Payees",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Payees",
                table: "Payees",
                column: "ID");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Payees",
                table: "Payees");

            migrationBuilder.DropColumn(
                name: "ID",
                table: "Payees");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Payees",
                nullable: false,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Payees",
                table: "Payees",
                column: "Name");
        }
    }
}
