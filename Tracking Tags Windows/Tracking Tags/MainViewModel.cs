﻿using System.Collections.ObjectModel;
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
using System.Text;
using System.Security.Cryptography;
using System.Windows.Media;

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
                        OnPropertyChanged(nameof(StatusBarBrush));
                        OnPropertyChanged(nameof(Themes));
                    }
                }
            }
        }

        public ThemeOption[] Themes => [.. ThemeManager.Current.Themes.OrderBy(theme => theme.Name).Select(theme => new ThemeOption(theme, theme == Theme))];

        private bool windowActive = false;
        public bool WindowActive
        {
            get => windowActive;
            set
            {
                if (Set(ref windowActive, value))
                {
                    OnPropertyChanged(nameof(StatusBarBrush));
                }
            }
        }

        public Brush? StatusBarBrush => WindowActive
            ? Theme?.Resources["MahApps.Brushes.Accent2"] as Brush
            : Theme?.Resources["MahApps.Brushes.WindowTitle.NonActive"] as Brush;

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
                if (SelectedPage.HubName == "other")
                {
                    Tags.Add(new Tag($"{prefix}{SelectedPage.PageName ?? SelectedPage.Name}_{UserName}", this));
                }
                else if (SelectedPage.HubName == "snap")
                {
                    Tags.Add(new Tag($"{prefix}snap_{SelectedPage.PageName ?? SelectedPage.Name}_{UserName}", this));
                    Tags.Add(new Tag($"{prefix}raw_{SelectedPage.PageName ?? SelectedPage.Name}_{UserName}", this));
                    Tags.Add(new Tag($"{prefix}snap_featured_{UserName}", this));
                    Tags.Add(new Tag($"{prefix}raw_featured_{UserName}", this));
                }
                else
                {
                    Tags.Add(new Tag($"{prefix}{SelectedPage.HubName}_{SelectedPage.PageName ?? SelectedPage.Name}_{UserName}", this));
                    Tags.Add(new Tag($"{prefix}{SelectedPage.HubName}_featured_{UserName}", this));
                }
            }
        }

        public ObservableCollection<LoadedPage> Pages { get; private set; }

        private async Task LoadPages()
        {
            try
            {
                var pageName = UserSettings.Get("Page", string.Empty);
                var pageParts = pageName.Split(':');
                if (pageParts.Length == 1) 
                {
                    pageName = "snap:" + pageParts[0];
                }

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
                    var pagesCatalog = JsonConvert.DeserializeObject<ScriptsCatalog>(content) ?? new ScriptsCatalog();
                    if (pagesCatalog.Hubs != null)
                    {
                        foreach (var hub in pagesCatalog.Hubs)
                        {
                            foreach (var hubPage in hub.Value)
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

        private static string ComputeSHA256(string s)
        {
            // Compute the hash of the given string
            byte[] hashValue = SHA256.HashData(Encoding.UTF8.GetBytes(s));

            // Convert the byte array to string format
            string hash = string.Empty;
            foreach (byte b in hashValue)
            {
                hash += $"{b:X2}";
            }

            return hash;
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
            if (x.HubName == "other" && y.HubName == "other")
            {
                return string.Compare(x.DisplayName, y.DisplayName, true);
            }
            if (x.HubName == "other")
            {
                return 1;
            }
            if (y.HubName == "other")
            {
                return -1;
            }
            return string.Compare(x.DisplayName, y.DisplayName, true);
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

    public class ScriptsCatalog
    {
        public ScriptsCatalog()
        {
            Hubs = new Dictionary<string, IList<PageEntry>>();
        }

        public IDictionary<string, IList<PageEntry>> Hubs { get; set; }
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

    public class LoadedPage
    {
        public LoadedPage(string hubName, PageEntry page)
        {
            HubName = hubName;
            Name = page.Name;
            PageName = page.PageName;
        }

        public string Id
        {
            get
            {
                if (string.IsNullOrEmpty(HubName))
                {
                    return Name;
                }
                return $"{HubName}:{Name}";
            }
        }

        public string HubName { get; private set; }
        public string Name { get; private set; }
        public string? PageName { get; private set; }
        public string DisplayName
        {
            get
            {
                if (HubName == "other")
                {
                    return Name;
                }
                return $"{HubName}_{Name}";
            }
        }
    }

    public class ThemeOption(Theme theme, bool isSelected = false)
    {
        public Theme Theme { get; } = theme;

        public bool IsSelected { get; } = isSelected;
    }
}
