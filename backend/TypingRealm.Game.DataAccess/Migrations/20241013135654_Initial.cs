using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TypingRealm.Game.DataAccess.Migrations;

/// <inheritdoc />
public partial class Initial : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "location",
            columns: table => new
            {
                id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                description = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_location", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "location_path",
            columns: table => new
            {
                id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                from_location_id = table.Column<string>(type: "character varying(50)", nullable: false),
                to_location_id = table.Column<string>(type: "character varying(50)", nullable: false),
                distance_marks = table.Column<long>(type: "bigint", nullable: false),
                name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                description = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_location_path", x => x.id);
                table.ForeignKey(
                    name: "fk_location_path_location_from_location_id",
                    column: x => x.from_location_id,
                    principalTable: "location",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_location_path_location_to_location_id",
                    column: x => x.to_location_id,
                    principalTable: "location",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "asset",
            columns: table => new
            {
                id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                type = table.Column<int>(type: "integer", nullable: false),
                path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                file_path = table.Column<string>(type: "text", nullable: false),
                location_id = table.Column<string>(type: "character varying(50)", nullable: true),
                location_path_id = table.Column<long>(type: "bigint", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_asset", x => x.id);
                table.ForeignKey(
                    name: "fk_asset_location_location_id",
                    column: x => x.location_id,
                    principalTable: "location",
                    principalColumn: "id");
                table.ForeignKey(
                    name: "fk_asset_location_path_location_path_id",
                    column: x => x.location_path_id,
                    principalTable: "location_path",
                    principalColumn: "id");
            });

        migrationBuilder.CreateTable(
            name: "character",
            columns: table => new
            {
                id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                profile_id = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                level = table.Column<int>(type: "integer", nullable: false),
                experience = table.Column<long>(type: "bigint", nullable: false),
                location_id = table.Column<string>(type: "character varying(50)", nullable: false),
                movement_progress_location_path_id = table.Column<long>(type: "bigint", nullable: true),
                movement_progress_distance_marks = table.Column<long>(type: "bigint", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_character", x => x.id);
                table.ForeignKey(
                    name: "fk_character_location_location_id",
                    column: x => x.location_id,
                    principalTable: "location",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_character_location_path_movement_progress_location_path_id",
                    column: x => x.movement_progress_location_path_id,
                    principalTable: "location_path",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_asset_location_id",
            table: "asset",
            column: "location_id");

        migrationBuilder.CreateIndex(
            name: "ix_asset_location_path_id",
            table: "asset",
            column: "location_path_id");

        migrationBuilder.CreateIndex(
            name: "ix_asset_path",
            table: "asset",
            column: "path");

        migrationBuilder.CreateIndex(
            name: "ix_character_location_id",
            table: "character",
            column: "location_id");

        migrationBuilder.CreateIndex(
            name: "ix_character_movement_progress_location_path_id",
            table: "character",
            column: "movement_progress_location_path_id");

        migrationBuilder.CreateIndex(
            name: "ix_character_profile_id",
            table: "character",
            column: "profile_id");

        migrationBuilder.CreateIndex(
            name: "ix_location_path",
            table: "location",
            column: "path");

        migrationBuilder.CreateIndex(
            name: "ix_location_path_from_location_id",
            table: "location_path",
            column: "from_location_id");

        migrationBuilder.CreateIndex(
            name: "ix_location_path_to_location_id",
            table: "location_path",
            column: "to_location_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "asset");

        migrationBuilder.DropTable(
            name: "character");

        migrationBuilder.DropTable(
            name: "location_path");

        migrationBuilder.DropTable(
            name: "location");
    }
}
