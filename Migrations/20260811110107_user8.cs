using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TM_PE.Migrations
{
    /// <inheritdoc />
    public partial class user8 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "tbl_jobticket");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "tbl_jobticket");

            migrationBuilder.AddColumn<string>(
                name: "NearestLandmark",
                table: "tbl_jobticket",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NearestLandmark",
                table: "tbl_jobticket");

            migrationBuilder.AddColumn<decimal>(
                name: "Latitude",
                table: "tbl_jobticket",
                type: "decimal(9,6)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Longitude",
                table: "tbl_jobticket",
                type: "decimal(9,6)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
