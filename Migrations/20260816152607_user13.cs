using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TM_PE.Migrations
{
    /// <inheritdoc />
    public partial class user13 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DateOfCompletion",
                table: "tbl_jobticket",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DateOfCompletion",
                table: "tbl_jobticket");
        }
    }
}
