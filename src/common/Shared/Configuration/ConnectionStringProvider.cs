using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace Common.Shared.Configuration
{
    /// <summary>
    /// Centralized provider for database connection strings.
    /// Priority: Environment Variable > User Secrets > appsettings.json
    /// </summary>
    public static class ConnectionStringProvider
    {
        private const string ENV_VAR_NAME = "MAGAZINE_DB";
        private const string CONFIG_KEY = "ConnectionStrings:MagazineDb";
        
        /// <summary>
        /// Gets the database connection string from configuration.
        /// Throws InvalidOperationException if not configured.
        /// </summary>
        public static string GetConnectionString()
        {
            // Priority 1: Environment Variable
            var connString = Environment.GetEnvironmentVariable(ENV_VAR_NAME);
            if (!string.IsNullOrWhiteSpace(connString))
            {
                LogSource("Environment Variable");
                return connString;
            }
            
            // Priority 2 & 3: Configuration (User Secrets or appsettings.json)
            var configuration = BuildConfiguration();
            connString = configuration[CONFIG_KEY];
            
            if (!string.IsNullOrWhiteSpace(connString))
            {
                LogSource("Configuration (User Secrets or appsettings.json)");
                return connString;
            }
            
            // No configuration found
            throw new InvalidOperationException(
                $"Database connection string not configured. " +
                $"Please set either:\n" +
                $"  1. Environment variable: {ENV_VAR_NAME}\n" +
                $"  2. User secrets (dotnet user-secrets set \"{CONFIG_KEY}\" \"<value>\")\n" +
                $"  3. appsettings.json: {CONFIG_KEY}"
            );
        }
        
        /// <summary>
        /// Tries to get connection string. Returns false if not configured.
        /// </summary>
        public static bool TryGetConnectionString(out string? connectionString)
        {
            try
            {
                connectionString = GetConnectionString();
                return true;
            }
            catch
            {
                connectionString = null;
                return false;
            }
        }
        
        private static IConfiguration BuildConfiguration()
        {
            var builder = new ConfigurationBuilder();
            
            // Add appsettings.json if it exists
            var settingsPath = FindAppsettingsJson();
            if (settingsPath != null)
            {
                builder.AddJsonFile(settingsPath, optional: true);
            }
            
            // Add user secrets in development
            if (IsDevelopmentEnvironment())
            {
                try
                {
                    // User secrets are automatically loaded if configured
                    builder.AddUserSecrets<ConnectionStringProviderMarker>(optional: true);
                }
                catch
                {
                    // User secrets not configured, which is fine
                }
            }
            
            // Add environment variables as final fallback
            builder.AddEnvironmentVariables();
            
            return builder.Build();
        }
        
        private static string? FindAppsettingsJson()
        {
            // Check current directory
            var candidates = new[]
            {
                Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json"),
                Path.Combine(AppContext.BaseDirectory ?? ".", "appsettings.json"),
            };
            
            foreach (var path in candidates)
            {
                if (File.Exists(path))
                    return path;
            }
            
            return null;
        }
        
        private static bool IsDevelopmentEnvironment()
        {
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") 
                   ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
            return string.IsNullOrEmpty(env) || env.Equals("Development", StringComparison.OrdinalIgnoreCase);
        }
        
        private static void LogSource(string source)
        {
            try
            {
                // Only log in debug/development
                if (IsDevelopmentEnvironment())
                {
                    Console.WriteLine($"[ConnectionStringProvider] Loading from: {source}");
                }
            }
            catch
            {
                // Ignore logging errors
            }
        }
    }
    
    // Marker class for user secrets assembly attribute
    internal class ConnectionStringProviderMarker { }
}

