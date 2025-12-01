using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PomodoroTimeTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSoundSelectionSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AlarmSound",
                table: "PomodoroSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WrapUpNotificationSound",
                table: "PomodoroSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AlarmSound",
                table: "PomodoroSettings");

            migrationBuilder.DropColumn(
                name: "WrapUpNotificationSound",
                table: "PomodoroSettings");
        }
    }
}
