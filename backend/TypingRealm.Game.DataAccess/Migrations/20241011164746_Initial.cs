using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TypingRealm.Game.DataAccess.Migrations;

/// <inheritdoc />
public partial class Initial : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "character",
            columns: table => new
            {
                id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                profile_id = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                level = table.Column<int>(type: "integer", nullable: false),
                experience = table.Column<long>(type: "bigint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_character", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_character_profile_id",
            table: "character",
            column: "profile_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "character");
    }
}
