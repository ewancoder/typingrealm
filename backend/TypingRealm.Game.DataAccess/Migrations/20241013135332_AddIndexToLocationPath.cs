using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TypingRealm.Game.DataAccess.Migrations;

/// <inheritdoc />
public partial class AddIndexToLocationPath : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "ix_location_path",
            table: "location",
            column: "path");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ix_location_path",
            table: "location");
    }
}
