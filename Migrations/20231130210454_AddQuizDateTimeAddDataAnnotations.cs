using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimpleMathQuizzes.Migrations
{
    /// <inheritdoc />
    public partial class AddQuizDateTimeAddDataAnnotations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreationDateTime",
                table: "Quizzes",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreationDateTime",
                table: "Quizzes");
        }
    }
}
