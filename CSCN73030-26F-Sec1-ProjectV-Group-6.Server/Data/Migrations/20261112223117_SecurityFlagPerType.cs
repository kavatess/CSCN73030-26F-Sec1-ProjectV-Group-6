using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class SecurityFlagPerType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SecurityFlag_StaffId",
                table: "SecurityFlag");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityFlag_StaffId_FlagType",
                table: "SecurityFlag",
                columns: new[] { "StaffId", "FlagType" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SecurityFlag_StaffId_FlagType",
                table: "SecurityFlag");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityFlag_StaffId",
                table: "SecurityFlag",
                column: "StaffId");
        }
    }
}
