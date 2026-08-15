using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TM_PE.Migrations
{
    /// <inheritdoc />
    public partial class user5 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tbl_jobticket",
                columns: table => new
                {
                    JobTicketID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TicketNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    JobName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ClientFullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PrimaryNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SecondaryNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    FiberPlan = table.Column<int>(type: "int", nullable: false),
                    InstallationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LocationAddress = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Latitude = table.Column<decimal>(type: "decimal(9,6)", nullable: false),
                    Longitude = table.Column<decimal>(type: "decimal(9,6)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_jobticket", x => x.JobTicketID);
                });

            migrationBuilder.CreateTable(
                name: "tbl_jobticketassignment",
                columns: table => new
                {
                    JobTicketAssignmentID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobTicketID = table.Column<int>(type: "int", nullable: false),
                    EmployeeID = table.Column<int>(type: "int", nullable: false),
                    IsLeader = table.Column<bool>(type: "bit", nullable: false),
                    AssignedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_jobticketassignment", x => x.JobTicketAssignmentID);
                    table.ForeignKey(
                        name: "FK_tbl_jobticketassignment_tbl_employees_EmployeeID",
                        column: x => x.EmployeeID,
                        principalTable: "tbl_employees",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tbl_jobticketassignment_tbl_jobticket_JobTicketID",
                        column: x => x.JobTicketID,
                        principalTable: "tbl_jobticket",
                        principalColumn: "JobTicketID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tbl_jobticketsubmission",
                columns: table => new
                {
                    JobTicketSubmissionID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobTicketID = table.Column<int>(type: "int", nullable: false),
                    EmployeeID = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Caption = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    DateSubmitted = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_jobticketsubmission", x => x.JobTicketSubmissionID);
                    table.ForeignKey(
                        name: "FK_tbl_jobticketsubmission_tbl_employees_EmployeeID",
                        column: x => x.EmployeeID,
                        principalTable: "tbl_employees",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tbl_jobticketsubmission_tbl_jobticket_JobTicketID",
                        column: x => x.JobTicketID,
                        principalTable: "tbl_jobticket",
                        principalColumn: "JobTicketID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_jobticketassignment_EmployeeID",
                table: "tbl_jobticketassignment",
                column: "EmployeeID");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_jobticketassignment_JobTicketID",
                table: "tbl_jobticketassignment",
                column: "JobTicketID");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_jobticketsubmission_EmployeeID",
                table: "tbl_jobticketsubmission",
                column: "EmployeeID");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_jobticketsubmission_JobTicketID",
                table: "tbl_jobticketsubmission",
                column: "JobTicketID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tbl_jobticketassignment");

            migrationBuilder.DropTable(
                name: "tbl_jobticketsubmission");

            migrationBuilder.DropTable(
                name: "tbl_jobticket");
        }
    }
}
