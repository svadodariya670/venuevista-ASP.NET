using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VenueBooking.Migrations
{
    /// <inheritdoc />
    public partial class AddIsCoverToThemeImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsCover",
                table: "ThemeImages",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsCover",
                table: "ThemeImages");
        }
    }
}
