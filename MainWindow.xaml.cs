using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace WindowsBulkPhotoDetailsEditor;

public partial class MainWindow : Window
{
    private string? currentFolder;

    public MainWindow()
    {
        InitializeComponent();
    }

    private void SavePhoto_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: PhotoRow photo } ||
            !PhotosGrid.CommitEdit() || !PhotosGrid.CommitEdit(DataGridEditingUnit.Row, true))
        {
            return;
        }

        try
        {
            photo.Save();
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or COMException)
        {
            MessageBox.Show(this, $"Could not save {photo.FileName}.{Environment.NewLine}{Environment.NewLine}{error.Message}",
                "Photo save failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

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
            var photos = await Task.Run(() => PhotoReader.ReadFolder(folder));
            PhotosGrid.ItemsSource = photos;
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
