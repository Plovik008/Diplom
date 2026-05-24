using System;
using System.Configuration;
using Npgsql;

class Program
{
    static void Main()
    {
        var cs = ConfigurationManager.ConnectionStrings["MuseumDB"]?.ConnectionString;
        if (string.IsNullOrWhiteSpace(cs))
        {
            Console.WriteLine("NO_CONNECTION_STRING");
            return;
        }

        var builder = new NpgsqlConnectionStringBuilder(cs);
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
        if (builder.SslMode == SslMode.Prefer) builder.SslMode = SslMode.Disable;

        using (var conn = new NpgsqlConnection(builder.ConnectionString))
        {
            conn.Open();
            using (var cmd = new NpgsqlCommand("SELECT username, password, role, fullname FROM users ORDER BY id", conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    Console.WriteLine($"{reader.GetString(0)} | {reader.GetString(1)} | {reader.GetString(2)} | {reader.GetString(3)}");
                }
            }
        }
    }
}
