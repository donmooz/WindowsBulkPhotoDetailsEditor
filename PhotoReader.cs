using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;

namespace WindowsBulkPhotoDetailsEditor;

internal static class PhotoReader
{
    private const int ThumbnailSize = 192;

    public static List<PhotoRow> ReadFolder(string folder)
    {
        var photos = new List<PhotoRow>();
        foreach (string path in Directory.EnumerateFiles(folder).Order(StringComparer.OrdinalIgnoreCase))
        {
            if (ReadPhoto(path) is { } photo)
            {
                photos.Add(photo);
            }
        }

        return photos;
    }

    private static PhotoRow? ReadPhoto(string path)
    {
        try
        {
            using var file = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            var decoder = BitmapDecoder.Create(file, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);
            var frame = decoder.Frames[0];
            var metadata = ReadMetadata(() => frame.Metadata as BitmapMetadata);
            string title = ReadMetadata(() => metadata?.Title) ?? "";
            string subject = ReadMetadata(() => metadata?.Subject) ?? "";
            string comments = ReadMetadata(() => metadata?.Comment) ?? "";
            bool isLandscape = frame.PixelWidth >= frame.PixelHeight;

            // Decode a small, self-contained image before closing the file.
            file.Position = 0;
            var thumbnail = new BitmapImage();
            thumbnail.BeginInit();
            thumbnail.CacheOption = BitmapCacheOption.OnLoad;
            thumbnail.StreamSource = file;
            if (isLandscape)
            {
                thumbnail.DecodePixelWidth = ThumbnailSize;
            }
            else
            {
                thumbnail.DecodePixelHeight = ThumbnailSize;
            }

            thumbnail.EndInit();
            thumbnail.Freeze();

            return new PhotoRow(path, thumbnail, new PhotoDetails(title, subject, comments));
        }
        catch (Exception error) when (IsImageReadError(error) || error is UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static T? ReadMetadata<T>(Func<T?> read) where T : class
    {
        try
        {
            return read();
        }
        catch (Exception error) when (IsImageReadError(error))
        {
            return null;
        }
    }

    private static bool IsImageReadError(Exception error) =>
        error is IOException or FormatException or NotSupportedException or COMException;
}
