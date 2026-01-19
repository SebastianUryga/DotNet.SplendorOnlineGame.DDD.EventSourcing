using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Splendor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeparatedOwnerAndPlayerIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OwnerId",
                table: "PlayerViews",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "PlayerViews");
        }
    }
}
