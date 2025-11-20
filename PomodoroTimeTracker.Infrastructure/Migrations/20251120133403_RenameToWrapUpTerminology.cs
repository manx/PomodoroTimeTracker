using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PomodoroTimeTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameToWrapUpTerminology : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SoftStopDurationMinutes",
                table: "PomodoroSettings",
                newName: "WrapUpPeriodMinutes");

            migrationBuilder.RenameColumn(
                name: "SoftStopAlarmVolume",
                table: "PomodoroSettings",
                newName: "WrapUpNotificationVolume");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "WrapUpPeriodMinutes",
                table: "PomodoroSettings",
                newName: "SoftStopDurationMinutes");

            migrationBuilder.RenameColumn(
                name: "WrapUpNotificationVolume",
                table: "PomodoroSettings",
                newName: "SoftStopAlarmVolume");
        }
    }
}
