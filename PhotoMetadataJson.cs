using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.Iptc;
using MetadataExtractor.Formats.Xmp;
using MetadataDirectory = MetadataExtractor.Directory;

namespace WindowsBulkPhotoDetailsEditor;

internal static class PhotoMetadataJson
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string Read(string path)
    {
        using var file = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        IReadOnlyList<MetadataDirectory> directories = ImageMetadataReader.ReadMetadata(file);
        PhotoDetails? windowsProperties = null;
        string? windowsPropertiesError = null;
        try
        {
            windowsProperties = PhotoMetadata.Read(path);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or COMException)
        {
            windowsPropertiesError = error.Message;
        }

        return JsonSerializer.Serialize(new
        {
            fileName = Path.GetFileName(path),
            windowsProperties,
            windowsPropertiesError,
            exif = directories.Where(directory => directory is ExifDirectoryBase or GpsDirectory)
                .Select(DescribeTags).ToArray(),
            xmp = directories.OfType<XmpDirectory>().Select(directory => directory.GetXmpProperties()).ToArray(),
            iptc = directories.OfType<IptcDirectory>().Select(DescribeTags).ToArray(),
            errors = directories.SelectMany(directory => directory.Errors.Select(error => $"{directory.Name}: {error}"))
                .ToArray()
        }, JsonOptions);
    }

    private static TagDirectory DescribeTags(MetadataDirectory directory) => new(
        directory.Name,
        directory.Tags.Select(tag => new TagValue(tag.Type, tag.Name,
            tag.Description ?? directory.GetString(tag.Type))).ToArray());

    private sealed record TagDirectory(string Name, TagValue[] Tags);
    private sealed record TagValue(int Id, string Name, string? Value);
}
