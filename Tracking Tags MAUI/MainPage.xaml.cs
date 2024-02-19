namespace Tracking_Tags_MAUI;

public partial class MainPage : ContentPage
{
	public MainPage()
	{
		InitializeComponent();
	}

	private void OnInputChanged(object sender, EventArgs e)
	{
		var userName = UserNameEntry.Text;
		var pageName = PagePicker.SelectedItem as String ?? "";

        if (BindingContext is TagsViewModel viewModel)
        {
            viewModel.SetUserName(userName, pageName);
        }
	}

	private async void OnPasteUserName(object sender, EventArgs e)
	{
		UserNameEntry.Text = await Clipboard.Default.GetTextAsync();
	}
}
