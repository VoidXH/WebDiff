using DiffMatchPatch;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Windows;
using System.Windows.Input;

namespace WebDiff {
    public partial class MainWindow : Window {
        static readonly HttpClient client = new();

        readonly List<Website> sites = new();
        readonly List<SavedWebsite> diffs = new();
        readonly Dictionary<Website, DateTime> lasts = new();

        public MainWindow() {
            InitializeComponent();
            Directory.CreateDirectory(sitesFolder);
            Directory.CreateDirectory(diffsFolder);
            if (File.Exists(sitesFile)) {
                IEnumerable<Website> loadedSites = File.ReadAllLines(sitesFile).Select(x => new Website(x));
                sites.AddRange(loadedSites);
            }
            websites.ItemsSource = sites;
            websites.MinColumnWidth = websites.Width - 10;
            UpdateDiffs();
        }

        static string GetFilename(string folder, Website site, DateTime time) =>
            $"{folder}\\{site.HashCode} {time.ToString().Replace(':', '_')}.htm";

        void UpdateDiffs() {
            diffs.Clear();
            string[] files = Directory.GetFiles(sitesFolder);
            Array.Sort(files);
            for (int i = 0; i < files.Length; i++) {
                diffs.Add(new SavedWebsite(sites, files[i]));
            }

            diffs.Sort((a, b) => b.Retrieved.CompareTo(a.Retrieved));
            lasts.Clear();
            for (int i = 0, c = diffs.Count; i < c && lasts.Count != diffs.Count; i++) {
                if (!lasts.ContainsKey(diffs[i].URL)) {
                    lasts[diffs[i].URL] = diffs[i].Retrieved;
                    diffs[i].Latest = true;
                }
            }

            diffList.ItemsSource = null;
            diffList.ItemsSource = diffs;
        }

        async void Update(object sender, RoutedEventArgs e) {
            List<Website> errors = new();
            diff_match_patch comparer = new();

            for (int i = 0, c = sites.Count; i < c; i++) {
                try {
                    HttpResponseMessage response = await client.GetAsync(sites[i].URL);
                    response.EnsureSuccessStatusCode();
                    string body = (await response.Content.ReadAsStringAsync()).RemoveScripts();

                    if (lasts.ContainsKey(sites[i])) {
                        string old = File.ReadAllText(GetFilename(sitesFolder, sites[i], lasts[sites[i]]));
                        List<Diff> foundDiffs = comparer.diff_main(old, body);
                        comparer.diff_cleanupSemantic(foundDiffs);
                        IEnumerable<Diff> output =
                            foundDiffs.Where(x => x.operation == Operation.INSERT && !int.TryParse(x.text, out int _));
                        if (!output.Any()) {
                            continue;
                        }
                        File.WriteAllLines(GetFilename(diffsFolder, sites[i], DateTime.Now), output.Select(x => x.text));
                    }
                    File.WriteAllText(GetFilename(sitesFolder, sites[i], DateTime.Now), body);
                } catch {
                    errors.Add(sites[i]);
                }
            }

            if (errors.Count == 0) {
                MessageBox.Show("Successfully fetched all websites!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            } else {
                string failed = string.Join('\n', errors.Select(x => x.URL));
                MessageBox.Show("Failed to fetch:\n" + failed, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            UpdateDiffs();
        }

        void OpenDiff(object sender, MouseButtonEventArgs e) {
            if (diffList.SelectedItem is not SavedWebsite selection) {
                return;
            }

            string diff = GetFilename(diffsFolder, selection.URL, selection.Retrieved);
            Process.Start(new ProcessStartInfo() {
                FileName = File.Exists(diff) ? diff : GetFilename(sitesFolder, selection.URL, selection.Retrieved),
                UseShellExecute = true
            });
        }

        protected override void OnClosed(EventArgs e) {
            File.WriteAllLines(sitesFile, sites.Select(x => x.URL));
            base.OnClosed(e);
        }

        const string sitesFile = "sites.txt";
        const string sitesFolder = "sites";
        const string diffsFolder = "diffs";
    }
}