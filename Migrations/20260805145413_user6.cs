using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TM_PE.Migrations
{
    /// <inheritdoc />
    public partial class user6 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "JobName",
                table: "tbl_jobticket");

            migrationBuilder.RenameColumn(
                name: "InstallationDate",
                table: "tbl_jobticket",
                newName: "ServiceDate");

            migrationBuilder.AlterColumn<int>(
                name: "FiberPlan",
                table: "tbl_jobticket",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "tbl_jobticket",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JobType",
                table: "tbl_jobticket",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "tbl_jobticket");

            migrationBuilder.DropColumn(
                name: "JobType",
                table: "tbl_jobticket");

            migrationBuilder.RenameColumn(
                name: "ServiceDate",
                table: "tbl_jobticket",
                newName: "InstallationDate");

            migrationBuilder.AlterColumn<int>(
                name: "FiberPlan",
                table: "tbl_jobticket",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JobName",
                table: "tbl_jobticket",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");
        }
    }
}
