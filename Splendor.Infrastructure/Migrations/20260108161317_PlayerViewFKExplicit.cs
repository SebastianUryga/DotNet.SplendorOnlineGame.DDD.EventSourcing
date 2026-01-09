using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Splendor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PlayerViewFKExplicit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlayerViews_GameViews_GameViewId",
                table: "PlayerViews");

            migrationBuilder.AlterColumn<Guid>(
                name: "GameViewId",
                table: "PlayerViews",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerViews_GameViews_GameViewId",
                table: "PlayerViews",
                column: "GameViewId",
                principalTable: "GameViews",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlayerViews_GameViews_GameViewId",
                table: "PlayerViews");

            migrationBuilder.AlterColumn<Guid>(
                name: "GameViewId",
                table: "PlayerViews",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerViews_GameViews_GameViewId",
                table: "PlayerViews",
                column: "GameViewId",
                principalTable: "GameViews",
                principalColumn: "Id");
        }
    }
}
