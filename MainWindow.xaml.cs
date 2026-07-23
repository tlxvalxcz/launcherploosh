using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using Wpf.Ui.Controls;

namespace PlooshLauncher
{
    // One POCO = one backend payload. Deserialize your JSON straight into this
    // and hand it to ApplyStats(). Numbers default to 0, time defaults to "0h, 0m, 0s".
    public sealed class StatsDto
    {
        public int VBucks { get; set; }
        public int SeasonLevel { get; set; }
        public int ArenaHype { get; set; }
        public int Eliminations { get; set; }
        public int Victories { get; set; }
        public int Matches { get; set; }
        public int PlayersOnline { get; set; }
        public string TimeAlive { get; set; } = "0h, 0m, 0s";
    }

    public partial class MainWindow : FluentWindow
    {
        // ============================================================
        //  PUT YOUR LINKS HERE
        // ============================================================
        private const string DownloadUrl = "https://example.com/your-download-link";
        private const string ClaimUrl = "https://example.com/your-claim-link";
        // ============================================================

        // Filled by LoginWindow after discord auth. Send AuthToken to your
        // stats backend so it knows whose numbers to push back.
        public string? AuthToken { get; private set; }
        public string? Username { get; private set; }

        private readonly ObservableCollection<string> _friends = new ObservableCollection<string>();
        private static readonly string FriendsFile = Path.Combine(AppContext.BaseDirectory, "friends.txt");

        public MainWindow()
        {
            InitializeComponent();
            LoadFriends();
            friendsList.ItemsSource = _friends;
        }

        // ============================================================
        //  AUTH HOOK — called once by LoginWindow right after discord
        // ============================================================
        public void SetUser(string username, string? token = null)
        {
            Username = username;
            AuthToken = token;
            Dispatcher.BeginInvoke(new Action(() => { welcomeNameText.Text = username; }));

            // >>> YOUR HOOK <<<  kick off your stats poll / websocket here.
            // e.g.  StatsClient.Start(AuthToken, dto => ApplyStats(dto));
        }

        // ============================================================
        //  STATS UPDATE API — the "as easy as possible" surface.
        //  Call from ANY thread (backend callback, timer, websocket).
        //  Marshals to the UI thread for you.
        // ============================================================

        // Option A: hand it the deserialized JSON object.
        public void ApplyStats(StatsDto s)
        {
            if (s == null) return;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                vBucksText.Text = Fmt(s.VBucks);
                seasonLevelText.Text = Fmt(s.SeasonLevel);
                arenaHypeText.Text = Fmt(s.ArenaHype);
                killsText.Text = Fmt(s.Eliminations);
                winsText.Text = Fmt(s.Victories);
                matchesText.Text = Fmt(s.Matches);
                timeText.Text = string.IsNullOrWhiteSpace(s.TimeAlive) ? "0h, 0m, 0s" : s.TimeAlive;
                playersOnlineText.Text = Fmt(s.PlayersOnline);
            }));
        }

        // Option B: pass the numbers positionally, no POCO needed.
        public void SetStats(int vbucks, int seasonLevel, int arenaHype,
                             int eliminations, int victories, int matches,
                             string timeAlive)
        {
            ApplyStats(new StatsDto
            {
                VBucks = vbucks,
                SeasonLevel = seasonLevel,
                ArenaHype = arenaHype,
                Eliminations = eliminations,
                Victories = victories,
                Matches = matches,
                TimeAlive = timeAlive
            });
        }

        // Single-field pokes, if your backend pushes them one at a time.
        public void SetPlayersOnline(int n) => Dispatcher.BeginInvoke(new Action(() => playersOnlineText.Text = Fmt(n)));
        public void SetTimeAlive(string s) => Dispatcher.BeginInvoke(new Action(() => timeText.Text = string.IsNullOrWhiteSpace(s) ? "0h, 0m, 0s" : s));

        // Helper if your backend sends raw seconds instead of a formatted string.
        public static string FormatTimeAlive(TimeSpan t) => $"{(int)t.TotalHours}h, {t.Minutes}m, {t.Seconds}s";
        public static string FormatTimeAlive(int totalSeconds) => FormatTimeAlive(TimeSpan.FromSeconds(totalSeconds));

        private static string Fmt(int n) => n.ToString("N0"); // 0 -> "0", 1500 -> "1,500"

        // ============================================================
        //  BUTTONS
        // ============================================================
        private void downloadClick(object sender, RoutedEventArgs e) => OpenUrl(DownloadUrl);
        private void claimClick(object sender, RoutedEventArgs e) => OpenUrl(ClaimUrl);

        private static void OpenUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return;
            try { Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true }); }
            catch { }
        }

        // ============================================================
        //  FRIENDS — edit friends.txt next to the .exe, or call at runtime
        // ============================================================
        private void LoadFriends()
        {
            try
            {
                if (!File.Exists(FriendsFile))
                    File.WriteAllLines(FriendsFile, new[] { "Player1", "Player2", "Player3" });

                var names = File.ReadAllLines(FriendsFile).Select(l => l.Trim()).Where(l => l.Length > 0).ToList();
                _friends.Clear();
                foreach (var n in names) _friends.Add(n);
            }
            catch
            {
                _friends.Clear();
                _friends.Add("Player1");
            }
        }

        public void RefreshFriends() => LoadFriends();

        public void AddFriend(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            name = name.Trim();
            if (_friends.Contains(name)) return;
            _friends.Add(name);
            try { File.AppendAllText(FriendsFile, name + Environment.NewLine); } catch { }
        }
    }
}