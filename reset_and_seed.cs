using System;
using System.Configuration;
using System.Linq;
using MuseumAccountingSystem.Services;
using Npgsql;

class Program
{
    static string BuildConnectionString()
    {
        string configuredConnection = ConfigurationManager.ConnectionStrings["MuseumDB"]?.ConnectionString;
        var builder = string.IsNullOrWhiteSpace(configuredConnection)
            ? new NpgsqlConnectionStringBuilder()
            : new NpgsqlConnectionStringBuilder(configuredConnection);

        string envHost = Environment.GetEnvironmentVariable("MUSEUM_DB_HOST");
        string envPort = Environment.GetEnvironmentVariable("MUSEUM_DB_PORT");
        string envDatabase = Environment.GetEnvironmentVariable("MUSEUM_DB_NAME");
        string envUsername = Environment.GetEnvironmentVariable("MUSEUM_DB_USER");
        string envPassword = Environment.GetEnvironmentVariable("MUSEUM_DB_PASSWORD");

        if (!string.IsNullOrWhiteSpace(envHost)) builder.Host = envHost;
        if (int.TryParse(envPort, out int parsedPort)) builder.Port = parsedPort;
        if (!string.IsNullOrWhiteSpace(envDatabase)) builder.Database = envDatabase;
        if (!string.IsNullOrWhiteSpace(envUsername)) builder.Username = envUsername;
        if (!string.IsNullOrWhiteSpace(envPassword)) builder.Password = envPassword;
        if (string.IsNullOrWhiteSpace(builder.Host)) builder.Host = "localhost";
        if (builder.Port == 0) builder.Port = 5432;
        if (string.IsNullOrWhiteSpace(builder.Database)) builder.Database = "museumdb";
        if (string.IsNullOrWhiteSpace(builder.Username)) builder.Username = "postgres";
        if (string.IsNullOrWhiteSpace(builder.Password)) builder.Password = "postgres";
        if (builder.SslMode == SslMode.Prefer) builder.SslMode = SslMode.Disable;

        return builder.ConnectionString;
    }

    static void Main()
    {
        var cs = BuildConnectionString();
        using (var conn = new NpgsqlConnection(cs))
        {
            conn.Open();
            string sql = @"TRUNCATE TABLE user_logs, issues, users, teachers, exhibits, locations, data_version RESTART IDENTITY CASCADE;";
            using (var cmd = new NpgsqlCommand(sql, conn))
            {
                cmd.ExecuteNonQuery();
            }
        }

        using (var service = new DatabaseService())
        {
            var users = service.GetTeacherUsers();
            Console.WriteLine("RESET_AND_SEED_DONE");
            foreach (var user in users)
            {
                Console.WriteLine($"TEACHER: {user.Username} | {user.Password} | {user.FullName}");
            }
        }

        using (var conn = new NpgsqlConnection(cs))
        {
            conn.Open();
            using (var cmd = new NpgsqlCommand("SELECT username, password, role, fullname FROM users ORDER BY id", conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    Console.WriteLine($"USER: {reader.GetString(0)} | {reader.GetString(1)} | {reader.GetString(2)} | {reader.GetString(3)}");
                }
            }
        }
    }
}
