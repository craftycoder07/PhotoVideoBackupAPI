using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhotoVideoBackupAPI.WebApi.Migrations
{
    /// <inheritdoc />
    public partial class AddStatsColumnToUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Stats column was already added to Users in RemoveDeviceTableAndAddIMEI migration.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Stats column is managed by RemoveDeviceTableAndAddIMEI migration.
        }
    }
}
