using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dojo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentFreeze : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FrozenByEmail",
                table: "Students",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FrozenByName",
                table: "Students",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "FrozenOn",
                table: "Students",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RemainingDurationDays",
                table: "Students",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FrozenByEmail",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "FrozenByName",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "FrozenOn",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "RemainingDurationDays",
                table: "Students");
        }
    }
}
