using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class PayrollStatements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PayrollStatement",
                columns: table => new
                {
                    PayrollStatementId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Period = table.Column<string>(type: "nchar(7)", fixedLength: true, maxLength: 7, nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: false),
                    Headcount = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Accepted = table.Column<bool>(type: "bit", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ExpenseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RemainingBudget = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    SentAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SentBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollStatement", x => x.PayrollStatementId);
                    table.CheckConstraint("CK_PayrollStatement_Amount", "[Amount] >= 0");
                    table.ForeignKey(
                        name: "FK_PayrollStatement_Department_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Department",
                        principalColumn: "DepartmentId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollStatement_DepartmentId",
                table: "PayrollStatement",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollStatement_Period_DepartmentId",
                table: "PayrollStatement",
                columns: new[] { "Period", "DepartmentId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PayrollStatement");
        }
    }
}
