using FramePFX.Themes;
using Microsoft.Win32;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Tracking_Tags.Properties;

namespace Tracking_Tags
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            var theme = Settings.Default.Theme;
            switch (theme)
            {
                case "SoftDark": ThemesController.SetTheme(ThemeType.SoftDark); break;
                case "LightTheme": ThemesController.SetTheme(ThemeType.LightTheme); break;
                case "DeepDark": ThemesController.SetTheme(ThemeType.DeepDark); break;
                case "DarkGreyTheme": ThemesController.SetTheme(ThemeType.DarkGreyTheme); break;
                case "GreyTheme": ThemesController.SetTheme(ThemeType.GreyTheme); break;
                default:
                    {
                        if (IsLightTheme())
                        {
                            ThemesController.SetTheme(ThemeType.LightTheme);
                        }
                        break;
                    }
            }

            if (DataContext is MainViewModel viewModel)
            {
                viewModel.ThemeName = ThemesController.CurrentTheme.GetName();
            }
        }

        private static bool IsLightTheme()
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var value = key?.GetValue("AppsUseLightTheme");
            return value is int i && i > 0;
        }

        private void OnThemeClick(object sender, RoutedEventArgs e)
        {
            switch (ThemesController.CurrentTheme.GetName())
            {
                case "SoftDark": ThemesController.SetTheme(ThemeType.LightTheme); break;
                case "LightTheme": ThemesController.SetTheme(ThemeType.DeepDark); break;
                case "DeepDark": ThemesController.SetTheme(ThemeType.DarkGreyTheme); break;
                case "DarkGreyTheme": ThemesController.SetTheme(ThemeType.GreyTheme); break;
                case "GreyTheme": ThemesController.SetTheme(ThemeType.SoftDark); break;
            }

            Settings.Default.Theme = ThemesController.CurrentTheme.GetName();
            Settings.Default.Save();

            if (DataContext is MainViewModel viewModel)
            {
                viewModel.ThemeName = ThemesController.CurrentTheme.GetName();
            }
        }

        private void OnPaste(object sender, RoutedEventArgs e)
        {
            if (Clipboard.ContainsText())
            {
                if (DataContext is MainViewModel viewModel)
                {
                    viewModel.UserName = Clipboard.GetText();
                }
            }    
        }
    }
}