using System.Windows;

namespace BOTBUDDY_CYBERSECURITY_CHATBOT
{
    public partial class App : Application
    {
        // Override the OnStartup method
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Database auto-initialization
            try
            {
                var repo = new TaskRepository();
                repo.InitializeDatabase();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Database setup failed: " + ex.Message);
            }
        }
    }
}