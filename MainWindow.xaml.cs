using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace WindowsBulkPhotoDetailsEditor;

public partial class MainWindow : Window
{
    private string? currentFolder;
    private IReadOnlyList<PhotoRow> photos = [];

    public MainWindow()
    {
        InitializeComponent();
    }

    private void SavePhoto_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: PhotoRow photo } ||
            !CommitGridEdits())
        {
            return;
        }

        if (TrySavePhoto(photo) is { } error)
        {
            ShowSaveErrors(error);
        }
    }

    private void SaveAll_Click(object sender, RoutedEventArgs e) => SaveAll();

    private void Exit_Click(object sender, RoutedEventArgs e) => Close();

    private void About_Click(object sender, RoutedEventArgs e)
    {
        string? version = typeof(MainWindow).Assembly.GetName().Version?.ToString(3);
        MessageBox.Show(this,
            $"Version {version}\n\nBrowse photos and thumbnails in a folder, edit their Title, Subject, and Comments, and save those details to one photo or all edited photos.",
            $"About {Title}", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);
        if (e.Cancel || !photos.Any(photo => photo.HasChanges))
        {
            return;
        }

        var choice = MessageBox.Show(this, "Save all unsaved photo edits before exiting?",
            "Unsaved photo edits", MessageBoxButton.YesNoCancel, MessageBoxImage.Question, MessageBoxResult.Cancel);
        e.Cancel = choice switch
        {
            MessageBoxResult.Yes => !SaveAll(),
            MessageBoxResult.No => false,
            _ => true,
        };
    }

    private bool SaveAll()
    {
        if (!CommitGridEdits())
        {
            return false;
        }

        var errors = new List<string>();
        foreach (var photo in photos.Where(photo => photo.HasChanges))
        {
            if (TrySavePhoto(photo) is { } error)
            {
                errors.Add(error);
            }
        }

        if (errors.Count == 0)
        {
            return true;
        }

        ShowSaveErrors(string.Join($"{Environment.NewLine}{Environment.NewLine}", errors));
        return false;
    }

    private bool CommitGridEdits() =>
        PhotosGrid.CommitEdit() && PhotosGrid.CommitEdit(DataGridEditingUnit.Row, true);

    private static string? TrySavePhoto(PhotoRow photo)
    {
        try
        {
            photo.Save();
            return null;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or COMException)
        {
            return $"Could not save {photo.FileName}.{Environment.NewLine}{error.Message}";
        }
    }

    private void ShowSaveErrors(string message) =>
        MessageBox.Show(this, message, "Photo save failed", MessageBoxButton.OK, MessageBoxImage.Error);

    private void SetPhotos(IReadOnlyList<PhotoRow> loadedPhotos)
    {
        foreach (var photo in photos)
        {
            photo.PropertyChanged -= Photo_PropertyChanged;
        }

        photos = loadedPhotos;
        foreach (var photo in photos)
        {
            photo.PropertyChanged += Photo_PropertyChanged;
        }

        PhotosGrid.ItemsSource = photos;
        UpdateSaveMenu();
    }

    private void Photo_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PhotoRow.HasChanges))
        {
            UpdateSaveMenu();
        }
    }

    private void UpdateSaveMenu() => SaveMenuItem.IsEnabled = photos.Any(photo => photo.HasChanges);

    private async void SelectFolder_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog { Title = "Select a photo folder" };
        if (currentFolder is not null)
        {
            dialog.InitialDirectory = currentFolder;
        }

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        SelectFolderButton.IsEnabled = false;
        StatusText.Text = "Loading photos…";

        try
        {
            string folder = dialog.FolderName;
            var loadedPhotos = await Task.Run(() => PhotoReader.ReadFolder(folder));
            SetPhotos(loadedPhotos);
            currentFolder = folder;
            FolderPath.Text = folder;
            StatusText.Text = photos.Count switch
            {
                0 => "No readable photos in this folder.",
                1 => "1 photo",
                _ => $"{photos.Count:N0} photos",
            };
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            StatusText.Text = $"Could not open {dialog.FolderName}. {error.Message}";
        }
        finally
        {
            SelectFolderButton.IsEnabled = true;
        }
    }
}
