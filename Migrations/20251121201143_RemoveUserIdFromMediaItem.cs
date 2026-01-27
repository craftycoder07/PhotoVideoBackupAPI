using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhotoVideoBackupAPI.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUserIdFromMediaItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // First, ensure all MediaItems have SessionId
            // If any MediaItems exist without SessionId, we need to handle them
            migrationBuilder.Sql(@"
                DO $$
                DECLARE
                    orphaned_count INTEGER;
                BEGIN
                    SELECT COUNT(*) INTO orphaned_count
                    FROM ""MediaItems""
                    WHERE ""SessionId"" IS NULL;
                    
                    IF orphaned_count > 0 THEN
                        RAISE EXCEPTION 'Cannot proceed: % MediaItems exist without SessionId. Please assign SessionId to all MediaItems before running this migration.', orphaned_count;
                    END IF;
                END $$;
            ");

            // Drop foreign key and index for UserId
            migrationBuilder.DropForeignKey(
                name: "FK_MediaItems_Users_UserId",
                table: "MediaItems");

            migrationBuilder.DropIndex(
                name: "IX_MediaItems_UserId",
                table: "MediaItems");

            // Remove UserId column
            migrationBuilder.DropColumn(
                name: "UserId",
                table: "MediaItems");

            // Make SessionId required (not nullable)
            migrationBuilder.AlterColumn<string>(
                name: "SessionId",
                table: "MediaItems",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "SessionId",
                table: "MediaItems",
                type: "character varying(50)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "MediaItems",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_MediaItems_UserId",
                table: "MediaItems",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_MediaItems_Users_UserId",
                table: "MediaItems",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
