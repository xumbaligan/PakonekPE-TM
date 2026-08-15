using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TM_PE.Migrations
{
    /// <inheritdoc />
    public partial class user10 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RescheduleHistoryID",
                table: "tbl_jobticketsubmission",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "tbl_jobticketreschedulehistory",
                columns: table => new
                {
                    JobTicketRescheduleHistoryID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobTicketID = table.Column<int>(type: "int", nullable: false),
                    OldServiceDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NewServiceDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PreviousStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    PreviousRemarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DateChanged = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_jobticketreschedulehistory", x => x.JobTicketRescheduleHistoryID);
                    table.ForeignKey(
                        name: "FK_tbl_jobticketreschedulehistory_tbl_jobticket_JobTicketID",
                        column: x => x.JobTicketID,
                        principalTable: "tbl_jobticket",
                        principalColumn: "JobTicketID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_jobticketsubmission_RescheduleHistoryID",
                table: "tbl_jobticketsubmission",
                column: "RescheduleHistoryID");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_jobticketreschedulehistory_JobTicketID",
                table: "tbl_jobticketreschedulehistory",
                column: "JobTicketID");

            migrationBuilder.AddForeignKey(
                name: "FK_tbl_jobticketsubmission_tbl_jobticketreschedulehistory_RescheduleHistoryID",
                table: "tbl_jobticketsubmission",
                column: "RescheduleHistoryID",
                principalTable: "tbl_jobticketreschedulehistory",
                principalColumn: "JobTicketRescheduleHistoryID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tbl_jobticketsubmission_tbl_jobticketreschedulehistory_RescheduleHistoryID",
                table: "tbl_jobticketsubmission");

            migrationBuilder.DropTable(
                name: "tbl_jobticketreschedulehistory");

            migrationBuilder.DropIndex(
                name: "IX_tbl_jobticketsubmission_RescheduleHistoryID",
                table: "tbl_jobticketsubmission");

            migrationBuilder.DropColumn(
                name: "RescheduleHistoryID",
                table: "tbl_jobticketsubmission");
        }
    }
}
