using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TypingRealm.Game.DataAccess.Migrations;

/// <inheritdoc />
public partial class AddDescriptionAndAssetsToPath : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "description",
            table: "location_path",
            type: "character varying(10000)",
            maxLength: 10000,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "name",
            table: "location_path",
            type: "character varying(100)",
            maxLength: 100,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<long>(
            name: "location_path_id",
            table: "asset",
            type: "bigint",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "ix_asset_location_path_id",
            table: "asset",
            column: "location_path_id");

        migrationBuilder.AddForeignKey(
            name: "fk_asset_location_path_location_path_id",
            table: "asset",
            column: "location_path_id",
            principalTable: "location_path",
            principalColumn: "id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "fk_asset_location_path_location_path_id",
            table: "asset");

        migrationBuilder.DropIndex(
            name: "ix_asset_location_path_id",
            table: "asset");

        migrationBuilder.DropColumn(
            name: "description",
            table: "location_path");

        migrationBuilder.DropColumn(
            name: "name",
            table: "location_path");

        migrationBuilder.DropColumn(
            name: "location_path_id",
            table: "asset");
    }
}
