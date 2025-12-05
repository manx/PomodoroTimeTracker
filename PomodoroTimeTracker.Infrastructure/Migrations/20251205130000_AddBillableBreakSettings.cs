using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PomodoroTimeTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBillableBreakSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ShortBreaksAreBillable",
                table: "PomodoroSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "LongBreaksAreBillable",
                table: "PomodoroSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsBillable",
                table: "TimeEntries",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShortBreaksAreBillable",
                table: "PomodoroSettings");

            migrationBuilder.DropColumn(
                name: "LongBreaksAreBillable",
                table: "PomodoroSettings");

            migrationBuilder.DropColumn(
                name: "IsBillable",
                table: "TimeEntries");
        }
    }
}
