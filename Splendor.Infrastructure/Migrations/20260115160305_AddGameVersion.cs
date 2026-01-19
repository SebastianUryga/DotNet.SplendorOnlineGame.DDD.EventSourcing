using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Splendor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGameVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "GameViews",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Version",
                table: "GameViews");
        }
    }
}
