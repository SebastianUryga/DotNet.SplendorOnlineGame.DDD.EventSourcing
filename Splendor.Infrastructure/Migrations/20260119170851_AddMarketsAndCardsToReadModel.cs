using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Splendor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMarketsAndCardsToReadModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OwnedCardIds",
                table: "PlayerViews",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Deck1Count",
                table: "GameViews",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Deck2Count",
                table: "GameViews",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Deck3Count",
                table: "GameViews",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Market1",
                table: "GameViews",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Market2",
                table: "GameViews",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Market3",
                table: "GameViews",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OwnedCardIds",
                table: "PlayerViews");

            migrationBuilder.DropColumn(
                name: "Deck1Count",
                table: "GameViews");

            migrationBuilder.DropColumn(
                name: "Deck2Count",
                table: "GameViews");

            migrationBuilder.DropColumn(
                name: "Deck3Count",
                table: "GameViews");

            migrationBuilder.DropColumn(
                name: "Market1",
                table: "GameViews");

            migrationBuilder.DropColumn(
                name: "Market2",
                table: "GameViews");

            migrationBuilder.DropColumn(
                name: "Market3",
                table: "GameViews");
        }
    }
}
