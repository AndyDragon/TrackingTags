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

        #region Selected page

        private LoadedPage? selectedPage = null;
        public LoadedPage? SelectedPage
        {
            get => selectedPage;
            set
            {
                if (Set(ref selectedPage, value))
                {
                    PopulateTags();
                    UserSettings.Store("Page", value?.Id ?? string.Empty);
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
            if (!string.IsNullOrEmpty(UserName) && SelectedPage != null)
            {
                var prefix = IncludeHash ? "#" : "";
                if (SelectedPage.Name.StartsWith('_'))
                {
                    Tags.Add(new Tag($"{prefix}{SelectedPage.Name[1..]}_{UserName}", this));
                }
                else if (!string.IsNullOrEmpty(SelectedPage.HubName))
                {
                    Tags.Add(new Tag($"{prefix}{SelectedPage.HubName}_{SelectedPage.PageName ?? SelectedPage.Name}_{UserName}", this));
                    Tags.Add(new Tag($"{prefix}{SelectedPage.HubName}_featured_{UserName}", this));
                }
                else
                {
                    Tags.Add(new Tag($"{prefix}snap_{SelectedPage.PageName ?? SelectedPage.Name}_{UserName}", this));
                    Tags.Add(new Tag($"{prefix}raw_{SelectedPage.PageName ?? SelectedPage.Name}_{UserName}", this));
                    Tags.Add(new Tag($"{prefix}snap_featured_{UserName}", this));
                    Tags.Add(new Tag($"{prefix}raw_featured_{UserName}", this));
                }
            }
        }

        public ObservableCollection<LoadedPage> Pages { get; private set; }

        private async Task LoadPages()
        {
            try
            {
                var pageName = UserSettings.Get("Page", string.Empty);

                // Disable client-side caching.
                httpClient.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue
                {
                    NoCache = true
                };
                var pagesUri = new Uri("https://vero.andydragon.com/static/data/pages.json");
                var content = await httpClient.GetStringAsync(pagesUri);
                var loadedPages = new List<LoadedPage>();
                LoadedPage? selectedPage = null;
                if (!string.IsNullOrEmpty(content))
                {
                    var pagesCatalog = JsonConvert.DeserializeObject<PagesCatalog>(content) ?? new PagesCatalog();
                    foreach (var page in pagesCatalog.Pages)
                    {
                        var loadedPage = new LoadedPage(page);
                        loadedPages.Add(loadedPage);
                        if (loadedPage.Id == pageName)
                        {
                            selectedPage = loadedPage;
                        }
                    }
                    if (pagesCatalog.Hubs != null)
                    {
                        var parts = WindowsIdentity.GetCurrent().Name.Split('\\');
                        var windowsUserName = parts.LastOrDefault();
                        foreach (var hub in pagesCatalog.Hubs)
                        {
                            foreach (var hubPage in hub.Value)
                            {
                                var canAddPage = true;
                                if (hubPage.Users != null)
                                {
                                    canAddPage = hubPage.Users.FirstOrDefault(user => string.Equals(user, windowsUserName, StringComparison.OrdinalIgnoreCase)) != null;
                                }
                                if (canAddPage)
                                {
                                    var loadedPage = new LoadedPage(hub.Key, hubPage);
                                    loadedPages.Add(loadedPage);
                                    if (loadedPage.Id == pageName)
                                    {
                                        selectedPage = loadedPage;
                                    }
                                }
                            }
                        }
                    }
                }
                foreach (var loadedPage in loadedPages.OrderBy(page => page, LoadedPageComparer.Default))
                {
                    Pages.Add(loadedPage);
                    SelectedPage = selectedPage;
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

    internal class LoadedPageComparer : IComparer<LoadedPage>
    {
        public int Compare(LoadedPage? x, LoadedPage? y)
        {
            if (x == null && y == null)
            {
                return 0;
            }
            if (x == null)
            {
                return -1;
            }
            if (y == null)
            {
                return 1;
            }
            if (x.Id.StartsWith('_')&& y.Id.StartsWith('_'))
            {
                return string.Compare(x.Id, y.Id, true);
            }
            if (x.Id.StartsWith('_'))
            {
                return 1;
            }
            if (y.Id.StartsWith('_'))
            {
                return -1;
            }
            int hubCompare = string.Compare(x.HubName ?? "snap", y.HubName ?? "snap", true);
            if (hubCompare == 0)
            {
                return string.Compare(x.Name, y.Name, true);
            }
            return hubCompare;
        }

        public static readonly LoadedPageComparer Default = new();
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
            Hubs = new Dictionary<string, IList<HubPageEntry>>();
        }

        public IList<PageEntry> Pages { get; set; }

        public IDictionary<string, IList<HubPageEntry>> Hubs { get; set; }
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

    public class HubPageEntry
    {
        public HubPageEntry()
        {
            Name = string.Empty;
        }

        public string Name { get; set; }
        public string? PageName { get; set; }
        public IList<string>? Users { get; set; }
    }

    public class LoadedPage
    {
        public LoadedPage(PageEntry page)
        {
            Name = page.Name;
            PageName = page.PageName;
        }

        public LoadedPage(string hubName, HubPageEntry page)
        {
            HubName = hubName;
            Name = page.Name;
            PageName = page.PageName;
        }

        public string Id
        {
            get
            {
                if (!string.IsNullOrEmpty(HubName))
                {
                    return $"{HubName}:{Name}";
                }
                return Name;
            }
        }
        public string Name { get; private set; }
        public string? PageName { get; private set; }
        public string? HubName { get; private set; }
        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrEmpty(HubName))
                {
                    return $"{HubName}_{Name}";
                }
                return Name;
            }
        }
    }

    public class ThemeOption(Theme theme, bool isSelected = false)
    {
        public Theme Theme { get; } = theme;

        public bool IsSelected { get; } = isSelected;
    }
}
