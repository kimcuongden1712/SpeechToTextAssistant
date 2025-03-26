using System;
using System.Windows;

namespace SpeechToTextAssistant
{
    public partial class App : Application
    {
        [STAThread]
        public static void Main()
        {
            var app = new App();
            app.InitializeComponent();
            app.Run();
        }
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