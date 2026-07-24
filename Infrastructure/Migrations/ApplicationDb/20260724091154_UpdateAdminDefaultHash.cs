using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Eshop.Infrastructure.Migrations.ApplicationDb
{
    /// <inheritdoc />
    public partial class UpdateAdminDefaultHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$OPxsghdegmQpE7W5Vm9weuKIafu4GGVyh5E4W.Hkr2gdWrTdzvpqm");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$fX1Z7.h2bXQenQ/K3d0fbeU3Zp7Z7WkO8/j7YAnF.gXjbe5Q2WdmG");
        }
    }
}
