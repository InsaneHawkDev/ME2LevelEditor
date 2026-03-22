using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Navigation;

namespace ME2LevelEditor.Views
{
    public partial class AboutDialog : Window
    {
        public AboutDialog()
        {
            InitializeComponent();
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            VersionText.Text = version != null
                ? Services.LocalizationService.T("Msg_Version", version.Major, version.Minor, version.Build)
                : Services.LocalizationService.T("Msg_Version", 1, 0, 0);
        }

        private void OnLinkNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }

        private void OnClose(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
