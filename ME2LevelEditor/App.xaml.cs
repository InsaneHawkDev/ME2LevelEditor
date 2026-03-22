using System.Windows;
using ME2LevelEditor.Services;

namespace ME2LevelEditor
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            LocalizationService.Init();
        }
    }
}
