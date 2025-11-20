using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System;
using System.IO;

namespace PomodoroTimeTracker.Infrastructure.Data;

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

        // Use a temporary path for design-time operations
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PomodoroTimeTracker"
        );
        Directory.CreateDirectory(appDataPath);
        var dbPath = Path.Combine(appDataPath, "pomodoro.db");

        optionsBuilder.UseSqlite($"Data Source={dbPath}");

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
