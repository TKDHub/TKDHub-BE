using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dojo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentEndDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "EndDate",
                table: "Students",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "Students");
        }
    }
}
