using System.Windows;

namespace SpeechToTextAssistant
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            // Initialize services and other startup logic here
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Clean up resources and services here
            base.OnExit(e);
        }
    }
}