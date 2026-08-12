using System.Text.RegularExpressions;

namespace PostGameCarnageData;

public static class Halo3FilmReader
{
    private const int PollIntervalMilliseconds = 250;
    private const int TimeoutSeconds = 50;

    private static readonly Dictionary<string, string> MapNameMappings =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["constru"] = "Construct",
            ["chill"] = "Narrows",
            ["cyberdy"] = "Pit",
            ["midship"] = "Heretic",
            ["warehou"] = "Foundry",
            ["guardia"] = "Guardian"
        };

    /// <summary>
    /// Waits for a new or modified Halo 3 theater film and
    /// determines the map from its filename.
    ///
    /// Existing films are captured as the initial baseline and
    /// are intentionally ignored.
    ///
    /// A film is considered new when:
    ///   - A new .MOV file appears.
    ///   - An existing .MOV file changes size.
    ///   - An existing .MOV file's last-write time changes.
    ///
    /// Waits for up to 50 seconds for a change.
    ///
    /// Throws NotSupportedException if no new or modified film
    /// appears within the timeout period.
    /// </summary>
    public static async Task<string> MapNameFromMostRecentFilm()
    {
        var userProfile =
            Environment.GetFolderPath(
                Environment.SpecialFolder.UserProfile);

        var movieDirectory =
            Path.Combine(
                userProfile,
                "AppData",
                "LocalLow",
                "MCC",
                "Temporary",
                "UserContent",
                "Halo3",
                "Movie");

        Log(
            $"Waiting for a new or changed Halo 3 theater film in: " +
            $"{movieDirectory}");

        Directory.CreateDirectory(movieDirectory);

        // Capture the state of all existing films.
        //
        // This is extremely important:
        // We do NOT process any of these files. They are our baseline.
        var knownFiles =
            CaptureFileState(movieDirectory);

        Log(
            $"Captured baseline containing {knownFiles.Count} " +
            $"existing theater film(s).");

        var timeout =
            TimeSpan.FromSeconds(TimeoutSeconds);

        var startTime =
            DateTime.UtcNow;

        while (DateTime.UtcNow - startTime < timeout)
        {
            var currentFiles =
                CaptureFileState(movieDirectory);

            // Look for a new file or a modification to an
            // existing file.
            foreach (var entry in currentFiles)
            {
                var filePath = entry.Key;
                var currentState = entry.Value;

                // New file.
                if (!knownFiles.TryGetValue(
                        filePath,
                        out var previousState))
                {
                    Log(
                        $"New Halo 3 theater film detected: " +
                        $"{Path.GetFileName(filePath)}");

                    var mapName =
                        ParseMapName(
                            Path.GetFileNameWithoutExtension(filePath));

                    if (mapName is not null)
                    {
                        Log(
                            $"Map identified from new theater film: " +
                            $"{mapName}");

                        return mapName;
                    }

                    Log(
                        $"New theater film does not contain a recognized " +
                        $"map name: {Path.GetFileName(filePath)}");

                    knownFiles[filePath] = currentState;

                    continue;
                }

                // Existing file changed.
                if (HasChanged(
                        previousState,
                        currentState))
                {
                    Log(
                        $"Halo 3 theater film changed: " +
                        $"{Path.GetFileName(filePath)}");

                    Log(
                        $"Previous state: " +
                        $"Size={previousState.Length}, " +
                        $"LastWriteUtc={previousState.LastWriteTimeUtc:O}");

                    Log(
                        $"Current state: " +
                        $"Size={currentState.Length}, " +
                        $"LastWriteUtc={currentState.LastWriteTimeUtc:O}");

                    var mapName =
                        ParseMapName(
                            Path.GetFileNameWithoutExtension(filePath));

                    if (mapName is not null)
                    {
                        Log(
                            $"Map identified from changed theater film: " +
                            $"{mapName}");

                        return mapName;
                    }

                    Log(
                        $"Changed theater film does not contain a " +
                        $"recognized map name: " +
                        $"{Path.GetFileName(filePath)}");

                    knownFiles[filePath] = currentState;
                }
            }

            // Remove deleted files from our baseline.
            //
            // Deletion itself is not considered a trigger.
            var deletedFiles =
                knownFiles.Keys
                    .Where(path => !currentFiles.ContainsKey(path))
                    .ToList();

            foreach (var deletedFile in deletedFiles)
            {
                knownFiles.Remove(deletedFile);
            }

            await Task.Delay(
                PollIntervalMilliseconds);
        }

        Log(
            $"No new or changed Halo 3 theater film appeared within " +
            $"{TimeoutSeconds} seconds.");

        throw new NotSupportedException(
            $"No new or changed Halo 3 theater film appeared within " +
            $"{TimeoutSeconds} seconds.");
    }

    /// <summary>
    /// Captures the current state of all Halo 3 theater films.
    ///
    /// This method intentionally performs no logging because it is
    /// called repeatedly while waiting for a change.
    /// </summary>
    private static Dictionary<string, FilmFileState> CaptureFileState(
        string movieDirectory)
    {
        var result =
            new Dictionary<string, FilmFileState>(
                StringComparer.OrdinalIgnoreCase);

        try
        {
            foreach (var file in Directory.EnumerateFiles(
                         movieDirectory,
                         "*.MOV",
                         SearchOption.TopDirectoryOnly))
            {
                var state =
                    GetFileState(file);

                if (state is not null)
                {
                    result[file] = state;
                }
            }
        }
        catch (IOException ex)
        {
            LogException(
                "Unable to enumerate Halo 3 theater films.",
                ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            LogException(
                "Access denied while enumerating Halo 3 theater films.",
                ex);
        }

        return result;
    }

    private static FilmFileState? GetFileState(
        string filePath)
    {
        try
        {
            var info =
                new FileInfo(filePath);

            if (!info.Exists)
            {
                return null;
            }

            return new FilmFileState(
                info.Length,
                info.LastWriteTimeUtc);
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static bool HasChanged(
        FilmFileState previous,
        FilmFileState current)
    {
        return
            previous.Length != current.Length ||
            previous.LastWriteTimeUtc != current.LastWriteTimeUtc;
    }

    /// <summary>
    /// Extracts a known shortened map identifier from a Halo 3
    /// theater film filename and converts it to the display name.
    /// </summary>
    private static string? ParseMapName(
        string fileName)
    {
        foreach (var mapping in MapNameMappings)
        {
            if (fileName.Contains(
                    mapping.Key,
                    StringComparison.OrdinalIgnoreCase))
            {
                return mapping.Value;
            }
        }

        return null;
    }

    private static void Log(
        string message)
    {
        Console.WriteLine(
            $"[{DateTime.UtcNow:O}] " +
            $"[Halo3FilmReader] {message}");
    }

    private static void LogException(
        string message,
        Exception exception)
    {
        Console.WriteLine(
            $"[{DateTime.UtcNow:O}] " +
            $"[Halo3FilmReader] ERROR: {message}");

        Console.WriteLine(
            $"[{DateTime.UtcNow:O}] " +
            $"[Halo3FilmReader] Exception Type: " +
            $"{exception.GetType().FullName}");

        Console.WriteLine(
            $"[{DateTime.UtcNow:O}] " +
            $"[Halo3FilmReader] Message: " +
            $"{exception.Message}");

        Console.WriteLine(
            $"[{DateTime.UtcNow:O}] " +
            $"[Halo3FilmReader] Full Exception:");

        Console.WriteLine(exception);
    }

    private sealed record FilmFileState(
        long Length,
        DateTime LastWriteTimeUtc);
}
