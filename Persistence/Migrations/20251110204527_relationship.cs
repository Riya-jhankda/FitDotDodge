using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class relationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApplicationUserId",
                table: "ClassEnrollments",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApplicationUserId",
                table: "Attendances",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClassEnrollments_ApplicationUserId",
                table: "ClassEnrollments",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_ApplicationUserId",
                table: "Attendances",
                column: "ApplicationUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Attendances_Users_ApplicationUserId",
                table: "Attendances",
                column: "ApplicationUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ClassEnrollments_Users_ApplicationUserId",
                table: "ClassEnrollments",
                column: "ApplicationUserId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Attendances_Users_ApplicationUserId",
                table: "Attendances");

            migrationBuilder.DropForeignKey(
                name: "FK_ClassEnrollments_Users_ApplicationUserId",
                table: "ClassEnrollments");

            migrationBuilder.DropIndex(
                name: "IX_ClassEnrollments_ApplicationUserId",
                table: "ClassEnrollments");

            migrationBuilder.DropIndex(
                name: "IX_Attendances_ApplicationUserId",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "ClassEnrollments");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "Attendances");
        }
    }
}
