using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IS.DbCommon.Migrations
{
    /// <inheritdoc />
    public partial class SmallChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AiMetadataJson",
                table: "photo_metadata");

            migrationBuilder.DropColumn(
                name: "ExifRawJson",
                table: "photo_metadata");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AiMetadataJson",
                table: "photo_metadata",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExifRawJson",
                table: "photo_metadata",
                type: "text",
                nullable: true);
        }
    }
}
