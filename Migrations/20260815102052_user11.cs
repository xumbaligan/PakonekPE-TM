using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TM_PE.Migrations
{
    /// <inheritdoc />
    public partial class user11 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SubmissionHistoryID",
                table: "tbl_jobticketsubmission",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "tbl_jobticketsubmissionhistory",
                columns: table => new
                {
                    JobTicketSubmissionHistoryID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobTicketID = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DateChanged = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_jobticketsubmissionhistory", x => x.JobTicketSubmissionHistoryID);
                    table.ForeignKey(
                        name: "FK_tbl_jobticketsubmissionhistory_tbl_jobticket_JobTicketID",
                        column: x => x.JobTicketID,
                        principalTable: "tbl_jobticket",
                        principalColumn: "JobTicketID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_jobticketsubmission_SubmissionHistoryID",
                table: "tbl_jobticketsubmission",
                column: "SubmissionHistoryID");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_jobticketsubmissionhistory_JobTicketID",
                table: "tbl_jobticketsubmissionhistory",
                column: "JobTicketID");

            migrationBuilder.AddForeignKey(
                name: "FK_tbl_jobticketsubmission_tbl_jobticketsubmissionhistory_SubmissionHistoryID",
                table: "tbl_jobticketsubmission",
                column: "SubmissionHistoryID",
                principalTable: "tbl_jobticketsubmissionhistory",
                principalColumn: "JobTicketSubmissionHistoryID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tbl_jobticketsubmission_tbl_jobticketsubmissionhistory_SubmissionHistoryID",
                table: "tbl_jobticketsubmission");

            migrationBuilder.DropTable(
                name: "tbl_jobticketsubmissionhistory");

            migrationBuilder.DropIndex(
                name: "IX_tbl_jobticketsubmission_SubmissionHistoryID",
                table: "tbl_jobticketsubmission");

            migrationBuilder.DropColumn(
                name: "SubmissionHistoryID",
                table: "tbl_jobticketsubmission");
        }
    }
}
