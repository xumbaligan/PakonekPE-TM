using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TM_PE.Migrations
{
    /// <inheritdoc />
    public partial class user3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Weight",
                table: "tbl_criteria",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "tbl_performanceevaluation",
                columns: table => new
                {
                    EvaluationID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeID = table.Column<int>(type: "int", nullable: false),
                    EvaluatorName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EvaluationPeriod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EvaluationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OverallScore = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    OverallRating = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    GeneralRemarks = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    EvaluationStatus = table.Column<string>(type: "nvarchar(20)", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_performanceevaluation", x => x.EvaluationID);
                    table.ForeignKey(
                        name: "FK_tbl_performanceevaluation_tbl_employees_EmployeeID",
                        column: x => x.EmployeeID,
                        principalTable: "tbl_employees",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tbl_appraisal",
                columns: table => new
                {
                    AppraisalID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeID = table.Column<int>(type: "int", nullable: false),
                    EvaluationID = table.Column<int>(type: "int", nullable: false),
                    AppraisalDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OverallRating = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Recommendation = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    SalaryAdjustmentRecommendation = table.Column<bool>(type: "bit", nullable: false),
                    PromotionRecommendation = table.Column<bool>(type: "bit", nullable: false),
                    TrainingRecommendation = table.Column<bool>(type: "bit", nullable: false),
                    ManagerRemarks = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AppraisalStatus = table.Column<string>(type: "nvarchar(20)", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_appraisal", x => x.AppraisalID);
                    table.ForeignKey(
                        name: "FK_tbl_appraisal_tbl_employees_EmployeeID",
                        column: x => x.EmployeeID,
                        principalTable: "tbl_employees",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tbl_appraisal_tbl_performanceevaluation_EvaluationID",
                        column: x => x.EvaluationID,
                        principalTable: "tbl_performanceevaluation",
                        principalColumn: "EvaluationID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tbl_evaluationresult",
                columns: table => new
                {
                    EvaluationResultID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EvaluationID = table.Column<int>(type: "int", nullable: false),
                    CriteriaID = table.Column<int>(type: "int", nullable: false),
                    Score = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_evaluationresult", x => x.EvaluationResultID);
                    table.ForeignKey(
                        name: "FK_tbl_evaluationresult_tbl_criteria_CriteriaID",
                        column: x => x.CriteriaID,
                        principalTable: "tbl_criteria",
                        principalColumn: "CriteriaId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tbl_evaluationresult_tbl_performanceevaluation_EvaluationID",
                        column: x => x.EvaluationID,
                        principalTable: "tbl_performanceevaluation",
                        principalColumn: "EvaluationID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tbl_feedback",
                columns: table => new
                {
                    FeedbackID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeID = table.Column<int>(type: "int", nullable: false),
                    SubmittedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EvaluationID = table.Column<int>(type: "int", nullable: true),
                    FeedbackType = table.Column<string>(type: "nvarchar(20)", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_feedback", x => x.FeedbackID);
                    table.ForeignKey(
                        name: "FK_tbl_feedback_tbl_employees_EmployeeID",
                        column: x => x.EmployeeID,
                        principalTable: "tbl_employees",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tbl_feedback_tbl_performanceevaluation_EvaluationID",
                        column: x => x.EvaluationID,
                        principalTable: "tbl_performanceevaluation",
                        principalColumn: "EvaluationID",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_appraisal_EmployeeID",
                table: "tbl_appraisal",
                column: "EmployeeID");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_appraisal_EvaluationID",
                table: "tbl_appraisal",
                column: "EvaluationID");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_evaluationresult_CriteriaID",
                table: "tbl_evaluationresult",
                column: "CriteriaID");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_evaluationresult_EvaluationID",
                table: "tbl_evaluationresult",
                column: "EvaluationID");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_feedback_EmployeeID",
                table: "tbl_feedback",
                column: "EmployeeID");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_feedback_EvaluationID",
                table: "tbl_feedback",
                column: "EvaluationID");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_performanceevaluation_EmployeeID",
                table: "tbl_performanceevaluation",
                column: "EmployeeID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tbl_appraisal");

            migrationBuilder.DropTable(
                name: "tbl_evaluationresult");

            migrationBuilder.DropTable(
                name: "tbl_feedback");

            migrationBuilder.DropTable(
                name: "tbl_performanceevaluation");

            migrationBuilder.DropColumn(
                name: "Weight",
                table: "tbl_criteria");
        }
    }
}
