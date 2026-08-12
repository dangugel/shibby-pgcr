using System;
using System.IO;
using System.Text.Json;
using PostGameCarnageData;
using PostGameCarnageData.Models;


namespace ShibbyPgcr
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            if (args.Length > 0)
            {
                List<ScoreboardEntry> scoreboard = BuildScoreboard(
                    args.ToList()
                );

                PrintScoreboard(scoreboard);
                return;
            }



            Console.WriteLine("Shibby Reporter");
            Console.WriteLine("==========================");
            Console.WriteLine();

            Console.WriteLine("Watching for Carnage Reports...");
            Console.WriteLine();

            var reader = new PostGameCarnageReportReader();

            await foreach (var report in reader.WatchAsync())
            {
                PrintReport(report);
                ExportReport(report);
            }
        }

        public static void PrintScoreboard(List<ScoreboardEntry> scoreboard)
        {
            Console.WriteLine();

            Console.WriteLine(
                $"{"Gamertag",-20} " +
                $"{"Kills",7} " +
                $"{"Deaths",8} " +
                $"{"Assists",9} " +
                $"{"+/-",7} " +
                $"{"Score",9} " +
                $"{"Best Streak",12}"
            );

            Console.WriteLine(new string('-', 80));

            foreach (ScoreboardEntry player in scoreboard.OrderByDescending(p => p.Score))
            {
                Console.WriteLine(
                    $"{player.Gamertag,-20} " +
                    $"{player.Kills,7} " +
                    $"{player.Deaths,8} " +
                    $"{player.Assists,9} " +
                    $"{player.PlusMinus,7} " +
                    $"{player.Score,9} " +
                    $"{player.MostKillsInARow,12}"
                );
            }

            Console.WriteLine();
        }




        private static void PrintReport(
            MultiplayerCarnageReport report)
        {
            Console.WriteLine();
            Console.WriteLine("========================================");
            Console.WriteLine("        MATCH DETECTED");
            Console.WriteLine("========================================");

            Console.WriteLine(
                $"Game Type:       {report.GameTypeName?.Value}");

            Console.WriteLine(
                $"Map:             {report.Map ?? "Unsupported"}");

            Console.WriteLine(
                $"Game ID:         {report.GameUniqueId?.Value}");

            Console.WriteLine(
                $"Hopper:          {report.HopperName?.Value}");

            Console.WriteLine(
                $"Hopper ID:       {report.HopperId?.Value}");

            Console.WriteLine(
                $"Matchmaking:     {report.IsMatchmaking?.Value}");

            Console.WriteLine(
                $"Teams Enabled:   {report.IsTeamsEnabled?.Value}");

            Console.WriteLine(
                $"Party Size:      {report.PartySize?.Value}");

            Console.WriteLine(
                $"Match Incomplete:{report.LastMatchIncomplete?.Value}");

            Console.WriteLine();
            Console.WriteLine("Players");
            Console.WriteLine("----------------------------------------");

            if (report.Players is null ||
                report.Players.Count == 0)
            {
                Console.WriteLine("No players found.");
            }
            else
            {
                PrintPlayerTable(report.Players);
            }

            Console.WriteLine("========================================");
            Console.WriteLine();
        }

    
        public static void ExportReport(MultiplayerCarnageReport report)
        {
            string desktopPath = Environment.GetFolderPath(
                Environment.SpecialFolder.Desktop
            );

            string filePath = Path.Combine(
                desktopPath,
                "ShibbyStats.jsonl"
            );

            string json = JsonSerializer.Serialize(report);

            File.AppendAllText(
                filePath,
                json + Environment.NewLine
            );
        }

        public static void PrintPlayerTable(List<Player> players)
        {
            var teams = players
                .GroupBy(player => player.TeamId)
                .OrderBy(group => group.Key);

            foreach (var team in teams)
            {
                Console.WriteLine();
                Console.WriteLine($"Team {team.Key}");
                Console.WriteLine(new string('-', 75));

                Console.WriteLine(
                    $"{"Gamertag",-20} " +
                    $"{"Kills",6} " +
                    $"{"Deaths",7} " +
                    $"{"Assists",8} " +
                    $"{"+/-",6} " +
                    $"{"Score",8} " +
                    $"{"Best Streak",12}"
                );

                Console.WriteLine(new string('-', 75));

                foreach (Player player in team)
                {
                    int plusMinus = player.Kills - player.Deaths;

                    Console.WriteLine(
                        $"{player.Gamertag,-20} " +
                        $"{player.Kills,6} " +
                        $"{player.Deaths,7} " +
                        $"{player.Assists,8} " +
                        $"{plusMinus,6} " +
                        $"{player.Score,8} " +
                        $"{player.MostKillsInARow,12}"
                    );
                }
            }
        }



        public class ScoreboardEntry
        {
            public string XboxUserId { get; set; } = "";
            public string Gamertag { get; set; } = "";
            public int Kills { get; set; }
            public int Deaths { get; set; }
            public int Assists { get; set; }
            public int PlusMinus => Kills - Deaths;
            public int Score { get; set; }
            public int MostKillsInARow { get; set; }
        }

        public static List<ScoreboardEntry> BuildScoreboard(
            List<string> jsonlFilePaths)
        {
            var processedGames = new HashSet<string>();
            var scoreboard = new Dictionary<string, ScoreboardEntry>();

            foreach (string filePath in jsonlFilePaths)
            {
                if (!File.Exists(filePath))
                {
                    continue;
                }

                foreach (string line in File.ReadLines(filePath))
                {
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    MultiplayerCarnageReport? report;

                    try
                    {
                        report = JsonSerializer.Deserialize<MultiplayerCarnageReport>(line);
                    }
                    catch (JsonException)
                    {
                        continue;
                    }

                    if (report == null)
                    {
                        continue;
                    }

                    string gameId = report.GameUniqueId.Value;

                    // Don't process the same game more than once,
                    // even if it exists in multiple JSONL files.
                    if (!processedGames.Add(gameId))
                    {
                        continue;
                    }

                    foreach (Player player in report.Players)
                    {
                        string playerId = player.XboxUserId;

                        if (!scoreboard.TryGetValue(playerId, out ScoreboardEntry? entry))
                        {
                            entry = new ScoreboardEntry
                            {
                                XboxUserId = player.XboxUserId,
                                Gamertag = player.Gamertag,
                                Kills = 0,
                                Deaths = 0,
                                Assists = 0,
                                Score = 0,
                                MostKillsInARow = 0
                            };

                            scoreboard.Add(playerId, entry);
                        }

                        entry.Gamertag = player.Gamertag;
                        entry.Kills += player.Kills;
                        entry.Deaths += player.Deaths;
                        entry.Assists += player.Assists;
                        entry.Score += player.Score;

                        if (player.MostKillsInARow > entry.MostKillsInARow)
                        {
                            entry.MostKillsInARow = player.MostKillsInARow;
                        }
                    }
                }
            }

            return scoreboard.Values.ToList();
        }

   }
}
