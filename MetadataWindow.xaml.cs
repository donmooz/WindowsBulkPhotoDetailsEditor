using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using MetadataExtractor;

namespace WindowsBulkPhotoDetailsEditor;

public partial class MetadataWindow : Window
{
    internal MetadataWindow(Window owner, string path)
    {
        InitializeComponent();
        Owner = owner;
        Icon = owner.Icon;
        Title = $"Metadata — {Path.GetFileName(path)}";
        Loaded += async (_, _) => await LoadMetadataAsync(path);
    }

    private async Task LoadMetadataAsync(string path)
    {
        try
        {
            JsonText.Text = await Task.Run(() => PhotoMetadataJson.Read(path));
            CopyButton.IsEnabled = true;
        }
        catch (Exception error) when (error is ImageProcessingException or UnauthorizedAccessException ||
            PhotoReader.IsImageReadError(error))
        {
            JsonText.Text = $"Could not read metadata.{Environment.NewLine}{error.Message}";
        }
    }

    private void Copy_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Clipboard.SetText(JsonText.Text);
        }
        catch (ExternalException error)
        {
            MessageBox.Show(this, error.Message, "Could not copy JSON", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
