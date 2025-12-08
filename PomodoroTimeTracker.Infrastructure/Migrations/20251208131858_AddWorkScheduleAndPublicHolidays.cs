using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PomodoroTimeTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkScheduleAndPublicHolidays : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PublicHolidays",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LocalName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    CountryCode = table.Column<string>(type: "TEXT", maxLength: 2, nullable: false),
                    IsGlobal = table.Column<bool>(type: "INTEGER", nullable: false),
                    FetchedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicHolidays", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkSchedules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ClientId = table.Column<int>(type: "INTEGER", nullable: true),
                    ProjectId = table.Column<int>(type: "INTEGER", nullable: true),
                    WorkPercentage = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 100),
                    BaseHoursPerDay = table.Column<decimal>(type: "TEXT", precision: 4, scale: 2, nullable: false, defaultValue: 8.0m),
                    WorkDays = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 62),
                    IncludePublicHolidays = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    CountryCode = table.Column<string>(type: "TEXT", maxLength: 2, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkSchedules", x => x.Id);
                    table.CheckConstraint("CK_WorkSchedule_ClientOrProject", "(ClientId IS NOT NULL AND ProjectId IS NULL) OR (ClientId IS NULL AND ProjectId IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_WorkSchedules_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkSchedules_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PublicHolidays_CountryCode",
                table: "PublicHolidays",
                column: "CountryCode");

            migrationBuilder.CreateIndex(
                name: "IX_PublicHolidays_Date_CountryCode",
                table: "PublicHolidays",
                columns: new[] { "Date", "CountryCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkSchedules_ClientId",
                table: "WorkSchedules",
                column: "ClientId",
                unique: true,
                filter: "ClientId IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_WorkSchedules_ProjectId",
                table: "WorkSchedules",
                column: "ProjectId",
                unique: true,
                filter: "ProjectId IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PublicHolidays");

            migrationBuilder.DropTable(
                name: "WorkSchedules");
        }
    }
}
