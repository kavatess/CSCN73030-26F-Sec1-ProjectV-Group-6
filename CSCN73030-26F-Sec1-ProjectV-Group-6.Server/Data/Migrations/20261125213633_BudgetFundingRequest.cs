using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class BudgetFundingRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "FundingAmount",
                table: "Requisition",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FundingCheckedAtUtc",
                table: "Requisition",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FundingDecidedAtUtc",
                table: "Requisition",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FundingRequestId",
                table: "Requisition",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FundingRequestedAtUtc",
                table: "Requisition",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "FundingUrgent",
                table: "Requisition",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Requisition_FundingRequestId",
                table: "Requisition",
                column: "FundingRequestId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Requisition_FundingRequestId",
                table: "Requisition");

            migrationBuilder.DropColumn(
                name: "FundingAmount",
                table: "Requisition");

            migrationBuilder.DropColumn(
                name: "FundingCheckedAtUtc",
                table: "Requisition");

            migrationBuilder.DropColumn(
                name: "FundingDecidedAtUtc",
                table: "Requisition");

            migrationBuilder.DropColumn(
                name: "FundingRequestId",
                table: "Requisition");

            migrationBuilder.DropColumn(
                name: "FundingRequestedAtUtc",
                table: "Requisition");

            migrationBuilder.DropColumn(
                name: "FundingUrgent",
                table: "Requisition");
        }
    }
}
