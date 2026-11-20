using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;

namespace WindowsBulkPhotoDetailsEditor;

internal sealed record PhotoDetails(string Title, string Subject, string Comments);

internal sealed class PhotoRow(string path, BitmapSource thumbnail, PhotoDetails details)
    : INotifyPropertyChanged, IEditableObject
{
    private PhotoDetails saved = details;
    private PhotoDetails current = details;
    private PhotoDetails? beforeEdit;

    public string FileName => Path.GetFileName(path);
    public BitmapSource Thumbnail { get; } = thumbnail;
    public bool HasChanges => current != saved;

    public string Title
    {
        get => current.Title;
        set => Update(current with { Title = value });
    }

    public string Subject
    {
        get => current.Subject;
        set => Update(current with { Subject = value });
    }

    public string Comments
    {
        get => current.Comments;
        set => Update(current with { Comments = value });
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void BeginEdit() => beforeEdit ??= current;

    public void EndEdit() => beforeEdit = null;

    public void CancelEdit()
    {
        if (beforeEdit is { } previous)
        {
            beforeEdit = null;
            Title = previous.Title;
            Subject = previous.Subject;
            Comments = previous.Comments;
        }
    }

    public void Save()
    {
        if (!HasChanges)
        {
            return;
        }

        PhotoMetadata.Save(path, saved, current);
        saved = current;
        PropertyChanged?.Invoke(this, new(nameof(HasChanges)));
    }

    private void Update(PhotoDetails value, [CallerMemberName] string? propertyName = null)
    {
        if (value == current)
        {
            return;
        }

        bool wasChanged = HasChanges;
        current = value;
        PropertyChanged?.Invoke(this, new(propertyName));
        if (wasChanged != HasChanges)
        {
            PropertyChanged?.Invoke(this, new(nameof(HasChanges)));
        }
    }
}
