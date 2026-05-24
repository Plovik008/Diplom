using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Windows;
using MuseumAccountingSystem.Models;
using Npgsql;
using Newtonsoft.Json;

namespace MuseumAccountingSystem.Services
{
    public class DatabaseService : IDisposable
    {
        private string connectionString;
        private System.Timers.Timer refreshTimer;
        private int lastDataVersion = 0;
        public event EventHandler DataChanged;

        public DatabaseService()
        {
            connectionString = BuildConnectionString();
            InitializeDatabase();
            StartRefreshTimer();
        }

        private string BuildConnectionString()
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

            if (string.IsNullOrWhiteSpace(builder.Host))
                builder.Host = "localhost";
            if (builder.Port == 0)
                builder.Port = 5432;
            if (string.IsNullOrWhiteSpace(builder.Database))
                builder.Database = "museumdb";
            if (string.IsNullOrWhiteSpace(builder.Username))
                builder.Username = "postgres";
            if (string.IsNullOrWhiteSpace(builder.Password))
                builder.Password = "postgres";
            if (builder.SslMode == SslMode.Prefer)
                builder.SslMode = SslMode.Disable;

            return builder.ConnectionString;
        }

        private void InitializeDatabase()
        {
            EnsureDatabaseExists();

            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();

                string clearOldTeachers = "DELETE FROM teachers WHERE fullname = 'Иванов Иван Иванович' OR email = 'ivanov@university.ru'";
                try
                {
                    using (var cmdClear = new NpgsqlCommand(clearOldTeachers, conn))
                    {
                        cmdClear.ExecuteNonQuery();
                    }
                }
                catch { }

                string createTables = @"
                    CREATE TABLE IF NOT EXISTS users (
                        id SERIAL PRIMARY KEY,
                        username VARCHAR(100) UNIQUE NOT NULL,
                        password VARCHAR(100) NOT NULL,
                        role VARCHAR(50) NOT NULL,
                        fullname VARCHAR(200) NOT NULL,
                        teacher_id INTEGER
                    );

                    CREATE TABLE IF NOT EXISTS teachers (
                        id SERIAL PRIMARY KEY,
                        fullname VARCHAR(200) NOT NULL,
                        department VARCHAR(200),
                        email VARCHAR(100),
                        phone VARCHAR(50)
                    );

                    CREATE TABLE IF NOT EXISTS exhibits (
                        id SERIAL PRIMARY KEY,
                        inventory_number VARCHAR(100) UNIQUE NOT NULL,
                        name VARCHAR(200) NOT NULL,
                        category VARCHAR(100),
                        material VARCHAR(100),
                        condition VARCHAR(50),
                        location VARCHAR(100),
                        photo_paths TEXT,
                        created_date TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                        cost DECIMAL(15,2),
                        last_restoration_date TIMESTAMP,
                        responsible_person VARCHAR(200),
                        source VARCHAR(100),
                        year_of_origin INTEGER,
                        data_version INTEGER DEFAULT 0
                    );

                    CREATE TABLE IF NOT EXISTS issues (
                        id SERIAL PRIMARY KEY,
                        exhibit_id INTEGER NOT NULL,
                        teacher_id INTEGER NOT NULL,
                        issue_date TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                        planned_return_date TIMESTAMP NOT NULL,
                        actual_return_date TIMESTAMP,
                        purpose TEXT,
                        status VARCHAR(50) NOT NULL,
                        data_version INTEGER DEFAULT 0
                    );

                    CREATE TABLE IF NOT EXISTS user_logs (
                        id SERIAL PRIMARY KEY,
                        username VARCHAR(100) NOT NULL,
                        user_role VARCHAR(50) NOT NULL,
                        action VARCHAR(100) NOT NULL,
                        target_type VARCHAR(100) NOT NULL,
                        target_name VARCHAR(200),
                        action_time TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                        details TEXT
                    );

                    CREATE TABLE IF NOT EXISTS locations (
                        id SERIAL PRIMARY KEY,
                        name VARCHAR(100) UNIQUE NOT NULL
                    );

                    CREATE TABLE IF NOT EXISTS data_version (
                        id INTEGER PRIMARY KEY DEFAULT 1,
                        version INTEGER DEFAULT 0
                    );

                    CREATE UNIQUE INDEX IF NOT EXISTS ux_users_teacher_id
                    ON users(teacher_id)
                    WHERE teacher_id IS NOT NULL;
                ";

                using (var cmd = new NpgsqlCommand(createTables, conn))
                {
                    cmd.ExecuteNonQuery();
                }

                string updateLegacySchema = @"
                    ALTER TABLE users ADD COLUMN IF NOT EXISTS teacher_id INTEGER;
                    ALTER TABLE teachers ADD COLUMN IF NOT EXISTS department VARCHAR(200);
                    ALTER TABLE teachers ADD COLUMN IF NOT EXISTS email VARCHAR(100);
                    ALTER TABLE teachers ADD COLUMN IF NOT EXISTS phone VARCHAR(50);
                ";
                using (var cmd = new NpgsqlCommand(updateLegacySchema, conn))
                {
                    cmd.ExecuteNonQuery();
                }

                string initLocations = @"
                    INSERT INTO locations (name) VALUES 
                        ('Музей'), ('Кабинет директора'), ('Аудитория 101'), ('Аудитория 202'),
                        ('Аудитория 303'), ('Выставочный зал'), ('Фондовая комната'), ('Реставрационная мастерская')
                    ON CONFLICT (name) DO NOTHING;
                ";
                using (var cmd = new NpgsqlCommand(initLocations, conn))
                {
                    cmd.ExecuteNonQuery();
                }

                string initVersion = @"
                    INSERT INTO data_version (id, version) VALUES (1, 0)
                    ON CONFLICT (id) DO NOTHING;
                ";
                using (var cmd = new NpgsqlCommand(initVersion, conn))
                {
                    cmd.ExecuteNonQuery();
                }

                string checkUsers = "SELECT COUNT(*) FROM users";
                using (var cmd = new NpgsqlCommand(checkUsers, conn))
                {
                    long count = (long)cmd.ExecuteScalar();
                    if (count == 0)
                    {
string insertDefault = @"
                            INSERT INTO users (username, password, role, fullname) VALUES 
                            ('admin', 'admin123', 'Admin', 'Администратор системы'),
                            ('employee', 'employee123', 'Employee', 'Сотрудник музея'),
                            ('teacher', 'teacher123', 'Teacher', 'Преподаватель');
                        ";
                        using (var cmd2 = new NpgsqlCommand(insertDefault, conn))
                        {
                            cmd2.ExecuteNonQuery();
                        }
                    }
                }
            }
        }

        private void EnsureDatabaseExists()
        {
            var targetBuilder = new NpgsqlConnectionStringBuilder(connectionString);
            var adminBuilder = new NpgsqlConnectionStringBuilder(connectionString)
            {
                Database = "postgres"
            };

            using (var conn = new NpgsqlConnection(adminBuilder.ConnectionString))
            {
                conn.Open();
                string sql = "SELECT 1 FROM pg_database WHERE datname = @database";
                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@database", targetBuilder.Database);
                    bool exists = cmd.ExecuteScalar() != null;
                    if (!exists)
                    {
                        string safeDatabaseName = targetBuilder.Database.Replace("\"", "\"\"");
                        using (var createCmd = new NpgsqlCommand($"CREATE DATABASE \"{safeDatabaseName}\"", conn))
                        {
                            createCmd.ExecuteNonQuery();
                        }
                    }
                }
            }
        }

        private void StartRefreshTimer()
        {
            refreshTimer = new System.Timers.Timer(3000);
            refreshTimer.Elapsed += (s, e) => CheckForUpdates();
            refreshTimer.Start();
        }

        private void CheckForUpdates()
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = "SELECT version FROM data_version WHERE id = 1";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        int currentVersion = Convert.ToInt32(cmd.ExecuteScalar());
                        if (currentVersion != lastDataVersion)
                        {
                            lastDataVersion = currentVersion;
                            DataChanged?.Invoke(this, EventArgs.Empty);
                        }
                    }
                }
            }
            catch { }
        }

        private void IncrementVersion()
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = "UPDATE data_version SET version = version + 1 WHERE id = 1";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch { }
        }

        public int GetCurrentDataVersion()
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = "SELECT version FROM data_version WHERE id = 1";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        return Convert.ToInt32(cmd.ExecuteScalar());
                    }
                }
            }
            catch { return 0; }
        }

        public int GetExhibitVersion(int exhibitId)
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = "SELECT data_version FROM exhibits WHERE id = @id";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", exhibitId);
                        var result = cmd.ExecuteScalar();
                        return result == DBNull.Value ? 0 : Convert.ToInt32(result);
                    }
                }
            }
            catch { return 0; }
        }

        private List<string> ParsePhotoPaths(string json)
        {
            if (string.IsNullOrEmpty(json)) return new List<string>();
            try
            {
                return JsonConvert.DeserializeObject<List<string>>(json) ?? new List<string>();
            }
            catch
            {
                return new List<string> { json };
            }
        }

        private string SerializePhotoPaths(List<string> paths)
        {
            if (paths == null || paths.Count == 0) return "[]";
            return JsonConvert.SerializeObject(paths);
        }

        public List<Exhibit> GetAllExhibits()
        {
            var exhibits = new List<Exhibit>();
            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                string sql = @"SELECT e.id, e.inventory_number, e.name, e.category, e.material, e.condition, 
                               e.location, e.photo_paths, e.created_date, e.cost, e.last_restoration_date,
                               e.responsible_person, e.source, e.year_of_origin, e.data_version,
                               CAST(CASE WHEN EXISTS(SELECT 1 FROM issues i WHERE i.exhibit_id = e.id AND i.status = 'Выдан') THEN 1 ELSE 0 END AS INTEGER) as is_issued_int
                               FROM exhibits e ORDER BY e.inventory_number";
                using (var cmd = new NpgsqlCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        exhibits.Add(new Exhibit
                        {
                            Id = reader.GetInt32(0),
                            InventoryNumber = reader.GetString(1),
                            Name = reader.GetString(2),
                            Category = reader.IsDBNull(3) ? null : reader.GetString(3),
                            Material = reader.IsDBNull(4) ? null : reader.GetString(4),
                            Condition = reader.IsDBNull(5) ? "В наличии" : reader.GetString(5),
                            Location = reader.IsDBNull(6) ? null : reader.GetString(6),
                            PhotoPaths = ParsePhotoPaths(reader.IsDBNull(7) ? null : reader.GetString(7)),
                            CreatedDate = reader.IsDBNull(8) ? DateTime.Now : reader.GetDateTime(8),
                            Cost = reader.IsDBNull(9) ? 0 : reader.GetDecimal(9),
                            LastRestorationDate = reader.IsDBNull(10) ? null : (DateTime?)reader.GetDateTime(10),
                            ResponsiblePerson = reader.IsDBNull(11) ? null : reader.GetString(11),
                            Source = reader.IsDBNull(12) ? null : reader.GetString(12),
                            YearOfOrigin = reader.IsDBNull(13) ? null : (int?)reader.GetInt32(13),
                            DataVersion = reader.IsDBNull(14) ? 0 : reader.GetInt32(14),
                            CurrentStatus = reader.GetInt32(15) == 1 ? "Выдан" : "В наличии"
                        });
                    }
                }
            }
            return exhibits;
        }

        public void AddExhibit(Exhibit exhibit, User currentUser = null)
        {
            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                string sql = @"INSERT INTO exhibits (inventory_number, name, category, material, condition, location, photo_paths, created_date, cost, last_restoration_date, responsible_person, source, year_of_origin, data_version)
                               VALUES (@inventory_number, @name, @category, @material, @condition, @location, @photo_paths, @created_date, @cost, @last_restoration_date, @responsible_person, @source, @year_of_origin, 0)";
                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@inventory_number", exhibit.InventoryNumber);
                    cmd.Parameters.AddWithValue("@name", exhibit.Name);
                    cmd.Parameters.AddWithValue("@category", (object)exhibit.Category ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@material", (object)exhibit.Material ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@condition", exhibit.Condition ?? "В наличии");
                    cmd.Parameters.AddWithValue("@location", exhibit.Location ?? "Музей");
                    cmd.Parameters.AddWithValue("@photo_paths", SerializePhotoPaths(exhibit.PhotoPaths));
                    cmd.Parameters.AddWithValue("@created_date", DateTime.Now);
                    cmd.Parameters.AddWithValue("@cost", exhibit.Cost);
                    cmd.Parameters.AddWithValue("@last_restoration_date", (object)exhibit.LastRestorationDate ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@responsible_person", (object)exhibit.ResponsiblePerson ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@source", (object)exhibit.Source ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@year_of_origin", (object)exhibit.YearOfOrigin ?? DBNull.Value);
                    cmd.ExecuteNonQuery();
                }
            }
            IncrementVersion();
            if (currentUser != null)
            {
                AddLog(currentUser, "Добавление", "Экспонат", exhibit.Name, $"Инв. номер: {exhibit.InventoryNumber}");
            }
        }

        public void UpdateExhibit(Exhibit exhibit, User currentUser = null)
        {
            int newVersion = 1;
            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                string getVersionSql = "SELECT COALESCE(data_version, 0) FROM exhibits WHERE id = @id";
                using (var getCmd = new NpgsqlCommand(getVersionSql, conn))
                {
                    getCmd.Parameters.AddWithValue("@id", exhibit.Id);
                    var result = getCmd.ExecuteScalar();
                    newVersion = Convert.ToInt32(result) + 1;
                }

                string sql = @"UPDATE exhibits SET 
                    inventory_number = @inventory_number, name = @name, category = @category, 
                    material = @material, condition = @condition, location = @location,
                    photo_paths = @photo_paths, cost = @cost, last_restoration_date = @last_restoration_date,
                    responsible_person = @responsible_person, source = @source, year_of_origin = @year_of_origin,
                    data_version = @data_version
                    WHERE id = @id";
                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", exhibit.Id);
                    cmd.Parameters.AddWithValue("@inventory_number", exhibit.InventoryNumber);
                    cmd.Parameters.AddWithValue("@name", exhibit.Name);
                    cmd.Parameters.AddWithValue("@category", (object)exhibit.Category ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@material", (object)exhibit.Material ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@condition", exhibit.Condition ?? "В наличии");
                    cmd.Parameters.AddWithValue("@location", exhibit.Location ?? "Музей");
                    cmd.Parameters.AddWithValue("@photo_paths", SerializePhotoPaths(exhibit.PhotoPaths));
                    cmd.Parameters.AddWithValue("@cost", exhibit.Cost);
                    cmd.Parameters.AddWithValue("@last_restoration_date", (object)exhibit.LastRestorationDate ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@responsible_person", (object)exhibit.ResponsiblePerson ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@source", (object)exhibit.Source ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@year_of_origin", (object)exhibit.YearOfOrigin ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@data_version", newVersion);
                    cmd.ExecuteNonQuery();
                }
            }
            IncrementVersion();
            if (currentUser != null)
            {
                AddLog(currentUser, "Редактирование", "Экспонат", exhibit.Name, $"Инв. номер: {exhibit.InventoryNumber}");
            }
        }

        public void DeleteExhibit(int id, User currentUser = null)
        {
            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();

                var exhibit = GetAllExhibits().FirstOrDefault(e => e.Id == id);
                if (currentUser != null && exhibit != null)
                {
                    AddLog(currentUser, "Удаление", "Экспонат", exhibit.Name, $"Инв. номер: {exhibit.InventoryNumber}");
                }

                string sql = "DELETE FROM issues WHERE exhibit_id = @id";
                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }

                sql = "DELETE FROM exhibits WHERE id = @id";
                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
            }
            IncrementVersion();
        }

        public List<Teacher> GetAllTeachers()
        {
            var teachers = new List<Teacher>();
            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                string sql = @"
                    SELECT t.id, t.fullname, t.department, t.email, t.phone, u.username, u.password
                    FROM teachers t
                    LEFT JOIN users u ON u.teacher_id = t.id AND u.role = 'Teacher'
                    ORDER BY t.fullname";
                using (var cmd = new NpgsqlCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        teachers.Add(new Teacher
                        {
                            Id = reader.GetInt32(0),
                            FullName = reader.GetString(1),
                            Department = reader.IsDBNull(2) ? null : reader.GetString(2),
                            Email = reader.IsDBNull(3) ? null : reader.GetString(3),
                            Phone = reader.IsDBNull(4) ? null : reader.GetString(4),
                            Username = reader.IsDBNull(5) ? null : reader.GetString(5),
                            Password = reader.IsDBNull(6) ? null : reader.GetString(6)
                        });
                    }
                }
            }
            return teachers;
        }

        public bool IsTeacherEmailExists(string email, int? excludeTeacherId = null)
        {
            if (string.IsNullOrEmpty(email))
                return false;

            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                string sql = "SELECT COUNT(*) FROM teachers WHERE LOWER(email) = LOWER(@email)";
                if (excludeTeacherId.HasValue)
                    sql += " AND id != @excludeId";

                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@email", email);
                    if (excludeTeacherId.HasValue)
                        cmd.Parameters.AddWithValue("@excludeId", excludeTeacherId.Value);

                    return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                }
            }
        }

        public bool IsTeacherUsernameExists(string username, int? excludeTeacherId = null)
        {
            if (string.IsNullOrWhiteSpace(username))
                return false;

            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                string sql = "SELECT COUNT(*) FROM users WHERE LOWER(username) = LOWER(@username) AND role = 'Teacher'";
                if (excludeTeacherId.HasValue)
                    sql += " AND (teacher_id IS NULL OR teacher_id != @excludeId)";

                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@username", username.Trim());
                    if (excludeTeacherId.HasValue)
                        cmd.Parameters.AddWithValue("@excludeId", excludeTeacherId.Value);

                    return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                }
            }
        }

        public void AddTeacher(Teacher teacher, User currentUser = null)
        {
            if (!string.IsNullOrEmpty(teacher.Email) && IsTeacherEmailExists(teacher.Email))
            {
                throw new Exception("Преподаватель с таким email уже существует");
            }

            if (!string.IsNullOrWhiteSpace(teacher.Username) && string.IsNullOrWhiteSpace(teacher.Password))
            {
                throw new Exception("Введите пароль для логина преподавателя");
            }

            if (string.IsNullOrWhiteSpace(teacher.Username) && !string.IsNullOrWhiteSpace(teacher.Password))
            {
                throw new Exception("Введите логин преподавателя");
            }

            if (!string.IsNullOrWhiteSpace(teacher.Username) && IsTeacherUsernameExists(teacher.Username))
            {
                throw new Exception("Преподаватель с таким логином уже существует");
            }

            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    string sql = "INSERT INTO teachers (fullname, department, email, phone) VALUES (@fullname, @department, @email, @phone) RETURNING id";
                    int teacherId;
                    using (var cmd = new NpgsqlCommand(sql, conn, transaction))
                    {
                        cmd.Parameters.AddWithValue("@fullname", teacher.FullName);
                        cmd.Parameters.AddWithValue("@department", (object)teacher.Department ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@email", (object)teacher.Email ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@phone", (object)teacher.Phone ?? DBNull.Value);
                        teacherId = Convert.ToInt32(cmd.ExecuteScalar());
                    }

                    teacher.Id = teacherId;
                    UpsertTeacherUser(conn, transaction, teacher);
                    transaction.Commit();
                }
            }
            IncrementVersion();
            if (currentUser != null)
            {
                AddLog(currentUser, "Добавление", "Преподаватель", teacher.FullName, BuildTeacherLogDetails(teacher));
            }
        }

        public void UpdateTeacher(Teacher teacher, User currentUser = null)
        {
            if (!string.IsNullOrEmpty(teacher.Email) && IsTeacherEmailExists(teacher.Email, teacher.Id))
            {
                throw new Exception("Преподаватель с таким email уже существует");
            }

            if (!string.IsNullOrWhiteSpace(teacher.Username) && string.IsNullOrWhiteSpace(teacher.Password))
            {
                throw new Exception("Введите пароль для логина преподавателя");
            }

            if (string.IsNullOrWhiteSpace(teacher.Username) && !string.IsNullOrWhiteSpace(teacher.Password))
            {
                throw new Exception("Введите логин преподавателя");
            }

            if (!string.IsNullOrWhiteSpace(teacher.Username) && IsTeacherUsernameExists(teacher.Username, teacher.Id))
            {
                throw new Exception("Преподаватель с таким логином уже существует");
            }

            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    string sql = "UPDATE teachers SET fullname = @fullname, department = @department, email = @email, phone = @phone WHERE id = @id";
                    using (var cmd = new NpgsqlCommand(sql, conn, transaction))
                    {
                        cmd.Parameters.AddWithValue("@id", teacher.Id);
                        cmd.Parameters.AddWithValue("@fullname", teacher.FullName);
                        cmd.Parameters.AddWithValue("@department", (object)teacher.Department ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@email", (object)teacher.Email ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@phone", (object)teacher.Phone ?? DBNull.Value);
                        cmd.ExecuteNonQuery();
                    }

                    UpsertTeacherUser(conn, transaction, teacher);
                    transaction.Commit();
                }
            }
            IncrementVersion();
            if (currentUser != null)
            {
                AddLog(currentUser, "Редактирование", "Преподаватель", teacher.FullName, BuildTeacherLogDetails(teacher));
            }
        }

        public void DeleteTeacher(int id, User currentUser = null)
        {
            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                var teacher = GetAllTeachers().FirstOrDefault(t => t.Id == id);
                if (currentUser != null && teacher != null)
                {
                    AddLog(currentUser, "Удаление", "Преподаватель", teacher.FullName, "");
                }
                using (var transaction = conn.BeginTransaction())
                {
                    using (var userCmd = new NpgsqlCommand("DELETE FROM users WHERE teacher_id = @id", conn, transaction))
                    {
                        userCmd.Parameters.AddWithValue("@id", id);
                        userCmd.ExecuteNonQuery();
                    }

                    using (var cmd = new NpgsqlCommand("DELETE FROM teachers WHERE id = @id", conn, transaction))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.ExecuteNonQuery();
                    }

                    transaction.Commit();
                }
            }
            IncrementVersion();
        }

        public void ImportTeachers(List<Teacher> teachers, User currentUser = null)
        {
            if (teachers == null || teachers.Count == 0)
                return;

            foreach (var teacher in teachers)
            {
                if (string.IsNullOrWhiteSpace(teacher.FullName))
                    throw new Exception("В списке импорта найден преподаватель без ФИО");
            }

            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    foreach (var teacher in teachers)
                    {
                        int? existingTeacherId = null;

                        if (!string.IsNullOrWhiteSpace(teacher.Email))
                        {
                            using (var findByEmail = new NpgsqlCommand("SELECT id FROM teachers WHERE LOWER(email) = LOWER(@email) LIMIT 1", conn, transaction))
                            {
                                findByEmail.Parameters.AddWithValue("@email", teacher.Email.Trim());
                                var result = findByEmail.ExecuteScalar();
                                if (result != null)
                                    existingTeacherId = Convert.ToInt32(result);
                            }
                        }

                        if (!existingTeacherId.HasValue)
                        {
                            using (var findByName = new NpgsqlCommand("SELECT id FROM teachers WHERE LOWER(fullname) = LOWER(@fullname) LIMIT 1", conn, transaction))
                            {
                                findByName.Parameters.AddWithValue("@fullname", teacher.FullName.Trim());
                                var result = findByName.ExecuteScalar();
                                if (result != null)
                                    existingTeacherId = Convert.ToInt32(result);
                            }
                        }

                        teacher.FullName = teacher.FullName?.Trim();
                        teacher.Department = NormalizeValue(teacher.Department);
                        teacher.Email = NormalizeValue(teacher.Email);
                        teacher.Phone = NormalizeValue(teacher.Phone);
                        teacher.Username = NormalizeValue(teacher.Username);
                        teacher.Password = NormalizeValue(teacher.Password);

                        if (existingTeacherId.HasValue)
                        {
                            teacher.Id = existingTeacherId.Value;
                            using (var updateCmd = new NpgsqlCommand("UPDATE teachers SET fullname = @fullname, department = @department, email = @email, phone = @phone WHERE id = @id", conn, transaction))
                            {
                                updateCmd.Parameters.AddWithValue("@id", teacher.Id);
                                updateCmd.Parameters.AddWithValue("@fullname", teacher.FullName);
                                updateCmd.Parameters.AddWithValue("@department", (object)teacher.Department ?? DBNull.Value);
                                updateCmd.Parameters.AddWithValue("@email", (object)teacher.Email ?? DBNull.Value);
                                updateCmd.Parameters.AddWithValue("@phone", (object)teacher.Phone ?? DBNull.Value);
                                updateCmd.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            using (var insertCmd = new NpgsqlCommand("INSERT INTO teachers (fullname, department, email, phone) VALUES (@fullname, @department, @email, @phone) RETURNING id", conn, transaction))
                            {
                                insertCmd.Parameters.AddWithValue("@fullname", teacher.FullName);
                                insertCmd.Parameters.AddWithValue("@department", (object)teacher.Department ?? DBNull.Value);
                                insertCmd.Parameters.AddWithValue("@email", (object)teacher.Email ?? DBNull.Value);
                                insertCmd.Parameters.AddWithValue("@phone", (object)teacher.Phone ?? DBNull.Value);
                                teacher.Id = Convert.ToInt32(insertCmd.ExecuteScalar());
                            }
                        }

                        if (!string.IsNullOrWhiteSpace(teacher.Username) && string.IsNullOrWhiteSpace(teacher.Password))
                            throw new Exception($"Для преподавателя \"{teacher.FullName}\" не указан пароль");

                        if (string.IsNullOrWhiteSpace(teacher.Username) && !string.IsNullOrWhiteSpace(teacher.Password))
                            throw new Exception($"Для преподавателя \"{teacher.FullName}\" не указан логин");

                        if (!string.IsNullOrWhiteSpace(teacher.Username) && IsTeacherUsernameExists(teacher.Username, teacher.Id))
                            throw new Exception($"Логин \"{teacher.Username}\" уже используется");

                        UpsertTeacherUser(conn, transaction, teacher);
                    }

                    transaction.Commit();
                }
            }

            IncrementVersion();
            if (currentUser != null)
            {
                AddLog(currentUser, "Импорт", "Преподаватели", $"Количество: {teachers.Count}", "Импорт преподавателей с логинами");
            }
        }

        private void UpsertTeacherUser(NpgsqlConnection conn, NpgsqlTransaction transaction, Teacher teacher)
        {
            if (string.IsNullOrWhiteSpace(teacher.Username))
            {
                using (var deleteCmd = new NpgsqlCommand("DELETE FROM users WHERE teacher_id = @teacherId AND role = 'Teacher'", conn, transaction))
                {
                    deleteCmd.Parameters.AddWithValue("@teacherId", teacher.Id);
                    deleteCmd.ExecuteNonQuery();
                }
                return;
            }

            string existingSql = "SELECT id FROM users WHERE teacher_id = @teacherId AND role = 'Teacher' LIMIT 1";
            using (var checkCmd = new NpgsqlCommand(existingSql, conn, transaction))
            {
                checkCmd.Parameters.AddWithValue("@teacherId", teacher.Id);
                var existingId = checkCmd.ExecuteScalar();
                if (existingId == null)
                {
                    string insertSql = @"INSERT INTO users (username, password, role, fullname, teacher_id)
                                         VALUES (@username, @password, 'Teacher', @fullname, @teacher_id)";
                    using (var insertCmd = new NpgsqlCommand(insertSql, conn, transaction))
                    {
                        insertCmd.Parameters.AddWithValue("@username", teacher.Username.Trim());
                        insertCmd.Parameters.AddWithValue("@password", teacher.Password.Trim());
                        insertCmd.Parameters.AddWithValue("@fullname", teacher.FullName);
                        insertCmd.Parameters.AddWithValue("@teacher_id", teacher.Id);
                        insertCmd.ExecuteNonQuery();
                    }
                }
                else
                {
                    string updateSql = @"UPDATE users
                                         SET username = @username, password = @password, fullname = @fullname
                                         WHERE id = @id";
                    using (var updateCmd = new NpgsqlCommand(updateSql, conn, transaction))
                    {
                        updateCmd.Parameters.AddWithValue("@id", Convert.ToInt32(existingId));
                        updateCmd.Parameters.AddWithValue("@username", teacher.Username.Trim());
                        updateCmd.Parameters.AddWithValue("@password", teacher.Password.Trim());
                        updateCmd.Parameters.AddWithValue("@fullname", teacher.FullName);
                        updateCmd.ExecuteNonQuery();
                    }
                }
            }
        }

        private string BuildTeacherLogDetails(Teacher teacher)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(teacher.Department))
                parts.Add($"Кафедра: {teacher.Department}");
            if (!string.IsNullOrWhiteSpace(teacher.Username))
                parts.Add($"Логин: {teacher.Username}");

            return parts.Count == 0 ? null : string.Join("; ", parts);
        }

        private string NormalizeValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            return value.Trim();
        }

        public Teacher GetTeacherByUser(User user)
        {
            if (user == null || !user.IsTeacher) return null;
            if (user.TeacherId.HasValue)
            {
                var teacherById = GetAllTeachers().FirstOrDefault(t => t.Id == user.TeacherId.Value);
                if (teacherById != null) return teacherById;
            }
            var teacherByName = GetAllTeachers().FirstOrDefault(t => string.Equals(t.FullName, user.FullName, StringComparison.OrdinalIgnoreCase));
            if (teacherByName != null) return teacherByName;
            var teachers = GetAllTeachers();
            if (teachers.Count == 1) return teachers[0];
            return null;
        }

        public int? GetTeacherIdByUser(User user)
        {
            return GetTeacherByUser(user)?.Id;
        }

        public List<Issue> GetAllIssues(bool onlyActive = false, int? teacherId = null)
        {
            var issues = new List<Issue>();
            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                string sql = @"SELECT i.id, i.exhibit_id, i.teacher_id, e.name, e.inventory_number, t.fullname, 
                               i.issue_date, i.planned_return_date, i.actual_return_date, i.purpose, i.status
                               FROM issues i
                               JOIN exhibits e ON i.exhibit_id = e.id
                               JOIN teachers t ON i.teacher_id = t.id";

                var conditions = new List<string>();
                if (teacherId.HasValue) conditions.Add($"i.teacher_id = {teacherId.Value}");
                if (onlyActive) conditions.Add("i.status = 'Выдан'");
                if (conditions.Count > 0) sql += " WHERE " + string.Join(" AND ", conditions);
                sql += " ORDER BY i.issue_date DESC";

                using (var cmd = new NpgsqlCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        issues.Add(new Issue
                        {
                            Id = reader.GetInt32(0),
                            ExhibitId = reader.GetInt32(1),
                            TeacherId = reader.GetInt32(2),
                            ExhibitName = reader.GetString(3),
                            ExhibitInventoryNumber = reader.GetString(4),
                            TeacherName = reader.GetString(5),
                            IssueDate = reader.GetDateTime(6),
                            PlannedReturnDate = reader.GetDateTime(7),
                            ActualReturnDate = reader.IsDBNull(8) ? null : (DateTime?)reader.GetDateTime(8),
                            Purpose = reader.IsDBNull(9) ? null : reader.GetString(9),
                            Status = reader.GetString(10)
                        });
                    }
                }
            }
            return issues;
        }

        public void IssueExhibit(int exhibitId, int teacherId, DateTime plannedReturnDate, string purpose, User currentUser = null)
        {
            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                int newVersion = 1;
                string getVersionSql = "SELECT COALESCE(data_version, 0) FROM exhibits WHERE id = @id";
                using (var getCmd = new NpgsqlCommand(getVersionSql, conn))
                {
                    getCmd.Parameters.AddWithValue("@id", exhibitId);
                    newVersion = Convert.ToInt32(getCmd.ExecuteScalar()) + 1;
                }

                string updateExhibitSql = "UPDATE exhibits SET data_version = @data_version WHERE id = @id";
                using (var updateCmd = new NpgsqlCommand(updateExhibitSql, conn))
                {
                    updateCmd.Parameters.AddWithValue("@id", exhibitId);
                    updateCmd.Parameters.AddWithValue("@data_version", newVersion);
                    updateCmd.ExecuteNonQuery();
                }

                string sql = @"INSERT INTO issues (exhibit_id, teacher_id, issue_date, planned_return_date, purpose, status, data_version)
                               VALUES (@exhibit_id, @teacher_id, @issue_date, @planned_return_date, @purpose, 'Выдан', 0)";
                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@exhibit_id", exhibitId);
                    cmd.Parameters.AddWithValue("@teacher_id", teacherId);
                    cmd.Parameters.AddWithValue("@issue_date", DateTime.Now);
                    cmd.Parameters.AddWithValue("@planned_return_date", plannedReturnDate);
                    cmd.Parameters.AddWithValue("@purpose", purpose ?? "");
                    cmd.ExecuteNonQuery();
                }
            }
            IncrementVersion();
            if (currentUser != null)
            {
                var exhibit = GetAllExhibits().FirstOrDefault(e => e.Id == exhibitId);
                var teacher = GetAllTeachers().FirstOrDefault(t => t.Id == teacherId);
                if (exhibit != null && teacher != null)
                {
                    AddLog(currentUser, "Выдача", "Экспонат", exhibit.Name, $"Преподаватель: {teacher.FullName}, дата возврата: {plannedReturnDate:dd.MM.yyyy}");
                }
            }
        }

        public void ReturnExhibit(int issueId, User currentUser = null)
        {
            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                var issue = GetAllIssues().FirstOrDefault(i => i.Id == issueId);
                if (currentUser != null && issue != null)
                {
                    int newVersion = 1;
                    string getVersionSql = "SELECT COALESCE(data_version, 0) FROM exhibits WHERE id = @id";
                    using (var getCmd = new NpgsqlCommand(getVersionSql, conn))
                    {
                        getCmd.Parameters.AddWithValue("@id", issue.ExhibitId);
                        newVersion = Convert.ToInt32(getCmd.ExecuteScalar()) + 1;
                    }

                    string updateExhibitSql = "UPDATE exhibits SET data_version = @data_version WHERE id = @id";
                    using (var updateCmd = new NpgsqlCommand(updateExhibitSql, conn))
                    {
                        updateCmd.Parameters.AddWithValue("@id", issue.ExhibitId);
                        updateCmd.Parameters.AddWithValue("@data_version", newVersion);
                        updateCmd.ExecuteNonQuery();
                    }

                    AddLog(currentUser, "Возврат", "Экспонат", issue.ExhibitName, $"Преподаватель: {issue.TeacherName}");
                }

                string sql = "UPDATE issues SET actual_return_date = @actual_return_date, status = 'Возвращен', data_version = 0 WHERE id = @id";
                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", issueId);
                    cmd.Parameters.AddWithValue("@actual_return_date", DateTime.Now);
                    cmd.ExecuteNonQuery();
                }
            }
            IncrementVersion();
        }

        public User AuthenticateUser(string username, string password)
        {
            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                string sql = "SELECT id, username, password, role, fullname, teacher_id FROM users WHERE username = @username AND password = @password";
                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@username", username);
                    cmd.Parameters.AddWithValue("@password", password);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new User
                            {
                                Id = reader.GetInt32(0),
                                Username = reader.GetString(1),
                                Password = reader.GetString(2),
                                Role = reader.GetString(3),
                                FullName = reader.GetString(4),
                                TeacherId = reader.IsDBNull(5) ? null : (int?)reader.GetInt32(5)
                            };
                        }
                    }
                }
            }
            return null;
        }

        public List<User> GetTeacherUsers()
        {
            var users = new List<User>();
            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                string sql = @"SELECT id, username, password, role, fullname, teacher_id
                               FROM users
                               WHERE role = 'Teacher'
                               ORDER BY fullname, username";
                using (var cmd = new NpgsqlCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        users.Add(new User
                        {
                            Id = reader.GetInt32(0),
                            Username = reader.GetString(1),
                            Password = reader.GetString(2),
                            Role = reader.GetString(3),
                            FullName = reader.GetString(4),
                            TeacherId = reader.IsDBNull(5) ? null : (int?)reader.GetInt32(5)
                        });
                    }
                }
            }

            return users;
        }

        public User GetTeacherUserById(int userId)
        {
            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                string sql = @"SELECT id, username, password, role, fullname, teacher_id
                               FROM users
                               WHERE id = @id AND role = 'Teacher'";
                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", userId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new User
                            {
                                Id = reader.GetInt32(0),
                                Username = reader.GetString(1),
                                Password = reader.GetString(2),
                                Role = reader.GetString(3),
                                FullName = reader.GetString(4),
                                TeacherId = reader.IsDBNull(5) ? null : (int?)reader.GetInt32(5)
                            };
                        }
                    }
                }
            }

            return null;
        }

        public void AddTeacherUser(User user, User currentUser = null)
        {
            ValidateTeacherUser(user);

            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    int teacherId = EnsureTeacherRecord(conn, transaction, null, user.FullName);

                    string sql = @"INSERT INTO users (username, password, role, fullname, teacher_id)
                                   VALUES (@username, @password, 'Teacher', @fullname, @teacher_id)";
                    using (var cmd = new NpgsqlCommand(sql, conn, transaction))
                    {
                        cmd.Parameters.AddWithValue("@username", user.Username.Trim());
                        cmd.Parameters.AddWithValue("@password", user.Password.Trim());
                        cmd.Parameters.AddWithValue("@fullname", user.FullName.Trim());
                        cmd.Parameters.AddWithValue("@teacher_id", teacherId);
                        cmd.ExecuteNonQuery();
                    }

                    transaction.Commit();
                }
            }

            IncrementVersion();
            if (currentUser != null)
            {
                AddLog(currentUser, "Добавление", "Пользователь", user.FullName, $"Логин: {user.Username}");
            }
        }

        public void UpdateTeacherUser(User user, User currentUser = null)
        {
            ValidateTeacherUser(user, user.Id);

            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    int teacherId = EnsureTeacherRecord(conn, transaction, user.Id, user.FullName);

                    string sql = @"UPDATE users
                                   SET username = @username,
                                       password = @password,
                                       fullname = @fullname,
                                       teacher_id = @teacher_id
                                   WHERE id = @id AND role = 'Teacher'";
                    using (var cmd = new NpgsqlCommand(sql, conn, transaction))
                    {
                        cmd.Parameters.AddWithValue("@id", user.Id);
                        cmd.Parameters.AddWithValue("@username", user.Username.Trim());
                        cmd.Parameters.AddWithValue("@password", user.Password.Trim());
                        cmd.Parameters.AddWithValue("@fullname", user.FullName.Trim());
                        cmd.Parameters.AddWithValue("@teacher_id", teacherId);
                        cmd.ExecuteNonQuery();
                    }

                    transaction.Commit();
                }
            }

            IncrementVersion();
            if (currentUser != null)
            {
                AddLog(currentUser, "Редактирование", "Пользователь", user.FullName, $"Логин: {user.Username}");
            }
        }

        public void DeleteTeacherUser(int userId, User currentUser = null)
        {
            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                string getSql = "SELECT fullname, username FROM users WHERE id = @id AND role = 'Teacher'";
                string fullName = null;
                string username = null;
                using (var getCmd = new NpgsqlCommand(getSql, conn))
                {
                    getCmd.Parameters.AddWithValue("@id", userId);
                    using (var reader = getCmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            fullName = reader.GetString(0);
                            username = reader.GetString(1);
                        }
                    }
                }

                using (var cmd = new NpgsqlCommand("DELETE FROM users WHERE id = @id AND role = 'Teacher'", conn))
                {
                    cmd.Parameters.AddWithValue("@id", userId);
                    cmd.ExecuteNonQuery();
                }

                if (currentUser != null && !string.IsNullOrWhiteSpace(fullName))
                {
                    AddLog(currentUser, "Удаление", "Пользователь", fullName, $"Логин: {username}");
                }
            }

            IncrementVersion();
        }

        private void ValidateTeacherUser(User user, int? excludeUserId = null)
        {
            if (user == null)
                throw new Exception("Пользователь не передан");

            if (string.IsNullOrWhiteSpace(user.FullName))
                throw new Exception("Введите ФИО пользователя");

            if (string.IsNullOrWhiteSpace(user.Username))
                throw new Exception("Введите логин пользователя");

            if (string.IsNullOrWhiteSpace(user.Password))
                throw new Exception("Введите пароль пользователя");

            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                string sql = "SELECT COUNT(*) FROM users WHERE LOWER(username) = LOWER(@username)";
                if (excludeUserId.HasValue)
                    sql += " AND id != @excludeId";

                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@username", user.Username.Trim());
                    if (excludeUserId.HasValue)
                        cmd.Parameters.AddWithValue("@excludeId", excludeUserId.Value);

                    if (Convert.ToInt32(cmd.ExecuteScalar()) > 0)
                        throw new Exception("Пользователь с таким логином уже существует");
                }
            }
        }

        private int EnsureTeacherRecord(NpgsqlConnection conn, NpgsqlTransaction transaction, int? userId, string fullName)
        {
            string normalizedName = fullName.Trim();
            int? teacherId = null;

            if (userId.HasValue)
            {
                using (var getTeacherCmd = new NpgsqlCommand("SELECT teacher_id FROM users WHERE id = @id", conn, transaction))
                {
                    getTeacherCmd.Parameters.AddWithValue("@id", userId.Value);
                    var existingTeacherId = getTeacherCmd.ExecuteScalar();
                    if (existingTeacherId != null && existingTeacherId != DBNull.Value)
                        teacherId = Convert.ToInt32(existingTeacherId);
                }
            }

            if (!teacherId.HasValue)
            {
                using (var findTeacherCmd = new NpgsqlCommand("SELECT id FROM teachers WHERE LOWER(fullname) = LOWER(@fullname) ORDER BY id LIMIT 1", conn, transaction))
                {
                    findTeacherCmd.Parameters.AddWithValue("@fullname", normalizedName);
                    var existingTeacherId = findTeacherCmd.ExecuteScalar();
                    if (existingTeacherId != null)
                        teacherId = Convert.ToInt32(existingTeacherId);
                }
            }

            if (teacherId.HasValue)
            {
                using (var updateTeacherCmd = new NpgsqlCommand("UPDATE teachers SET fullname = @fullname WHERE id = @id", conn, transaction))
                {
                    updateTeacherCmd.Parameters.AddWithValue("@fullname", normalizedName);
                    updateTeacherCmd.Parameters.AddWithValue("@id", teacherId.Value);
                    updateTeacherCmd.ExecuteNonQuery();
                }

                return teacherId.Value;
            }

            using (var insertTeacherCmd = new NpgsqlCommand("INSERT INTO teachers (fullname) VALUES (@fullname) RETURNING id", conn, transaction))
            {
                insertTeacherCmd.Parameters.AddWithValue("@fullname", normalizedName);
                return Convert.ToInt32(insertTeacherCmd.ExecuteScalar());
            }
        }

        public List<UserActionLog> GetAllLogs()
        {
            var logs = new List<UserActionLog>();
            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                string sql = "SELECT id, username, user_role, action, target_type, target_name, action_time, details FROM user_logs ORDER BY action_time DESC";
                using (var cmd = new NpgsqlCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        logs.Add(new UserActionLog
                        {
                            Id = reader.GetInt32(0),
                            Username = reader.GetString(1),
                            UserRole = reader.GetString(2),
                            Action = reader.GetString(3),
                            TargetType = reader.GetString(4),
                            TargetName = reader.IsDBNull(5) ? null : reader.GetString(5),
                            ActionTime = reader.GetDateTime(6),
                            Details = reader.IsDBNull(7) ? null : reader.GetString(7)
                        });
                    }
                }
            }
            return logs;
        }

        private void AddLog(User user, string action, string targetType, string targetName, string details)
        {
            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                string sql = "INSERT INTO user_logs (username, user_role, action, target_type, target_name, action_time, details) VALUES (@username, @user_role, @action, @target_type, @target_name, @action_time, @details)";
                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@username", user.Username);
                    cmd.Parameters.AddWithValue("@user_role", user.Role);
                    cmd.Parameters.AddWithValue("@action", action);
                    cmd.Parameters.AddWithValue("@target_type", targetType);
                    cmd.Parameters.AddWithValue("@target_name", (object)targetName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@action_time", DateTime.Now);
                    cmd.Parameters.AddWithValue("@details", (object)details ?? DBNull.Value);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public List<string> GetLocations()
        {
            var locations = new List<string>();
            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                string sql = "SELECT name FROM locations ORDER BY name";
                using (var cmd = new NpgsqlCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        locations.Add(reader.GetString(0));
                    }
                }
            }
            return locations;
        }

        public bool IsInventoryNumberExists(string inventoryNumber, int? excludeId = null)
        {
            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                string sql = "SELECT COUNT(*) FROM exhibits WHERE LOWER(inventory_number) = LOWER(@inventory_number)";
                if (excludeId.HasValue)
                {
                    sql += $" AND id != {excludeId.Value}";
                }
                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@inventory_number", inventoryNumber);
                    long count = (long)cmd.ExecuteScalar();
                    return count > 0;
                }
            }
        }

        public List<ExhibitStatistics> GetPopularExhibits(int months = 12, int? teacherId = null)
        {
            var stats = new List<ExhibitStatistics>();
            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                string whereClause = $"WHERE i.issue_date >= @startDate";
                if (teacherId.HasValue) whereClause += $" AND i.teacher_id = {teacherId.Value}";

                string sql = $@"SELECT i.exhibit_id, COUNT(*) as issue_count, e.name, e.inventory_number
                               FROM issues i
                               JOIN exhibits e ON i.exhibit_id = e.id
                               {whereClause}
                               GROUP BY i.exhibit_id, e.name, e.inventory_number
                               ORDER BY issue_count DESC
                               LIMIT 10";

                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@startDate", DateTime.Now.AddMonths(-months));
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            stats.Add(new ExhibitStatistics
                            {
                                ExhibitId = reader.GetInt32(0),
                                IssueCount = reader.GetInt32(1),
                                ExhibitName = reader.GetString(2),
                                InventoryNumber = reader.GetString(3)
                            });
                        }
                    }
                }
            }
            return stats;
        }

        public List<TeacherStatistics> GetTeacherStatistics(int? teacherId = null)
        {
            var stats = new List<TeacherStatistics>();
            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                string whereClause = teacherId.HasValue ? $"WHERE t.id = {teacherId.Value}" : "";

                string sql = $@"SELECT t.id, t.fullname, t.department,
                               COUNT(i.id) as total_issues,
                               COUNT(CASE WHEN i.status = 'Выдан' AND i.planned_return_date < CURRENT_TIMESTAMP THEN 1 END) as overdue_count,
                               COUNT(CASE WHEN i.status = 'Возвращен' THEN 1 END) as returned_count
                               FROM teachers t
                               LEFT JOIN issues i ON t.id = i.teacher_id
                               {whereClause}
                               GROUP BY t.id, t.fullname, t.department
                               ORDER BY total_issues DESC";

                using (var cmd = new NpgsqlCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        stats.Add(new TeacherStatistics
                        {
                            TeacherId = reader.GetInt32(0),
                            TeacherName = reader.GetString(1),
                            Department = reader.IsDBNull(2) ? null : reader.GetString(2),
                            TotalIssues = reader.GetInt32(3),
                            OverdueCount = reader.GetInt32(4),
                            ReturnedCount = reader.GetInt32(5)
                        });
                    }
                }
            }
            return stats;
        }

        public string BackupDatabase(string backupPath)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["MuseumDB"].ConnectionString;
                var builder = new NpgsqlConnectionStringBuilder(connectionString);

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string backupFile = Path.Combine(backupPath, $"museum_backup_{timestamp}.sql");

                string pgDumpPath = @"C:\Program Files\PostgreSQL\16\bin\pg_dump.exe";
                if (!File.Exists(pgDumpPath))
                {
                    pgDumpPath = @"C:\Program Files (x86)\PostgreSQL\16\bin\pg_dump.exe";
                }

                if (!File.Exists(pgDumpPath))
                {
                    MessageBox.Show("Утилита pg_dump не найдена. Резервная копия не может быть создана.", 
                        "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return null;
                }

                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = pgDumpPath,
                    Arguments = $"-h {builder.Host} -p {builder.Port} -U {builder.Username} -F c -b -v -f \"{backupFile}\" {builder.Database}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                startInfo.EnvironmentVariables["PGPASSWORD"] = builder.Password;

                var process = System.Diagnostics.Process.Start(startInfo);
                process.WaitForExit(30000);

                if (process.ExitCode == 0 && File.Exists(backupFile))
                {
                    return backupFile;
                }
                else
                {
                    var error = process.StandardError.ReadToEnd();
                    throw new Exception($"Ошибка при создании резервной копии: {error}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Не удалось создать резервную копию: {ex.Message}");
            }
        }

        public void CleanupOrphanedPhotos()
        {
            try
            {
                string photosFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Photos");
                if (!Directory.Exists(photosFolder))
                    return;

                var allPhotoPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var exhibits = GetAllExhibits();

                foreach (var exhibit in exhibits)
                {
                    if (exhibit.PhotoPaths != null)
                    {
                        foreach (var path in exhibit.PhotoPaths)
                        {
                            if (!string.IsNullOrEmpty(path))
                            {
                                string normalizedPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                                allPhotoPaths.Add(normalizedPath);
                            }
                        }
                    }
                }

                int deletedCount = 0;
                foreach (var file in Directory.GetFiles(photosFolder))
                {
                    string normalizedFile = Path.GetFullPath(file).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    if (!allPhotoPaths.Contains(normalizedFile))
                    {
                        try
                        {
                            File.Delete(file);
                            deletedCount++;
                        }
                        catch { }
                    }
                }
            }
            catch { }
        }

        public void Dispose()
        {
            refreshTimer?.Stop();
            refreshTimer?.Dispose();
        }
    }
}
