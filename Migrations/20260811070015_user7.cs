using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TM_PE.Migrations
{
    /// <inheritdoc />
    public partial class user7 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Caption",
                table: "tbl_jobticketsubmission");

            migrationBuilder.AddColumn<string>(
                name: "Remarks",
                table: "tbl_jobticket",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Remarks",
                table: "tbl_jobticket");

            migrationBuilder.AddColumn<string>(
                name: "Caption",
                table: "tbl_jobticketsubmission",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);
        }
    }
}
