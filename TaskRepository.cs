using MySql.Data.MySqlClient;
using System.Data;

namespace BOTBUDDY_CYBERSECURITY_CHATBOT
{
    public class TaskRepository
    {
        private readonly string _connectionString;

        public TaskRepository()
        {
            // UPDATE: Change the password below to match your MySQL root password
            string server = "localhost";
            string database = "botbuddy_db";
            string username = "root";
            string password = "your_password_here";  

            _connectionString = $"Server={server};Database={database};Uid={username};Pwd={password};";
        }

        public void InitializeDatabase()
        {
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    string createTable = @"
                CREATE TABLE IF NOT EXISTS Tasks (
                    Id INT AUTO_INCREMENT PRIMARY KEY,
                    Title VARCHAR(255) NOT NULL UNIQUE,
                    Description TEXT,
                    ReminderDate DATETIME,
                    IsCompleted BOOLEAN DEFAULT FALSE,
                    Category VARCHAR(50) DEFAULT 'Task',
                    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                )";
                    using (var cmd = new MySqlCommand(createTable, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }

                
                    try
                    {
                        string alterTable = "ALTER TABLE Tasks ADD COLUMN Category VARCHAR(50) DEFAULT 'Task'";
                        using (var cmd = new MySqlCommand(alterTable, conn))
                        {
                            cmd.ExecuteNonQuery();
                        }
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Database init error: {ex.Message}");
                throw;
            }
        }

        public void AddOrUpdateTask(string title, string category = "Task", string description = "", DateTime? reminderDate = null)
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                conn.Open();

                string checkSql = "SELECT Id FROM Tasks WHERE Title = @Title";
                using (var checkCmd = new MySqlCommand(checkSql, conn))
                {
                    checkCmd.Parameters.AddWithValue("@Title", title);
                    var existingId = checkCmd.ExecuteScalar();

                    if (existingId != null)
                    {
                        string updateSql = @"UPDATE Tasks SET 
                                    Description = @Description, 
                                    ReminderDate = @ReminderDate,
                                    IsCompleted = @IsCompleted,
                                    Category = @Category
                                    WHERE Title = @Title";
                        using (var updateCmd = new MySqlCommand(updateSql, conn))
                        {
                            updateCmd.Parameters.AddWithValue("@Title", title);
                            updateCmd.Parameters.AddWithValue("@Description", description ?? "");
                            updateCmd.Parameters.AddWithValue("@ReminderDate", reminderDate.HasValue ? (object)reminderDate.Value : DBNull.Value);
                            updateCmd.Parameters.AddWithValue("@IsCompleted", false);
                            updateCmd.Parameters.AddWithValue("@Category", category);
                            updateCmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        string insertSql = @"INSERT INTO Tasks (Title, Description, ReminderDate, IsCompleted, Category) 
                                    VALUES (@Title, @Description, @ReminderDate, @IsCompleted, @Category)";
                        using (var insertCmd = new MySqlCommand(insertSql, conn))
                        {
                            insertCmd.Parameters.AddWithValue("@Title", title);
                            insertCmd.Parameters.AddWithValue("@Description", description ?? "");
                            insertCmd.Parameters.AddWithValue("@ReminderDate", reminderDate.HasValue ? (object)reminderDate.Value : DBNull.Value);
                            insertCmd.Parameters.AddWithValue("@IsCompleted", false);
                            insertCmd.Parameters.AddWithValue("@Category", category); 
                            insertCmd.ExecuteNonQuery();
                        }
                    }
                }
            }
        }

        public List<TaskItem> GetAllTasks()
        {
            var tasks = new List<TaskItem>();
            using (var conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
               
                string sql = "SELECT Id, Title, Description, ReminderDate, IsCompleted, Category, CreatedAt FROM Tasks ORDER BY CreatedAt DESC";
                using (var cmd = new MySqlCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        tasks.Add(new TaskItem
                        {
                            Id = reader.GetInt32("Id"),
                            Title = reader.GetString("Title"),
                            Description = reader.IsDBNull("Description") ? "" : reader.GetString("Description"),
                            ReminderDate = reader.IsDBNull("ReminderDate") ? null : reader.GetDateTime("ReminderDate"),
                            IsCompleted = reader.GetBoolean("IsCompleted"),
                            Category = reader.IsDBNull("Category") ? "Task" : reader.GetString("Category"), // READ Category
                            CreatedAt = reader.GetDateTime("CreatedAt")
                        });
                    }
                }
            }
            return tasks;
        }

        public void CompleteTask(int id)
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                string sql = "UPDATE Tasks SET IsCompleted = TRUE WHERE Id = @Id";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void DeleteTask(int id)
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                string sql = "DELETE FROM Tasks WHERE Id = @Id";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public TaskItem? GetTaskById(int id)
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                string sql = "SELECT Id, Title, Description, ReminderDate, IsCompleted, CreatedAt FROM Tasks WHERE Id = @Id";
                using (var cmd = new MySqlCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new TaskItem
                        {
                            Id = reader.GetInt32("Id"),
                            Title = reader.GetString("Title"),
                            Description = reader.IsDBNull("Description") ? "" : reader.GetString("Description"),
                            ReminderDate = reader.IsDBNull("ReminderDate") ? null : reader.GetDateTime("ReminderDate"),
                            IsCompleted = reader.GetBoolean("IsCompleted"),
                            CreatedAt = reader.GetDateTime("CreatedAt")
                        };
                    }
                }
            }
            return null;
        }

        public TaskItem? GetTaskByTitle(string title)
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                string sql = "SELECT Id, Title, Description, ReminderDate, IsCompleted, CreatedAt FROM Tasks WHERE Title = @Title";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Title", title);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new TaskItem
                            {
                                Id = reader.GetInt32("Id"),
                                Title = reader.GetString("Title"),
                                Description = reader.IsDBNull("Description") ? "" : reader.GetString("Description"),
                                ReminderDate = reader.IsDBNull("ReminderDate") ? null : reader.GetDateTime("ReminderDate"),
                                IsCompleted = reader.GetBoolean("IsCompleted"),
                                CreatedAt = reader.GetDateTime("CreatedAt")
                            };
                        }
                    }
                }
            }
            return null;
        }

        public void ClearAll()
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                string sql = "DELETE FROM Tasks";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }

    public class TaskItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime? ReminderDate { get; set; }
        public bool IsCompleted { get; set; }
        public string Category { get; set; } = "Task"; 
        public DateTime CreatedAt { get; set; }
    }
}
