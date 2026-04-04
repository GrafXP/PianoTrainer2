using PianoTrainer2.Models;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace PianoTrainer2.Services
{
    public static class SongDownloader
    {
        private static readonly HttpClient _http = new();
        private static readonly string _cacheDir;

        static SongDownloader()
        {
            _cacheDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "PianoTrainer2", "Songs");
            Directory.CreateDirectory(_cacheDir);
        }

        public static string GetCachedPath(BuiltInSong song)
            => Path.Combine(_cacheDir, song.FileName);

        public static bool IsCached(BuiltInSong song)
            => File.Exists(GetCachedPath(song));

        public static async Task<string> EnsureDownloadedAsync(BuiltInSong song, IProgress<int>? progress = null)
        {
            var path = GetCachedPath(song);
            if (File.Exists(path)) return path;

            var response = await _http.GetAsync(song.Url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var total = response.Content.Headers.ContentLength ?? -1;
            await using var stream = await response.Content.ReadAsStreamAsync();
            await using var file = File.Create(path);

            var buffer = new byte[8192];
            long downloaded = 0;
            int read;
            while ((read = await stream.ReadAsync(buffer)) > 0)
            {
                await file.WriteAsync(buffer.AsMemory(0, read));
                downloaded += read;
                if (total > 0)
                    progress?.Report((int)(downloaded * 100 / total));
            }
            progress?.Report(100);
            return path;
        }
    }
}
