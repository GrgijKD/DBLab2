using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibraryInfrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ConfigureWorkerUserRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Workers_ApplicationUserId",
                table: "Workers",
                column: "ApplicationUserId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Workers_AspNetUsers_ApplicationUserId",
                table: "Workers",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Workers_AspNetUsers_ApplicationUserId",
                table: "Workers");

            migrationBuilder.DropIndex(
                name: "IX_Workers_ApplicationUserId",
                table: "Workers");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "Workers");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DueDate",
                table: "BookReservations",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);
        }
    }
}
