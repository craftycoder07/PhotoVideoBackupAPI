using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhotoVideoBackupAPI.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDeviceTableAndAddIMEI : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // First, add the new columns to Users table
            migrationBuilder.AddColumn<string>(
                name: "ApiKey",
                table: "Users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSeen",
                table: "Users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "RegisteredDate",
                table: "Users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Settings",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Stats",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "");

            // Now migrate device data to users before dropping the table
            // For users with multiple devices, use the most recent device's data
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'Devices') THEN
                        UPDATE ""Users""
                        SET 
                            ""ApiKey"" = COALESCE(
                                (SELECT ""ApiKey"" FROM ""Devices"" 
                                 WHERE ""Devices"".""UserId"" = ""Users"".""Id"" 
                                 ORDER BY ""LastSeen"" DESC LIMIT 1),
                                ""ApiKey""
                            ),
                            ""RegisteredDate"" = COALESCE(
                                (SELECT ""RegisteredDate"" FROM ""Devices"" 
                                 WHERE ""Devices"".""UserId"" = ""Users"".""Id"" 
                                 ORDER BY ""RegisteredDate"" ASC LIMIT 1),
                                ""CreatedAt""
                            ),
                            ""LastSeen"" = COALESCE(
                                (SELECT MAX(""LastSeen"") FROM ""Devices"" 
                                 WHERE ""Devices"".""UserId"" = ""Users"".""Id""),
                                ""LastLoginAt""
                            ),
                            ""Settings"" = COALESCE(
                                (SELECT ""Settings"" FROM ""Devices"" 
                                 WHERE ""Devices"".""UserId"" = ""Users"".""Id"" 
                                 ORDER BY ""LastSeen"" DESC LIMIT 1),
                                '{}'
                            ),
                            ""Stats"" = COALESCE(
                                (SELECT ""Stats"" FROM ""Devices"" 
                                 WHERE ""Devices"".""UserId"" = ""Users"".""Id"" 
                                 ORDER BY ""LastSeen"" DESC LIMIT 1),
                                '{}'
                            )
                        WHERE EXISTS (SELECT 1 FROM ""Devices"" WHERE ""Devices"".""UserId"" = ""Users"".""Id"");
                    END IF;
                END $$;
            ");

            // Set default values for users without devices
            migrationBuilder.Sql(@"
                UPDATE ""Users""
                SET 
                    ""RegisteredDate"" = ""CreatedAt"",
                    ""LastSeen"" = ""LastLoginAt"",
                    ""Settings"" = '{}',
                    ""Stats"" = '{}'
                WHERE ""RegisteredDate"" = '0001-01-01 00:00:00+00';
            ");

            // Now drop foreign keys and the Devices table
            migrationBuilder.DropForeignKey(
                name: "FK_BackupSessions_Devices_DeviceId",
                table: "BackupSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_MediaItems_Devices_DeviceId",
                table: "MediaItems");

            migrationBuilder.DropTable(
                name: "Devices");

            migrationBuilder.DropIndex(
                name: "IX_MediaItems_DeviceId",
                table: "MediaItems");

            migrationBuilder.DropIndex(
                name: "IX_BackupSessions_DeviceId",
                table: "BackupSessions");

            migrationBuilder.DropColumn(
                name: "DeviceId",
                table: "MediaItems");

            migrationBuilder.DropColumn(
                name: "DeviceId",
                table: "BackupSessions");

            migrationBuilder.CreateIndex(
                name: "IX_Users_ApiKey",
                table: "Users",
                column: "ApiKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_ApiKey",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ApiKey",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastSeen",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RegisteredDate",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Settings",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Stats",
                table: "Users");

            migrationBuilder.AddColumn<string>(
                name: "DeviceId",
                table: "MediaItems",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DeviceId",
                table: "BackupSessions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Devices",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    UserId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ApiKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DeviceId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DeviceModel = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DeviceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LastSeen = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RegisteredDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Settings = table.Column<string>(type: "text", nullable: false),
                    Stats = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Devices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Devices_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MediaItems_DeviceId",
                table: "MediaItems",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_BackupSessions_DeviceId",
                table: "BackupSessions",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_Devices_ApiKey",
                table: "Devices",
                column: "ApiKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Devices_DeviceId",
                table: "Devices",
                column: "DeviceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Devices_UserId",
                table: "Devices",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_BackupSessions_Devices_DeviceId",
                table: "BackupSessions",
                column: "DeviceId",
                principalTable: "Devices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MediaItems_Devices_DeviceId",
                table: "MediaItems",
                column: "DeviceId",
                principalTable: "Devices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
