using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhotoVideoBackupAPI.Migrations
{
    /// <inheritdoc />
    public partial class RemoveIMEIColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop indexes only if they exist
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM pg_indexes 
                        WHERE indexname = 'IX_MediaItems_IMEI' 
                        AND tablename = 'MediaItems'
                    ) THEN
                        DROP INDEX ""IX_MediaItems_IMEI"";
                    END IF;
                END $$;
            ");

            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM pg_indexes 
                        WHERE indexname = 'IX_BackupSessions_IMEI' 
                        AND tablename = 'BackupSessions'
                    ) THEN
                        DROP INDEX ""IX_BackupSessions_IMEI"";
                    END IF;
                END $$;
            ");

            // Drop columns only if they exist
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'MediaItems' 
                        AND column_name = 'IMEI'
                    ) THEN
                        ALTER TABLE ""MediaItems"" DROP COLUMN ""IMEI"";
                    END IF;
                END $$;
            ");

            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'BackupSessions' 
                        AND column_name = 'IMEI'
                    ) THEN
                        ALTER TABLE ""BackupSessions"" DROP COLUMN ""IMEI"";
                    END IF;
                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IMEI",
                table: "MediaItems",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IMEI",
                table: "BackupSessions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MediaItems_IMEI",
                table: "MediaItems",
                column: "IMEI");

            migrationBuilder.CreateIndex(
                name: "IX_BackupSessions_IMEI",
                table: "BackupSessions",
                column: "IMEI");
        }
    }
}
