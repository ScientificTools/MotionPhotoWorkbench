using System.Drawing;
using System.Globalization;
using System.Net;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace MotionPhotoWorkbench;

internal sealed class ExportPlaybackForm : Form
{
    private readonly string _outputPath;
    private readonly WebView2 _webView = new() { Dock = DockStyle.Fill };
    private readonly Label _status = new()
    {
        Text = "Chargement…", Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleCenter, Padding = new Padding(24)
    };

    public ExportPlaybackForm(string outputPath)
    {
        _outputPath = Path.GetFullPath(outputPath);
        Text = Path.GetFileName(_outputPath);
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(900, 650);
        MinimumSize = new Size(400, 300);
        Controls.Add(_webView);
        Controls.Add(_status);
        _status.BringToFront();
        Shown += async (_, _) => await InitializePlayerAsync();
        // Disposing the child control also stops playback and releases the media file.
        FormClosed += (_, _) => _webView.Dispose();
    }

    private async Task InitializePlayerAsync()
    {
        try
        {
            if (!File.Exists(_outputPath))
                throw new FileNotFoundException("Le fichier exporté est introuvable.", _outputPath);

            long sizeInBytes = new FileInfo(_outputPath).Length;
            const long bytesPerMo = 1024 * 1024;
            var culture = CultureInfo.GetCultureInfo("fr-FR");
            string size = sizeInBytes < bytesPerMo
                ? (sizeInBytes / 1024d).ToString("F0", culture) + " Ko"
                : (sizeInBytes / (double)bytesPerMo).ToString("F1", culture) + " Mo";
            Text = $"{Path.GetFileName(_outputPath)} - {size}";

            string userData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MotionPhotoWorkbench", "WebView2");
            var environment = await CoreWebView2Environment.CreateAsync(userDataFolder: userData);
            if (IsDisposed || Disposing) return;
            await _webView.EnsureCoreWebView2Async(environment);
            if (IsDisposed || Disposing) return;

            var core = _webView.CoreWebView2;
            core.Settings.AreDefaultContextMenusEnabled = false;
            core.Settings.AreDevToolsEnabled = false;
            core.Settings.IsStatusBarEnabled = false;
            core.SetVirtualHostNameToFolderMapping("export.local", Path.GetDirectoryName(_outputPath)!,
                CoreWebView2HostResourceAccessKind.DenyCors);
            core.NewWindowRequested += (_, e) => e.Handled = true;
            // NavigateToString reports a data:text/html URI here, even though the
            // resulting document's location is about:blank.
            core.NavigationStarting += (_, e) => e.Cancel =
                e.Uri != "about:blank" && !e.Uri.StartsWith("data:text/html", StringComparison.OrdinalIgnoreCase);
            core.NavigationCompleted += (_, e) =>
            {
                if (IsDisposed || Disposing) return;
                if (e.IsSuccess)
                    _status.Visible = false;
                else
                    ShowError($"Impossible de charger le lecteur : {e.WebErrorStatus}.\n\nLe fichier exporté reste enregistré sur disque.");
            };
            core.ProcessFailed += (_, _) => ShowError("Le lecteur s’est arrêté. Fermez cette fenêtre et cliquez à nouveau sur Visualiser.");

            string source = WebUtility.HtmlEncode("https://export.local/" + Uri.EscapeDataString(Path.GetFileName(_outputPath)));
            string extension = Path.GetExtension(_outputPath).ToLowerInvariant();
            string media = extension is ".gif" or ".webp"
                ? $"<img id='media' src='{source}' alt='Animation exportée'>"
                : $"<video id='media' src='{source}' autoplay loop muted controls playsinline></video>";
            core.NavigateToString($$"""
                <!doctype html>
                <html lang="fr"><head><meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1">
                <style>
                html,body { margin:0; width:100%; height:100%; background:#181818; color:white; font:16px sans-serif; }
                body { display:flex; align-items:center; justify-content:center; }
                video,img { width:100%; height:100%; object-fit:contain; }
                #error { position:absolute; padding:24px; text-align:center; }
                </style></head><body>
                {{media}}
                <p id="error" hidden>Impossible de lire ce fichier. Le format est peut-être indisponible sur ce poste ou le fichier est inaccessible.</p>
                <script>
                const media = document.getElementById('media');
                function showError() { media.hidden = true; document.getElementById('error').hidden = false; }
                media.addEventListener('error', showError);
                if (media instanceof HTMLImageElement && media.complete && !media.naturalWidth) showError();
                if (media instanceof HTMLVideoElement && media.error) showError();
                </script></body></html>
                """);
        }
        catch (WebView2RuntimeNotFoundException)
        {
            ShowError("Pour visualiser le résultat, installez Microsoft Edge WebView2 Runtime, puis réessayez.\n\nLe fichier exporté reste enregistré sur disque.");
        }
        catch (Exception ex)
        {
            ShowError($"Impossible d’ouvrir le lecteur : {ex.Message}\n\nLe fichier exporté reste enregistré sur disque.");
        }
    }

    private void ShowError(string message)
    {
        if (IsDisposed || Disposing) return;
        _status.Text = message;
        _status.Visible = true;
        _status.BringToFront();
    }
}
