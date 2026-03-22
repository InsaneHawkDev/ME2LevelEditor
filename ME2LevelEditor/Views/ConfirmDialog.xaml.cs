using System.Windows;
using System.Windows.Controls;
using ME2LevelEditor.Services;

namespace ME2LevelEditor.Views
{
    public enum ConfirmResult
    {
        Yes,
        No,
        Cancel
    }

    public partial class ConfirmDialog : Window
    {
        public ConfirmResult Result { get; private set; } = ConfirmResult.Cancel;

        public string DialogTitle { get; set; }

        private ConfirmDialog()
        {
            InitializeComponent();
            DataContext = this;
        }

        private Button CreateButton(string text, ConfirmResult result, bool isAccent = false)
        {
            var btn = new Button
            {
                Content = text,
                Padding = new Thickness(16, 6, 16, 6),
                Margin = new Thickness(8, 0, 0, 0),
                MinWidth = 70
            };
            if (isAccent)
            {
                btn.Background = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#2a3a5c"));
                btn.BorderBrush = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#00c8ff"));
            }
            btn.Click += (s, e) =>
            {
                Result = result;
                DialogResult = true;
                Close();
            };
            return btn;
        }

        public static ConfirmResult Show(Window owner, string message, string title, bool showCancel = false)
        {
            var dialog = new ConfirmDialog
            {
                DialogTitle = title,
                Owner = owner
            };
            dialog.MessageText.Text = message;

            dialog.ButtonPanel.Children.Add(
                dialog.CreateButton(LocalizationService.T("Btn_Yes"), ConfirmResult.Yes, true));
            dialog.ButtonPanel.Children.Add(
                dialog.CreateButton(LocalizationService.T("Btn_No"), ConfirmResult.No));
            if (showCancel)
            {
                dialog.ButtonPanel.Children.Add(
                    dialog.CreateButton(LocalizationService.T("Btn_Cancel"), ConfirmResult.Cancel));
            }

            dialog.ShowDialog();
            return dialog.Result;
        }
    }
}
