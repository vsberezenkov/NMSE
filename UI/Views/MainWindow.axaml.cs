using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using NMSE.UI.ViewModels;

namespace NMSE.UI.Views;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();

        _viewModel = new MainWindowViewModel();
        DataContext = _viewModel;

        Loaded += async (_, _) =>
        {
            var (x, y, w, h) = _viewModel.GetWindowState();
            if (w > 0 && h > 0)
            {
                Width = w;
                Height = h;
                Position = new PixelPoint(x, y);
            }

            await _viewModel.InitializeAsync();
            BuildLanguageMenu();
        };

        Closing += (_, _) =>
        {
            _viewModel.SaveWindowState(
                Position.X, Position.Y,
                (int)Bounds.Width, (int)Bounds.Height);
        };
    }

    private async void OnOpenDirectory(object? sender, RoutedEventArgs e)
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Save Directory",
            AllowMultiple = false
        });

        if (folders.Count > 0)
        {
            var path = folders[0].TryGetLocalPath();
            if (path != null)
                _viewModel.RecordRecentDirectory(path);
        }
    }

    private async void OnOpenFile(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Save File",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("NMS Save Files") { Patterns = new[] { "*.hg", "*.dat" } },
                new FilePickerFileType("All Files") { Patterns = new[] { "*" } }
            }
        });

        if (files.Count > 0)
        {
            var path = files[0].TryGetLocalPath();
            if (path != null)
                await _viewModel.LoadSaveDataAsync(path);
        }
    }

    private async void OnSaveAs(object? sender, RoutedEventArgs e)
    {
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save As",
            DefaultExtension = "hg",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("NMS Save Files") { Patterns = new[] { "*.hg" } },
                new FilePickerFileType("All Files") { Patterns = new[] { "*" } }
            }
        });

        if (file != null)
        {
            var path = file.TryGetLocalPath();
            if (path != null)
            {
                _viewModel.SetSaveFilePath(path);
                _viewModel.SaveCommand.Execute(null);
            }
        }
    }

    private void OnBrowseDirectory(object? sender, RoutedEventArgs e)
    {
        OnOpenDirectory(sender, e);
    }

    private void BuildLanguageMenu()
    {
        var languageMenu = this.FindControl<MenuItem>("LanguageMenu");
        if (languageMenu == null) return;

        languageMenu.Items.Clear();
        foreach (var lang in _viewModel.Languages)
        {
            var tag = lang.Tag;
            var item = new MenuItem { Header = tag };
            item.Click += (_, _) => _viewModel.SelectLanguageCommand.Execute(tag);
            languageMenu.Items.Add(item);
        }
    }

    private async void OnAbout(object? sender, RoutedEventArgs e)
    {
        var aboutWindow = new Window
        {
            Title = "About",
            Width = 380,
            Height = 200,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            Content = new StackPanel
            {
                Margin = new Thickness(24),
                Spacing = 8,
                Children =
                {
                    new TextBlock
                    {
                        Text = $"{MainWindowViewModel.AppName}",
                        FontSize = 16, FontWeight = Avalonia.Media.FontWeight.Bold
                    },
                    new TextBlock
                    {
                        Text = $"Build {BuildInfo.VerMajor}.{BuildInfo.VerMinor}.{BuildInfo.VerPatch} ({MainWindowViewModel.SuppGameRel})"
                    },
                    new TextBlock { Text = "" },
                    new TextBlock { Text = "by vector_cmdr" },
                    new TextBlock
                    {
                        Text = MainWindowViewModel.GitHubCreatorUrl,
                        Foreground = Avalonia.Media.Brushes.CornflowerBlue,
                        Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
                    }
                }
            }
        };

        await aboutWindow.ShowDialog(this);
    }

}
