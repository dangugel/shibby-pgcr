using System.Runtime.CompilerServices;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using PostGameCarnageData.Models;

namespace PostGameCarnageData;

public sealed class PostGameCarnageReportReader
{
    private const string ReportFilePrefix = "mpcarnagereport";
    private const string ReportFileExtension = ".xml";

    private readonly string _directoryPath;

    public PostGameCarnageReportReader()
    {
        var userProfile =
            Environment.GetFolderPath(
                Environment.SpecialFolder.UserProfile);

        _directoryPath = Path.Combine(
            userProfile,
            "AppData",
            "LocalLow",
            "MCC",
            "Temporary");

        Log(
            $"Initialized. Directory: {_directoryPath}");
    }

    /// <summary>
    /// Continuously monitors the MCC carnage report directory.
    ///
    /// Existing files are snapshotted when the reader starts and are
    /// not processed immediately.
    ///
    /// A report is processed when:
    ///   - A new report file appears.
    ///   - An existing report's size changes.
    ///   - An existing report's last-write time changes.
    ///
    /// The directory is polled silently while no changes occur.
    /// </summary>
    public async IAsyncEnumerable<MultiplayerCarnageReport> WatchAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Log("Starting WatchAsync.");

        Directory.CreateDirectory(_directoryPath);

        Log(
            $"Monitoring directory: {_directoryPath}");

        // Capture the state of every carnage report that already exists.
        //
        // IMPORTANT:
        // We intentionally DO NOT process these files.
        // They become our baseline and we wait for them to change.
        var knownFiles = CaptureFileState();

        Log(
            $"Captured initial state for {knownFiles.Count} " +
            $"existing carnage report(s).");

        Log("Waiting for new or updated carnage reports...");

        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(
                TimeSpan.FromMilliseconds(250),
                cancellationToken);

            var currentFiles = CaptureFileState();

            foreach (var entry in currentFiles)
            {
                var filePath = entry.Key;
                var currentState = entry.Value;

                if (!knownFiles.TryGetValue(
                        filePath,
                        out var previousState))
                {
                    // New file.
                    Log(
                        $"New carnage report detected: " +
                        $"{Path.GetFileName(filePath)}");

                    LogFileStateChange(
                        previous: null,
                        current: currentState);

                    var report = await ProcessReportAsync(
                        filePath,
                        cancellationToken);

                    // Always update our baseline after processing so
                    // that we don't repeatedly process the same state.
                    knownFiles[filePath] = currentState;

                    if (report is not null)
                    {
                        yield return report;
                    }

                    continue;
                }

                if (HasChanged(previousState, currentState))
                {
                    // Existing file was modified.
                    Log(
                        $"Carnage report updated: " +
                        $"{Path.GetFileName(filePath)}");

                    LogFileStateChange(
                        previousState,
                        currentState);

                    var report = await ProcessReportAsync(
                        filePath,
                        cancellationToken);

                    // MCC may modify the file while we are reading it,
                    // so capture the state AFTER the read and use that
                    // as our new baseline.
                    knownFiles[filePath] =
                        GetFileState(filePath) ?? currentState;

                    if (report is not null)
                    {
                        yield return report;
                    }
                }
            }

            // Remove files that no longer exist.
            //
            // Deletion is not itself a report-processing event.
            var deletedFiles =
                knownFiles.Keys
                    .Where(path => !currentFiles.ContainsKey(path))
                    .ToList();

