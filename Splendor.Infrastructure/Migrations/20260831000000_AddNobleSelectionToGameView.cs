using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Splendor.Infrastructure.Persistence;

#nullable disable

namespace Splendor.Infrastructure.Migrations;

[DbContext(typeof(ReadModelsContext))]
[Migration("20260831000000_AddNobleSelectionToGameView")]
public partial class AddNobleSelectionToGameView : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "EligibleNobleIds",
            table: "GameViews",
            type: "nvarchar(max)",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "PlayerIdAwaitingNobleSelection",
            table: "GameViews",
            type: "nvarchar(max)",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "EligibleNobleIds",
            table: "GameViews");

        migrationBuilder.DropColumn(
            name: "PlayerIdAwaitingNobleSelection",
            table: "GameViews");
    }
}
