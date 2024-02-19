using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Input;
using System.Collections.ObjectModel;

namespace Tracking_Tags_MAUI;

internal class TagsViewModel
{
    public TagsViewModel()
    {
        Tags = [];
        Pages = [];

        Pages.Add("abandoned");
        Pages.Add("longexposure");
        Pages.Add("reflection");
    }

    public void SetUserName(string userName, string pageName)
    {
        Tags.Clear();
        if (!string.IsNullOrEmpty(userName) && !string.IsNullOrEmpty(pageName))
        {
            Tags.Add(new TagViewModel($"snap_{pageName}_{userName}"));
            Tags.Add(new TagViewModel($"raw_{pageName}_{userName}"));
            Tags.Add(new TagViewModel($"snap_featured_{userName}"));
            Tags.Add(new TagViewModel($"raw_featured_{userName}"));
        }
    }

    public ObservableCollection<TagViewModel> Tags { get; }
    public ObservableCollection<string> Pages { get; }
}

internal class TagViewModel : ObservableObject
{
    private readonly Tag _tag = new();

    public TagViewModel(string text)
    {
        _tag.Text = text;
        CopyCommand = new AsyncRelayCommand(Copy);
    }

    public ICommand CopyCommand { get; private set; }

    public string Text
    {
        get => _tag.Text;
        set
        {
            if (_tag.Text != value)
            {
                _tag.Text = value;
                OnPropertyChanged();
            }
        }
    }

    private async Task Copy()
    {
		await Clipboard.Default.SetTextAsync(_tag.Text);
    }
}

internal class Tag
{
    public string Text = "";
}
