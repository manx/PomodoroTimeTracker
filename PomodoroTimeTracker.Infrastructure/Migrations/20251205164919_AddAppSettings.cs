using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PomodoroTimeTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAppSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LastOpenedSettingsTab = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    LanguageOverride = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    DateFormatOverride = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    WeekStartDay = table.Column<int>(type: "INTEGER", nullable: false),
                    WeekYearStandard = table.Column<int>(type: "INTEGER", nullable: false),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSettings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppSettings");
        }
    }
}
