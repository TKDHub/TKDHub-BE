using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dojo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameProfileImageUrl_To_ProfileImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ProfileImageUrl",
                table: "Students",
                newName: "ProfileImage");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ProfileImage",
                table: "Students",
                newName: "ProfileImageUrl");
        }
    }
}
