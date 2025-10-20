using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhotoVideoBackupAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddUserAuthenticationSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "MediaItems",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Devices",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "BackupSessions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Username = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MediaItems_UserId",
                table: "MediaItems",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Devices_UserId",
                table: "Devices",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_BackupSessions_UserId",
                table: "BackupSessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_BackupSessions_Users_UserId",
                table: "BackupSessions",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Devices_Users_UserId",
                table: "Devices",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MediaItems_Users_UserId",
                table: "MediaItems",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BackupSessions_Users_UserId",
                table: "BackupSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_Devices_Users_UserId",
                table: "Devices");

            migrationBuilder.DropForeignKey(
                name: "FK_MediaItems_Users_UserId",
                table: "MediaItems");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropIndex(
                name: "IX_MediaItems_UserId",
                table: "MediaItems");

            migrationBuilder.DropIndex(
                name: "IX_Devices_UserId",
                table: "Devices");

            migrationBuilder.DropIndex(
                name: "IX_BackupSessions_UserId",
                table: "BackupSessions");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "MediaItems");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "BackupSessions");
        }
    }
}
