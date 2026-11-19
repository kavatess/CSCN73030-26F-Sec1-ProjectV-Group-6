using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "RequisitionNumberSequence");

            migrationBuilder.CreateSequence(
                name: "StaffNumberSequence");

            migrationBuilder.CreateTable(
                name: "AuditLog",
                columns: table => new
                {
                    AuditId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EntityType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ActorId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TimestampUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLog", x => x.AuditId);
                });

            migrationBuilder.CreateTable(
                name: "CredentialType",
                columns: table => new
                {
                    CredentialTypeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsDutyBlocking = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CredentialType", x => x.CredentialTypeId);
                });

            migrationBuilder.CreateTable(
                name: "Department",
                columns: table => new
                {
                    DepartmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Department", x => x.DepartmentId);
                });

            migrationBuilder.CreateTable(
                name: "OutboxEvent",
                columns: table => new
                {
                    EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SchemaVersion = table.Column<int>(type: "int", nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PublishedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxEvent", x => x.EventId);
                });

            migrationBuilder.CreateTable(
                name: "Service",
                columns: table => new
                {
                    ServiceId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DepartmentId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Service", x => x.ServiceId);
                    table.ForeignKey(
                        name: "FK_Service_Department_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Department",
                        principalColumn: "DepartmentId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Staff",
                columns: table => new
                {
                    StaffId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StaffNumber = table.Column<string>(type: "nchar(11)", fixedLength: true, maxLength: 11, nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PreferredName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    CitizenId = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    HireDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EmploymentStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    EmploymentType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    HomeDepartmentId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Staff", x => x.StaffId);
                    table.ForeignKey(
                        name: "FK_Staff_Department_HomeDepartmentId",
                        column: x => x.HomeDepartmentId,
                        principalTable: "Department",
                        principalColumn: "DepartmentId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Compensation",
                columns: table => new
                {
                    CompensationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StaffId = table.Column<int>(type: "int", nullable: false),
                    PayBand = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AnnualSalary = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Compensation", x => x.CompensationId);
                    table.CheckConstraint("CK_Compensation_Dates", "[EffectiveTo] IS NULL OR [EffectiveFrom] < [EffectiveTo]");
                    table.ForeignKey(
                        name: "FK_Compensation_Staff_StaffId",
                        column: x => x.StaffId,
                        principalTable: "Staff",
                        principalColumn: "StaffId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Credential",
                columns: table => new
                {
                    CredentialId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StaffId = table.Column<int>(type: "int", nullable: false),
                    CredentialTypeId = table.Column<int>(type: "int", nullable: false),
                    Number = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IssueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ExpiryDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Credential", x => x.CredentialId);
                    table.CheckConstraint("CK_Credential_Dates", "[IssueDate] < [ExpiryDate]");
                    table.ForeignKey(
                        name: "FK_Credential_CredentialType_CredentialTypeId",
                        column: x => x.CredentialTypeId,
                        principalTable: "CredentialType",
                        principalColumn: "CredentialTypeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Credential_Staff_StaffId",
                        column: x => x.StaffId,
                        principalTable: "Staff",
                        principalColumn: "StaffId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Requisition",
                columns: table => new
                {
                    RequisitionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequisitionNumber = table.Column<string>(type: "nchar(11)", fixedLength: true, maxLength: 11, nullable: false),
                    RequestingDeptId = table.Column<int>(type: "int", nullable: false),
                    StaffId = table.Column<int>(type: "int", nullable: true),
                    Type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RoleTitle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Positions = table.Column<int>(type: "int", nullable: false),
                    PositionsFilled = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ReasonCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    BudgetDecision = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Requisition", x => x.RequisitionId);
                    table.CheckConstraint("CK_Requisition_Positions", "[Positions] > 0");
                    table.CheckConstraint("CK_Requisition_PositionsFilled", "[PositionsFilled] >= 0 AND [PositionsFilled] <= [Positions]");
                    table.ForeignKey(
                        name: "FK_Requisition_Department_RequestingDeptId",
                        column: x => x.RequestingDeptId,
                        principalTable: "Department",
                        principalColumn: "DepartmentId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Requisition_Staff_StaffId",
                        column: x => x.StaffId,
                        principalTable: "Staff",
                        principalColumn: "StaffId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RosterEntry",
                columns: table => new
                {
                    RosterEntryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StaffId = table.Column<int>(type: "int", nullable: false),
                    ServiceId = table.Column<int>(type: "int", nullable: false),
                    ShiftStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ShiftEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ShiftType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RosterEntry", x => x.RosterEntryId);
                    table.CheckConstraint("CK_RosterEntry_Shift", "[ShiftStart] < [ShiftEnd]");
                    table.ForeignKey(
                        name: "FK_RosterEntry_Service_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Service",
                        principalColumn: "ServiceId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RosterEntry_Staff_StaffId",
                        column: x => x.StaffId,
                        principalTable: "Staff",
                        principalColumn: "StaffId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SecurityFlag",
                columns: table => new
                {
                    FlagId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StaffId = table.Column<int>(type: "int", nullable: false),
                    FlagType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecurityFlag", x => x.FlagId);
                    table.ForeignKey(
                        name: "FK_SecurityFlag_Staff_StaffId",
                        column: x => x.StaffId,
                        principalTable: "Staff",
                        principalColumn: "StaffId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StaffAssignment",
                columns: table => new
                {
                    AssignmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StaffId = table.Column<int>(type: "int", nullable: false),
                    ServiceId = table.Column<int>(type: "int", nullable: false),
                    RoleTitle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffAssignment", x => x.AssignmentId);
                    table.CheckConstraint("CK_StaffAssignment_Dates", "[EndDate] IS NULL OR [StartDate] < [EndDate]");
                    table.ForeignKey(
                        name: "FK_StaffAssignment_Service_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Service",
                        principalColumn: "ServiceId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StaffAssignment_Staff_StaffId",
                        column: x => x.StaffId,
                        principalTable: "Staff",
                        principalColumn: "StaffId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_EntityType_EntityId",
                table: "AuditLog",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_Compensation_StaffId",
                table: "Compensation",
                column: "StaffId");

            migrationBuilder.CreateIndex(
                name: "IX_Credential_CredentialTypeId",
                table: "Credential",
                column: "CredentialTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Credential_ExpiryDate",
                table: "Credential",
                column: "ExpiryDate");

            migrationBuilder.CreateIndex(
                name: "IX_Credential_StaffId",
                table: "Credential",
                column: "StaffId");

            migrationBuilder.CreateIndex(
                name: "IX_CredentialType_Name",
                table: "CredentialType",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Department_Code",
                table: "Department",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxEvent_PublishedAtUtc",
                table: "OutboxEvent",
                column: "PublishedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Requisition_IdempotencyKey",
                table: "Requisition",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Requisition_RequestingDeptId",
                table: "Requisition",
                column: "RequestingDeptId");

            migrationBuilder.CreateIndex(
                name: "IX_Requisition_RequisitionNumber",
                table: "Requisition",
                column: "RequisitionNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Requisition_StaffId",
                table: "Requisition",
                column: "StaffId");

            migrationBuilder.CreateIndex(
                name: "IX_RosterEntry_ServiceId_ShiftStart",
                table: "RosterEntry",
                columns: new[] { "ServiceId", "ShiftStart" });

            migrationBuilder.CreateIndex(
                name: "IX_RosterEntry_StaffId_ShiftStart",
                table: "RosterEntry",
                columns: new[] { "StaffId", "ShiftStart" });

            migrationBuilder.CreateIndex(
                name: "IX_SecurityFlag_StaffId",
                table: "SecurityFlag",
                column: "StaffId");

            migrationBuilder.CreateIndex(
                name: "IX_Service_DepartmentId_Name",
                table: "Service",
                columns: new[] { "DepartmentId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Staff_Email",
                table: "Staff",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Staff_HomeDepartmentId",
                table: "Staff",
                column: "HomeDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Staff_LastName_FirstName",
                table: "Staff",
                columns: new[] { "LastName", "FirstName" });

            migrationBuilder.CreateIndex(
                name: "IX_Staff_StaffNumber",
                table: "Staff",
                column: "StaffNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StaffAssignment_ServiceId",
                table: "StaffAssignment",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffAssignment_StaffId",
                table: "StaffAssignment",
                column: "StaffId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLog");

            migrationBuilder.DropTable(
                name: "Compensation");

            migrationBuilder.DropTable(
                name: "Credential");

            migrationBuilder.DropTable(
                name: "OutboxEvent");

            migrationBuilder.DropTable(
                name: "Requisition");

            migrationBuilder.DropTable(
                name: "RosterEntry");

            migrationBuilder.DropTable(
                name: "SecurityFlag");

            migrationBuilder.DropTable(
                name: "StaffAssignment");

            migrationBuilder.DropTable(
                name: "CredentialType");

            migrationBuilder.DropTable(
                name: "Service");

            migrationBuilder.DropTable(
                name: "Staff");

            migrationBuilder.DropTable(
                name: "Department");

            migrationBuilder.DropSequence(
                name: "RequisitionNumberSequence");

            migrationBuilder.DropSequence(
                name: "StaffNumberSequence");
        }
    }
}
