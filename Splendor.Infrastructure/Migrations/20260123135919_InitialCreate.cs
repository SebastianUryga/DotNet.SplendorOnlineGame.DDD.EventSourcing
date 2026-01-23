using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Splendor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GameViews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MarketGems_Diamond = table.Column<int>(type: "int", nullable: false),
                    MarketGems_Sapphire = table.Column<int>(type: "int", nullable: false),
                    MarketGems_Emerald = table.Column<int>(type: "int", nullable: false),
                    MarketGems_Ruby = table.Column<int>(type: "int", nullable: false),
                    MarketGems_Onyx = table.Column<int>(type: "int", nullable: false),
                    MarketGems_Gold = table.Column<int>(type: "int", nullable: false),
                    CurrentPlayerId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Market1 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Market2 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Market3 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Deck1Count = table.Column<int>(type: "int", nullable: false),
                    Deck2Count = table.Column<int>(type: "int", nullable: false),
                    Deck3Count = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameViews", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlayerViews",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    OwnerId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Gems_Diamond = table.Column<int>(type: "int", nullable: false),
                    Gems_Sapphire = table.Column<int>(type: "int", nullable: false),
                    Gems_Emerald = table.Column<int>(type: "int", nullable: false),
                    Gems_Ruby = table.Column<int>(type: "int", nullable: false),
                    Gems_Onyx = table.Column<int>(type: "int", nullable: false),
                    Gems_Gold = table.Column<int>(type: "int", nullable: false),
                    OwnedCardIds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GameViewId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerViews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerViews_GameViews_GameViewId",
                        column: x => x.GameViewId,
                        principalTable: "GameViews",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerViews_GameViewId",
                table: "PlayerViews",
                column: "GameViewId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlayerViews");

            migrationBuilder.DropTable(
                name: "GameViews");
        }
    }
}
