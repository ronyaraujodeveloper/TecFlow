using Ofertio.Mobile.Services.Push;

namespace Ofertio.Mobile;

public partial class MainPage : ContentPage
{
    private MobilePushCoordinator? _pushCoordinator;

    public MainPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        _pushCoordinator ??= Handler?.MauiContext?.Services.GetService<MobilePushCoordinator>();
        _pushCoordinator?.Start();
        PushBootstrap.RequestTokenIfAvailable();
    }

    protected override void OnDisappearing()
    {
        _pushCoordinator?.Stop();
        base.OnDisappearing();
    }
}
