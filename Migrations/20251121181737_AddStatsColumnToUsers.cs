using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhotoVideoBackupAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddStatsColumnToUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Stats",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "");

            // Set default empty Stats for all existing users
            migrationBuilder.Sql(@"
                UPDATE ""Users""
                SET ""Stats"" = '{}'
                WHERE ""Stats"" IS NULL OR ""Stats"" = '';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Stats",
                table: "Users");
        }
    }
}
