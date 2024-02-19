using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using Newtonsoft.Json;
using Notification.Wpf;

using Tracking_Tags.Properties;

namespace Tracking_Tags
{
    internal class MainViewModel : INotifyPropertyChanged
    {
        private readonly HttpClient httpClient = new();
        private readonly NotificationManager notificationManager = new();

        public MainViewModel()
        {
            Tags = [];
            Pages = [];
            Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "---";

            _ = LoadPages();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public string Version { get; set; }

        #region Themes

        private string themeName = "";

        public string ThemeName
        {
            get
            {
                return themeName switch
                {
                    "SoftDark" => "Soft dark",
                    "LightTheme" => "Light",
                    "DeepDark" => "Deep dark",
                    "DarkGreyTheme" => "Dark gray",
                    "GreyTheme" => "Gray",
                    _ => themeName,
                };
            }
            set
            {
                if (themeName != value)
                {
                    themeName = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ThemeName)));
                }
            }
        }

        #endregion

        #region User name

        private string userName = string.Empty;
        public string UserName 
        {
            get { return userName; } 
            set 
            {
                if (userName != value)
                {
                    userName = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UserName)));
                    PopulateTags();
                }
            } 
        }

        #endregion

        #region Page name

        private string pageName = Settings.Default.Page ?? string.Empty;
        public string PageName
        {
            get { return pageName; }
            set
            {
                if (pageName != value)
                {
                    pageName = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PageName)));
                    PopulateTags();
                    Settings.Default.Page = value;
                    Settings.Default.Save();
                }
            }
        }

        #endregion

        #region Include hash

        private bool includeHash = Settings.Default.IncludeHash;
        public bool IncludeHash
        {
            get { return includeHash; }
            set
            {
                if (includeHash != value)
                {
                    includeHash = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IncludeHash)));
                    PopulateTags();
                    Settings.Default.IncludeHash = value;
                    Settings.Default.Save();
                }
            }        
        }

        #endregion

        public ObservableCollection<Tag> Tags { get; private set; }

        private void PopulateTags()
        {
            Tags.Clear();
            if (!string.IsNullOrEmpty(UserName) && !string.IsNullOrEmpty(PageName))
            {
                var prefix = IncludeHash ? "#" : "";
                Tags.Add(new Tag($"{prefix}snap_{PageName}_{UserName}", notificationManager));
                Tags.Add(new Tag($"{prefix}raw_{PageName}_{UserName}", notificationManager));
                Tags.Add(new Tag($"{prefix}snap_featured_{UserName}", notificationManager));
                Tags.Add(new Tag($"{prefix}raw_featured_{UserName}", notificationManager));
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
                // TODO andydragon : handle errors
                Console.WriteLine("Error occurred: {0}", ex.Message);
            }
        }
    }

    internal class Tag(string text, NotificationManager notificationManager)
    {
        public string Text { get; set; } = text;

        private ICommand? copyCommand = null;

        public ICommand Copy
        {
            get
            {
                return copyCommand ??= new CommandHandler(
                    () => 
                    {
                        Clipboard.SetText(Text);
                        notificationManager.Show(
                            "Copied",
                            "Copied the tag to the clipboard",
                            type: NotificationType.Success,
                            areaName: "WindowArea",
                            expirationTime: TimeSpan.FromSeconds(1.5));
                    }, 
                    () => !string.IsNullOrEmpty(Text));
            }
        }
    }

    public class CommandHandler(Action action, Func<bool> canExecute) : ICommand
    {
        private readonly Action action = action;
        private readonly Func<bool> canExecute = canExecute;

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter)
        {
            return canExecute();
        }

        public void Execute(object? parameter)
        {
            action();
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
}
