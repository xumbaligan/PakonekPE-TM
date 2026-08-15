using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TM_PE.Migrations
{
    /// <inheritdoc />
    public partial class user4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_tbl_taskactivity_AssignedEmployeeID",
                table: "tbl_taskactivity",
                column: "AssignedEmployeeID");

            migrationBuilder.AddForeignKey(
                name: "FK_tbl_taskactivity_tbl_employees_AssignedEmployeeID",
                table: "tbl_taskactivity",
                column: "AssignedEmployeeID",
                principalTable: "tbl_employees",
                principalColumn: "EmployeeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tbl_taskactivity_tbl_employees_AssignedEmployeeID",
                table: "tbl_taskactivity");

            migrationBuilder.DropIndex(
                name: "IX_tbl_taskactivity_AssignedEmployeeID",
                table: "tbl_taskactivity");
        }
    }
}
