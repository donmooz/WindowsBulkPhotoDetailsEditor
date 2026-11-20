# Windows Bulk Photo Details Editor

A C# Windows desktop app built with WPF and .NET 10. Select a folder to browse readable photos with thumbnails and edit their Title, Subject, and Comments. **Save** writes the changed fields for that photo to its file. Writable fields depend on the Windows property handler for the file format.

## Build and run

Install the .NET 10 SDK on Windows, then run these commands from this folder:

```powershell
dotnet build
dotnet run --no-build
```

## Verify in the app

1. Open a folder containing two JPEGs and confirm both rows show thumbnails and disabled **Save** buttons.
2. Double-click a **Title**, **Subject**, or **Comments** cell and change its value. Confirm **Save** enables for that row only. Restore the original values and confirm the button disables again.
3. Edit the row and click **Save**. Confirm the button disables, then reopen the folder and check that the new values persisted.
4. Mark a test JPEG read-only in Explorer and try to save an edit. Confirm an error dialog names the file and the row remains editable with **Save** enabled. Clear the read-only attribute and retry.
5. Edit two JPEGs and choose **File → Save**. Reopen the folder and confirm that both edits persisted and the menu item is disabled.
6. Make an edit and choose **File → Exit** or close the window. Check that **Cancel** keeps the window and edits, **No** exits without saving, and **Yes** saves before exiting. If a file cannot be saved, the app reports the error and stays open.
7. Choose **Help → About** and check that the dialog shows the version and describes the app.
8. Double-click a thumbnail. Resize the modal window in both directions and check that the entire photo stays visible and keeps its proportions. Press Escape to close the preview.

The app skips unsupported or unreadable images and scans only the selected folder.
