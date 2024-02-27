using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Reflection;
using System.Security.Principal;
using System.Windows;
using System.Windows.Input;
using Newtonsoft.Json;
using Notification.Wpf;
using ControlzEx.Theming;

namespace TrackingTags
{
    internal class MainViewModel : NotifyPropertyChanged
    {
        private readonly HttpClient httpClient = new();
        private readonly NotificationManager notificationManager = new();

        public MainViewModel()
        {
            Tags = [];
            Pages = [];

            _ = LoadPages();
        }

        #region Data locations

        private static string GetDataLocationPath()
        {
            var user = WindowsIdentity.GetCurrent();
            var dataLocationPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "AndyDragonSoftware",
                "TrackingTags",
                user.Name);
            if (!Directory.Exists(dataLocationPath))
            {
                Directory.CreateDirectory(dataLocationPath);
            }
            return dataLocationPath;
        }

        public static string GetUserSettingsPath()
        {
            var dataLocationPath = GetDataLocationPath();
            return Path.Combine(dataLocationPath, "settings.json");
        }

        #endregion

        #region Themes

        private Theme? theme = ThemeManager.Current.DetectTheme();
        public Theme? Theme
        {
            get => theme;
            set
            {
                if (Set(ref theme, value))
                {
                    if (Theme != null)
                    {
                        ThemeManager.Current.ChangeTheme(Application.Current, Theme);
                        UserSettings.Store("theme", Theme.Name);
                    }
                }
            }
        }

        public ThemeOption[] Themes => [.. ThemeManager.Current.Themes.OrderBy(theme => theme.Name).Select(theme => new ThemeOption(theme, theme == Theme))];

        #endregion

        #region Version
        public static string Version => Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "---";

        #endregion

        #region User name

        private string userName = string.Empty;
        public string UserName 
        {
            get => userName;
            set 
            {
                if (Set(ref userName, value))
                {
                    PopulateTags();
                }
            } 
        }

        #endregion

        #region Page name

        private string pageName = UserSettings.Get("Page", string.Empty);
        public string PageName
        {
            get => pageName;
            set
            {
                if (Set(ref pageName, value))
                {
                    PopulateTags();
                    UserSettings.Store("Page", value);
                }
            }
        }

        #endregion

        #region Include hash

        private bool includeHash = UserSettings.Get("IncludeHash", false);
        public bool IncludeHash
        {
            get => includeHash;
            set
            {
                if (Set(ref includeHash, value))
                {
                    PopulateTags();
                    UserSettings.Store("IncludeHash", value);
                }
            }        
        }

        #endregion

        public void ShowToast(
            string title,
            string message,
            NotificationType type = NotificationType.Success,
            TimeSpan? duration = null)
        {
            notificationManager.Show(title, message, type: type, areaName: "WindowArea", expirationTime: duration ?? TimeSpan.FromSeconds(3));
        }

        private ICommand? pasteUserCommand = null;
        public ICommand PasteUserCommand
        {
            get
            {
                pasteUserCommand ??= new Command(
                    () =>
                    {
                        if (Clipboard.ContainsText())
                        {
                            UserName = Clipboard.GetText();
                        }
                    });
                return pasteUserCommand;
            }
        }

        private ICommand? setThemeCommand = null;
        public ICommand SetThemeCommand
        {
            get
            {
                setThemeCommand ??= new CommandWithParameter(
                    (parameter) =>
                    {
                        if (parameter is Theme theme)
                        {
                            Theme = theme;
                        }
                    });
                return setThemeCommand;
            }
        }

        public ObservableCollection<Tag> Tags { get; private set; }

        private void PopulateTags()
        {
            Tags.Clear();
            if (!string.IsNullOrEmpty(UserName) && !string.IsNullOrEmpty(PageName))
            {
                var prefix = IncludeHash ? "#" : "";
                Tags.Add(new Tag($"{prefix}snap_{PageName}_{UserName}", this));
                Tags.Add(new Tag($"{prefix}raw_{PageName}_{UserName}", this));
                Tags.Add(new Tag($"{prefix}snap_featured_{UserName}", this));
                Tags.Add(new Tag($"{prefix}raw_featured_{UserName}", this));
            }
        }

        public ObservableCollection<string> Pages { get; private set; }

        private async Task LoadPages()
        {
            try
            {
                // Disable client-side caching.
                httpClient.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue
                {
                    NoCache = true
                };
                var pagesUri = new Uri("https://vero.andydragon.com/static/data/pages.json");
                var content = await httpClient.GetStringAsync(pagesUri);
                if (!string.IsNullOrEmpty(content))
                {
                    var pagesCatalog = JsonConvert.DeserializeObject<PagesCatalog>(content) ?? new PagesCatalog();
                    foreach (var page in pagesCatalog.Pages.Select(page => page.Name))
                    {
                        Pages.Add(page);
                    }
                }
            }
            catch (Exception ex)
            {
                ShowToast(
                    "Failed to load pages",
                    $"Failed to load the pages from the server: {ex.Message}",
                    NotificationType.Error,
                    TimeSpan.FromSeconds(10));
                
            }
        }
    }

    internal class Tag(string text, MainViewModel viewModel)
    {
        public string Text { get; set; } = text;

        private ICommand? copyCommand = null;

        public ICommand Copy
        {
            get
            {
                copyCommand ??= new Command(
                    () =>
                    {
                        Clipboard.SetText(Text);
                        viewModel.ShowToast(
                            "Copied",
                            "Copied the tag to the clipboard",
                            NotificationType.Success,
                            TimeSpan.FromSeconds(1.5));
                    },
                    () => !string.IsNullOrEmpty(Text));
                return copyCommand;
            }
        }
    }

    public class PagesCatalog
    {
        public PagesCatalog()
        {
            Pages = [];
        }

        public PageEntry[] Pages { get; set; }
    }

    public class PageEntry
    {
        public PageEntry()
        {
            Name = string.Empty;
        }

        public string Name { get; set; }
        public string? PageName { get; set; }
    }

    public class ThemeOption(Theme theme, bool isSelected = false)
    {
        public Theme Theme { get; } = theme;

        public bool IsSelected { get; } = isSelected;
    }
}
