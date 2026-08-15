using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TM_PE.Migrations
{
    /// <inheritdoc />
    public partial class user1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tbl_criteria",
                columns: table => new
                {
                    CriteriaId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CriteriaName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RoleType = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_criteria", x => x.CriteriaId);
                });

            migrationBuilder.CreateTable(
                name: "tbl_departments",
                columns: table => new
                {
                    DepartmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DepartmentName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_departments", x => x.DepartmentId);
                });

            migrationBuilder.CreateTable(
                name: "tbl_officetask",
                columns: table => new
                {
                    OfficeTaskID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaskNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TaskName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Progress = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_officetask", x => x.OfficeTaskID);
                });

            migrationBuilder.CreateTable(
                name: "tbl_employees",
                columns: table => new
                {
                    EmployeeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    RoleType = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    HiredAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_employees", x => x.EmployeeId);
                    table.ForeignKey(
                        name: "FK_tbl_employees_tbl_departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "tbl_departments",
                        principalColumn: "DepartmentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tbl_taskactivity",
                columns: table => new
                {
                    ActivityID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActivityName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FeedBack = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OfficeTaskID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_taskactivity", x => x.ActivityID);
                    table.ForeignKey(
                        name: "FK_tbl_taskactivity_tbl_officetask_OfficeTaskID",
                        column: x => x.OfficeTaskID,
                        principalTable: "tbl_officetask",
                        principalColumn: "OfficeTaskID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tbl_taskassignment",
                columns: table => new
                {
                    TaskAssignmentID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OfficeTaskID = table.Column<int>(type: "int", nullable: false),
                    EmployeeID = table.Column<int>(type: "int", nullable: false),
                    AssignedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_taskassignment", x => x.TaskAssignmentID);
                    table.ForeignKey(
                        name: "FK_tbl_taskassignment_tbl_employees_EmployeeID",
                        column: x => x.EmployeeID,
                        principalTable: "tbl_employees",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tbl_taskassignment_tbl_officetask_OfficeTaskID",
                        column: x => x.OfficeTaskID,
                        principalTable: "tbl_officetask",
                        principalColumn: "OfficeTaskID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tbl_activitysubmission",
                columns: table => new
                {
                    SubmissionID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActivityID = table.Column<int>(type: "int", nullable: false),
                    EmployeeID = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateSubmitted = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_activitysubmission", x => x.SubmissionID);
                    table.ForeignKey(
                        name: "FK_tbl_activitysubmission_tbl_employees_EmployeeID",
                        column: x => x.EmployeeID,
                        principalTable: "tbl_employees",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tbl_activitysubmission_tbl_taskactivity_ActivityID",
                        column: x => x.ActivityID,
                        principalTable: "tbl_taskactivity",
                        principalColumn: "ActivityID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_activitysubmission_ActivityID",
                table: "tbl_activitysubmission",
                column: "ActivityID");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_activitysubmission_EmployeeID",
                table: "tbl_activitysubmission",
                column: "EmployeeID");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_employees_DepartmentId",
                table: "tbl_employees",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_employees_Email",
                table: "tbl_employees",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbl_taskactivity_OfficeTaskID",
                table: "tbl_taskactivity",
                column: "OfficeTaskID");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_taskassignment_EmployeeID",
                table: "tbl_taskassignment",
                column: "EmployeeID");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_taskassignment_OfficeTaskID",
                table: "tbl_taskassignment",
                column: "OfficeTaskID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tbl_activitysubmission");

            migrationBuilder.DropTable(
                name: "tbl_criteria");

            migrationBuilder.DropTable(
                name: "tbl_taskassignment");

            migrationBuilder.DropTable(
                name: "tbl_taskactivity");

            migrationBuilder.DropTable(
                name: "tbl_employees");

            migrationBuilder.DropTable(
                name: "tbl_officetask");

            migrationBuilder.DropTable(
                name: "tbl_departments");
        }
    }
}
