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


   }
}