            foreach (var deletedFile in deletedFiles)
            {
                knownFiles.Remove(deletedFile);
            }
        }

        Log("WatchAsync stopped.");
    }

    /// <summary>
    /// Reads and processes a carnage report.
    ///
    /// After the XML has been successfully deserialized:
    ///
    /// 1. Matchmaking reports are discarded.
    /// 2. The most recent Halo 3 theater film is inspected to
    ///    determine the map name.
    /// 3. If no new theater film appears within the allowed timeout,
    ///    the report is discarded.
    /// </summary>
    private static async Task<MultiplayerCarnageReport?> ProcessReportAsync(
        string filePath,
        CancellationToken cancellationToken)
    {
        LogFileInfo(filePath);

        var report =
            await ReadReportWithRetryAsync(
                filePath,
                cancellationToken);

        if (report is null)
        {
            Log(
                $"Unable to read changed carnage report: " +
                $"{Path.GetFileName(filePath)}");

            return null;
        }

        // ------------------------------------------------------------
        // Discard matchmaking games.
        // ------------------------------------------------------------

        if (report.IsMatchmaking?.Value == true)
        {
            Log(
                $"Discarding matchmaking carnage report: " +
                $"{Path.GetFileName(filePath)}");

            return null;
        }

        if (report.LastMatchIncomplete?.Value == true)
        {
            Log(
                $"Discarding carnage report with incomplete match: " +
                $"{Path.GetFileName(filePath)}");

            return null;
        }

        Log(
            $"Successfully processed carnage report: " +
            $"{Path.GetFileName(filePath)}");

        // ------------------------------------------------------------
        // Determine the map from the new Halo 3 theater film.
        // ------------------------------------------------------------

        try
        {
            var mapName =
                await Halo3FilmReader.MapNameFromMostRecentFilm();

            report.Map = mapName;

            Log(
                $"Map determined from most recent theater film: " +
                $"{mapName ?? "<unknown>"}");
        }
        catch (NotSupportedException ex)
        {
            // The film reader uses NotSupportedException to indicate
            // that no new/usable theater film appeared within its
            // timeout window.
            //
            // This report is therefore discarded rather than being
            // returned with an incorrect map from an older film.
            LogException(
                "Discarding carnage report because no new supported " +
                "Halo 3 theater film became available.",
                ex);

            return null;
        }
        catch (OperationCanceledException) when (
            cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Unexpected errors should not cause the entire reader
            // to stop. Preserve the carnage report, but leave its
            // map unset.
            LogException(
                "Unable to determine map from the most recent theater film.",
                ex);

            report.Map = null;
        }

        return report;
    }

    /// <summary>
    /// Captures the current state of all carnage report files.
    ///
    /// No logging occurs during normal operation because this method
    /// runs every 250ms.
    /// </summary>
    private Dictionary<string, ReportFileState> CaptureFileState()
    {
        var result =
            new Dictionary<string, ReportFileState>(
                StringComparer.OrdinalIgnoreCase);

        if (!Directory.Exists(_directoryPath))
        {
            return result;
        }

        try
        {
            foreach (var file in Directory.EnumerateFiles(
                         _directoryPath,
                         $"{ReportFilePrefix}*{ReportFileExtension}",
                         SearchOption.TopDirectoryOnly))
            {
                if (!IsReportFile(file))
                {
                    continue;
                }

                var state = GetFileState(file);

                if (state is not null)
                {
                    result[file] = state;
                }
            }
        }
        catch (IOException ex)
        {
            LogException(
                "Unable to enumerate carnage report directory.",
                ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            LogException(
                "Access denied while enumerating carnage report directory.",
                ex);
        }

        return result;
    }

    private static ReportFileState? GetFileState(string filePath)
    {
        try
        {
            var info = new FileInfo(filePath);

            if (!info.Exists)
            {
                return null;
            }

            return new ReportFileState(
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
        ReportFileState previous,
        ReportFileState current)
    {
        return
            previous.Length != current.Length ||
            previous.LastWriteTimeUtc != current.LastWriteTimeUtc;
    }

    private static void LogFileStateChange(
        ReportFileState? previous,
        ReportFileState current)
    {
        if (previous is null)
        {
            Log(
                $"Initial state: " +
                $"Size={current.Length} bytes, " +
                $"LastWriteUtc={current.LastWriteTimeUtc:O}");

            return;
        }

        Log(
            $"Previous state: " +
            $"Size={previous.Length} bytes, " +
            $"LastWriteUtc={previous.LastWriteTimeUtc:O}");

        Log(
            $"Current state: " +
            $"Size={current.Length} bytes, " +
            $"LastWriteUtc={current.LastWriteTimeUtc:O}");
    }

    private static bool IsReportFile(string path)
    {
        var fileName = Path.GetFileName(path);

        return
            fileName.StartsWith(
                ReportFilePrefix,
                StringComparison.OrdinalIgnoreCase) &&
            fileName.EndsWith(
                ReportFileExtension,
                StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<MultiplayerCarnageReport?> ReadReportWithRetryAsync(
        string filePath,
        CancellationToken cancellationToken)
    {
        const int maxAttempts = 10;
        const int retryDelayMilliseconds = 100;

        Log(
            $"Beginning XML read: {filePath}");

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Log(
                $"XML read attempt {attempt}/{maxAttempts}: " +
                $"{Path.GetFileName(filePath)}");

            try
            {
                LogFileInfo(filePath);

                await using var stream =
                    new FileStream(
                        filePath,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.ReadWrite);

                Log(
                    $"File opened successfully. " +
                    $"Length={stream.Length} bytes.");

                var serializer =
                    new XmlSerializer(
                        typeof(MultiplayerCarnageReport));

                Log(
                    $"XmlSerializer created for type: " +
                    $"{typeof(MultiplayerCarnageReport).FullName}");

                var report =
                    serializer.Deserialize(stream)
                    as MultiplayerCarnageReport;

                if (report is not null)
                {
                    Log(
                        $"XML deserialization SUCCESS: " +
                        $"{Path.GetFileName(filePath)}");

                    Log(
                        $"Result type: {report.GetType().FullName}");

                    return report;
                }

                Log(
                    "XML deserialization returned null.");

                Log(
                    $"Stream position after deserialize: " +
                    $"{stream.Position}/{stream.Length}");
            }
            catch (InvalidOperationException ex)
            {
                LogException(
                    $"XML DESERIALIZATION FAILED on attempt " +
                    $"{attempt}/{maxAttempts}.",
                    ex);

                LogXmlFailureDetails(filePath, ex);
            }
            catch (XmlException ex)
            {
                LogException(
                    $"XML PARSING FAILED on attempt " +
                    $"{attempt}/{maxAttempts}.",
                    ex);

                LogXmlFailureDetails(filePath, ex);
            }
            catch (IOException ex)
            {
                LogException(
                    $"IO FAILED while reading report on attempt " +
                    $"{attempt}/{maxAttempts}.",
                    ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                LogException(
                    $"ACCESS DENIED while reading report on attempt " +
                    $"{attempt}/{maxAttempts}.",
                    ex);
            }
            catch (Exception ex)
            {
                LogException(
                    $"UNEXPECTED ERROR while reading report on attempt " +
                    $"{attempt}/{maxAttempts}.",
                    ex);
            }

            if (attempt < maxAttempts)
            {
                Log(
                    $"Waiting {retryDelayMilliseconds}ms before retry...");

                await Task.Delay(
                    retryDelayMilliseconds,
                    cancellationToken);
            }
        }

        Log(
            $"Unable to read report after {maxAttempts} attempts: " +
            $"{filePath}");

        return null;
    }

    private static void LogXmlFailureDetails(
        string filePath,
        Exception exception)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                Log(
                    $"XML failure diagnostics: file no longer exists: " +
                    $"{filePath}");

                return;
            }

            var bytes =
                File.ReadAllBytes(filePath);

            Log(
                $"XML failure diagnostics: file contains " +
                $"{bytes.Length} bytes.");

            var text =
                Encoding.UTF8.GetString(bytes);

            const int previewLength = 4096;

            if (text.Length > previewLength)
            {
                text = text[..previewLength] +
                       "\n...[XML PREVIEW TRUNCATED]...";
            }

            Log(
                "XML content preview:\n" +
                text);

            var xmlException =
                FindXmlException(exception);

            if (xmlException is not null)
            {
                Log(
                    $"XmlException details: " +
                    $"LineNumber={xmlException.LineNumber}, " +
                    $"LinePosition={xmlException.LinePosition}");

                Log(
                    $"XmlException SourceUri: " +
                    $"{xmlException.SourceUri ?? "<none>"}");
            }
        }
        catch (Exception ex)
        {
            LogException(
                "Unable to collect XML failure diagnostics.",
                ex);
        }
    }

    private static XmlException? FindXmlException(Exception exception)
    {
        Exception? current = exception;

        while (current is not null)
        {
            if (current is XmlException xmlException)
            {
                return xmlException;
            }

            current = current.InnerException;
        }

        return null;
    }

    private static void LogFileInfo(string filePath)
    {
        try
        {
            var info = new FileInfo(filePath);

            Log(
                $"FileInfo: " +
                $"Name='{info.Name}', " +
                $"FullPath='{info.FullName}', " +
                $"Exists={info.Exists}, " +
                $"Length={info.Length}, " +
                $"LastWriteUtc={info.LastWriteTimeUtc:O}, " +
                $"CreationUtc={info.CreationTimeUtc:O}");
        }
        catch (Exception ex)
        {
            LogException(
                $"Unable to retrieve FileInfo for '{filePath}'.",
                ex);
        }
    }

    private static void Log(string message)
    {
        Console.WriteLine(
            $"[{DateTime.UtcNow:O}] " +
            $"[CarnageReportReader] {message}");
    }

    private static void LogException(
        string message,
        Exception exception)
    {
        Console.WriteLine(
            $"[{DateTime.UtcNow:O}] " +
            $"[CarnageReportReader] ERROR: {message}");

        Console.WriteLine(
            $"[{DateTime.UtcNow:O}] " +
            $"[CarnageReportReader] Exception Type: " +
            $"{exception.GetType().FullName}");

        Console.WriteLine(
            $"[{DateTime.UtcNow:O}] " +
            $"[CarnageReportReader] Message: " +
            $"{exception.Message}");

        if (exception is XmlException xmlException)
        {
            Console.WriteLine(
                $"[{DateTime.UtcNow:O}] " +
                $"[CarnageReportReader] XML Line: " +
                $"{xmlException.LineNumber}");

            Console.WriteLine(
                $"[{DateTime.UtcNow:O}] " +
                $"[CarnageReportReader] XML Position: " +
                $"{xmlException.LinePosition}");
        }

        Console.WriteLine(
            $"[{DateTime.UtcNow:O}] " +
            $"[CarnageReportReader] Full Exception:");

        Console.WriteLine(
            exception);
    }

    private sealed record ReportFileState(
        long Length,
        DateTime LastWriteTimeUtc);
}
