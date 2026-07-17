using System;
using System.IO;

namespace B44.Common.Persistence;

/// <summary>Save-location helpers for file-backed stores.</summary>
public static class SavePaths
{
    /// <summary>
    /// Resolves a save path under the per-user application-data directory
    /// (e.g. <c>%APPDATA%/MyGame/save.json</c>), creating the directory.
    /// </summary>
    public static string ResolveAppData(string appFolderName, string fileName)
    {
        string dataDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        if (string.IsNullOrWhiteSpace(dataDir))
        {
            throw new StoreException(
                "Unable to resolve the application data directory for the save file.",
                new InvalidOperationException("Application data path was null or empty."));
        }

        string saveDirectory = Path.Combine(dataDir, appFolderName);
        Directory.CreateDirectory(saveDirectory);
        return Path.Combine(saveDirectory, fileName);
    }
}
