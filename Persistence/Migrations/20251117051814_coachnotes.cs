using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class coachnotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Attendances_Users_ApplicationUserId",
                table: "Attendances");

            migrationBuilder.DropForeignKey(
                name: "FK_ClassEnrollments_Classes_ClassId",
                table: "ClassEnrollments");

            migrationBuilder.DropForeignKey(
                name: "FK_ClassEnrollments_Users_ApplicationUserId",
                table: "ClassEnrollments");

            migrationBuilder.DropForeignKey(
                name: "FK_ClassEnrollments_Users_UserId",
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

            migrationBuilder.AddColumn<string>(
                name: "CoachNote",
                table: "Classes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Info",
                table: "Classes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NoteLastUpdated",
                table: "Classes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ClassEnrollments_Classes_ClassId",
                table: "ClassEnrollments",
                column: "ClassId",
                principalTable: "Classes",
                principalColumn: "ClassId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ClassEnrollments_Users_UserId",
                table: "ClassEnrollments",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClassEnrollments_Classes_ClassId",
                table: "ClassEnrollments");

            migrationBuilder.DropForeignKey(
                name: "FK_ClassEnrollments_Users_UserId",
                table: "ClassEnrollments");

            migrationBuilder.DropColumn(
                name: "CoachNote",
                table: "Classes");

            migrationBuilder.DropColumn(
                name: "Info",
                table: "Classes");

            migrationBuilder.DropColumn(
                name: "NoteLastUpdated",
                table: "Classes");

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
                name: "FK_ClassEnrollments_Classes_ClassId",
                table: "ClassEnrollments",
                column: "ClassId",
                principalTable: "Classes",
                principalColumn: "ClassId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ClassEnrollments_Users_ApplicationUserId",
                table: "ClassEnrollments",
                column: "ApplicationUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ClassEnrollments_Users_UserId",
                table: "ClassEnrollments",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
