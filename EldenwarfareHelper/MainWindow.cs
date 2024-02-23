using System.Collections.Generic;
using DirectN;
using Wice.Effects;
using Wice;
using System;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;
using System.IO;
using System.Diagnostics.Eventing.Reader;
using Newtonsoft.Json.Linq;
using System.IO.Compression;
using Gameloop.Vdf.Linq;
using Gameloop.Vdf;
using Gameloop.Vdf.JsonConverter;
using System.Linq;
using static System.Net.Mime.MediaTypeNames;
using Windows.Gaming.Input;

namespace EldenwarfareHelper
{
    public class MainWindow : Window
    {
        public static _D3DCOLORVALUE ButtonColor;
        public static _D3DCOLORVALUE ButtonShadowColor;
        public static _D3DCOLORVALUE TextColor;

        private const int _headersMargin = 10;
        private Border _pageHolder;
        private readonly List<SymbolHeader> _headers = new List<SymbolHeader>();

        private string _appRoot;
        private string _githubLatestUrl;
        private string _zipPath;

        private bool _downloaded;
        private bool _installed;
        private bool _modDeactivated;

        static MainWindow()
        {
            TextColor = new _D3DCOLORVALUE(0XFFFFFFFF);
        }

        // define Window settings
        public MainWindow()
        {
            _appRoot = System.AppDomain.CurrentDomain.BaseDirectory;
            _zipPath = System.IO.Path.Combine(_appRoot, "downloads", "eldenwarfare.zip");
            _githubLatestUrl = "https://api.github.com/repos/ClayAmore/EldenWarfare/releases/latest";

            _downloaded = File.Exists(_zipPath);
            string gamePath;
            if (tryGetGamePath(out gamePath)) {
                _modDeactivated = File.Exists(System.IO.Path.Combine(gamePath, "dinput8.dll.deactivated")) ||
                    (!File.Exists(System.IO.Path.Combine(gamePath, "dinput8.dll")) &&
                    !File.Exists(System.IO.Path.Combine(gamePath, "dinput8.dll.deactivated")));

                _installed = (File.Exists(System.IO.Path.Combine(gamePath, "dinput8.dll")) ||
                        File.Exists(System.IO.Path.Combine(gamePath, "dinput8.dll.deactivated"))) &&
                    File.Exists(System.IO.Path.Combine(gamePath, "mod_loader_config.ini")) &&
                    File.Exists(System.IO.Path.Combine(gamePath, "mods", "EldenWarfare.dll"));
            }

            // we draw our own titlebar using Wice itself
            WindowsFrameMode = WindowsFrameMode.None;

            // resize to 66% of the screen
            var monitor = Monitor.Primary.Bounds;
            ResizeClient(monitor.Width * 1 / 5, monitor.Height * 1 / 2);

            // the EnableBlurBehind call is necessary when using the Windows' acrylic
            // otherwise the window will be (almost) black
            //Native.EnableBlurBehind();
            RenderBrush = AcrylicBrush.CreateAcrylicBrush(
                CompositionDevice,
                new _D3DCOLORVALUE(0xFF1E1E1E),
                1.0f,
                useWindowsAcrylic: false
                );

            // uncomment this to enable Pointer messages
            //WindowsFunctions.EnableMouseInPointer();
            AddControls();

            AddContent();
        }

        // add basic controls for layout
        private void AddControls()
        {
            // add a Wice titlebar (looks similar to UWP)
            var titleBar = new TitleBar { IsMain = true };
            titleBar.Title.SetSolidColor(TextColor);
            titleBar.Title.SetFontSize(20);
            titleBar.Margin = D2D_RECT_F.Thickness(_headersMargin, _headersMargin, _headersMargin, _headersMargin);
            titleBar.Title.SetFontStretch(DWRITE_FONT_STRETCH.DWRITE_FONT_STRETCH_EXTRA_CONDENSED);
            titleBar.MaxButton.Remove();
            Children.Add(titleBar);

            titleBar.MinButton.Path.Shape.StrokeThickness = 2f;
            titleBar.MinButton.Path.StrokeBrush = Compositor.CreateColorBrush(_D3DCOLORVALUE.White);

            titleBar.CloseButton.Path.Shape.StrokeThickness = 2f;
            titleBar.CloseButton.Path.StrokeBrush = Compositor.CreateColorBrush(_D3DCOLORVALUE.White);
        }

        private void AddContent()
        {
            var stack = new Stack();
            stack.Orientation = Orientation.Vertical;
            stack.HorizontalAlignment = Alignment.Center;
            stack.VerticalAlignment = Alignment.Center;
            stack.Width = Width;
            stack.Height = Height;


            TextBox info1 = new TextBox();
            info1.Width = Width / 2;
            info1.Text = "This will download the mod!";
            info1.SetSolidColor(TextColor);
            info1.SetFontSize(20);
            info1.Height = 50;
            info1.WordWrapping = DWRITE_WORD_WRAPPING.DWRITE_WORD_WRAPPING_WHOLE_WORD;
            info1.VerticalAlignment = Alignment.Center;
            info1.HorizontalAlignment = Alignment.Center;

            var downloadButtonText = new TextBox();
            downloadButtonText.Margin = D2D_RECT_F.Thickness(5, 0, 5, 5);
            downloadButtonText.ForegroundBrush = new SolidColorBrush(_D3DCOLORVALUE.White);
            downloadButtonText.Text = _downloaded ? "Delete" : "Download";
            downloadButtonText.HorizontalAlignment = Alignment.Center;
            downloadButtonText.VerticalAlignment = Alignment.Center;
            downloadButtonText.FontSize = 20;
            Dock.SetDockType(downloadButtonText, DockType.Top);

            Button button = new Button();
            button.Width = 300;
            button.Height = 50;
            button.VerticalAlignment = Alignment.Center;
            button.Margin = D2D_RECT_F.Sized(0,0,0,20);
            button.Child = new Dock();
            button.Child.Margin = D2D_RECT_F.Thickness(10);
            button.Child = downloadButtonText;
            button.DoWhenAttachedToComposition(() => 
            button.RenderBrush = button.Compositor.CreateColorBrush(_D3DCOLORVALUE.LightGray.ChangeAlpha(128)));
            button.Click += (s, e) =>
            {
                // Method not async. Not the best 
                // implementaion. I know.
                downloadMod();
                if (_downloaded) downloadButtonText.Text = "Delete";
                else downloadButtonText.Text = "Download";
            };

            TextBox info2 = new TextBox();
            info2.Width = Width / 2;
            info2.Text = "This will install the mod in your game folder!";
            info2.SetSolidColor(TextColor);
            info2.SetFontSize(20);
            info2.Height = 50;
            info2.WordWrapping = DWRITE_WORD_WRAPPING.DWRITE_WORD_WRAPPING_WHOLE_WORD;
            info2.VerticalAlignment = Alignment.Center;
            info2.HorizontalAlignment = Alignment.Center;

            var installButtonText = new TextBox();
            installButtonText.Margin = D2D_RECT_F.Thickness(5, 0, 5, 5);
            installButtonText.ForegroundBrush = new SolidColorBrush(_D3DCOLORVALUE.White);
            installButtonText.Text = _installed ? "Uninstall" : "Install";
            installButtonText.HorizontalAlignment = Alignment.Center;
            installButtonText.VerticalAlignment = Alignment.Center;
            installButtonText.FontSize = 20;
            Dock.SetDockType(installButtonText, DockType.Top);

            Button button2 = new Button();
            button2.Width = 300;
            button2.Height = 50;
            button2.VerticalAlignment = Alignment.Center;
            button2.Margin = D2D_RECT_F.Sized(0, 0, 0, 20);
            button2.Child = new Dock();
            button2.Child.Margin = D2D_RECT_F.Thickness(10);
            button2.Child = installButtonText;
            button2.DoWhenAttachedToComposition(() =>
            button2.RenderBrush = button2.Compositor.CreateColorBrush(_D3DCOLORVALUE.LightGray.ChangeAlpha(128)));
            button2.Click += (s, e) =>
            {
                copyToGameDirectory();
                if (_installed) installButtonText.Text = "Uninstall";
                else installButtonText.Text = "Install";
            };


            TextBox info3 = new TextBox();
            info3.Text = "This will activate or deactivate the mod!";
            info3.SetSolidColor(TextColor);
            info3.SetFontSize(20);
            info3.Height = 50;
            info3.WordWrapping = DWRITE_WORD_WRAPPING.DWRITE_WORD_WRAPPING_WHOLE_WORD;
            info3.VerticalAlignment = Alignment.Center;
            info3.HorizontalAlignment = Alignment.Center;

            TextBox activeText = new TextBox();

            var toggle = new ToggleSwitch();
            toggle.Width = 100;
            toggle.Height = 50;
            toggle.Margin = D2D_RECT_F.Sized(0, 0, 0, 10);
            toggle.Value = !_modDeactivated;
            toggle.Click += (s, e) =>
            {
                toggle.Value = toggleMod();
                activeText.Text = "Mod is: " + (toggle.Value ? "ON" : "OFF");
            };

            activeText.Text = "Mod is: " + (toggle.Value ? "ON" : "OFF");
            activeText.SetSolidColor(TextColor);
            activeText.SetFontSize(30);
            activeText.VerticalAlignment = Alignment.Center;
            activeText.HorizontalAlignment = Alignment.Center;

            stack.Children.Add(info1);
            stack.Children.Add(button);
            stack.Children.Add(info2);
            stack.Children.Add(button2);
            stack.Children.Add(info3);
            stack.Children.Add(toggle);
            stack.Children.Add(activeText);

            Children.Add(stack);
        }

        private void downloadMod()
        {
            if (_downloaded)
            {
                var filePath = System.IO.Path.Combine(_appRoot, "downloads", "eldenwarfare.zip");
                File.Delete(filePath);
                _downloaded = false;
            }
            else
            {
                var data = Get(_githubLatestUrl).GetAwaiter().GetResult();
                if (string.IsNullOrEmpty(data)) return;

                Response response = JsonConvert.DeserializeObject<Response>(data);
                
                if (response.assets.Count < 1) return;
                var downloadUrl = response.assets[0].browser_download_url;

                if (string.IsNullOrEmpty(downloadUrl)) return;

                var urlSplit = downloadUrl.Split("/");
                System.IO.Directory.CreateDirectory(System.IO.Path.Combine(_appRoot, "downloads"));
                var success = DownoadZip(downloadUrl, System.IO.Path.Combine(_appRoot, "downloads", "eldenwarfare.zip")).GetAwaiter().GetResult();
                if (success) _downloaded = true;
            }
        }

        private async Task<string> Get(string uri)
        {
            using var client = new HttpClient();

            client.BaseAddress = new Uri("https://api.github.com");
            client.DefaultRequestHeaders.Add("User-Agent", "C# console program");
            client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));

            HttpResponseMessage response = await client.GetAsync(uri);
            response.EnsureSuccessStatusCode();
            var resp = await response.Content.ReadAsStringAsync();
            return resp;
        }

        private async Task<bool> DownoadZip(string url, string filename)
        {
            using (var httpClient = new HttpClient())
            {
                await httpClient.DownloadFile(url, filename);
                return true;
            }
        }

        private bool tryGetGamePath(out string gamePath)
        {
            gamePath = string.Empty;
            string steamPath = Microsoft.Win32.Registry.GetValue("HKEY_CURRENT_USER\\Software\\Valve\\Steam", "SteamPath", "").ToString();
               
            if (string.IsNullOrEmpty(steamPath)) return false;
            
            var vdfPath = System.IO.Path.Combine(steamPath, "SteamApps", "libraryfolders.vdf");
            VProperty applist = VdfConvert.Deserialize(File.ReadAllText(vdfPath));
            var json = applist.ToJson();
            JObject obj = JObject.Parse(json.Value.ToString());
            IList<string> paths = obj.Values().Where(v => v.Type == JTokenType.Object).Select(v => JObject.Parse(v.ToString())).Where(v => v.ContainsKey("path")).Select(v => v["path"].ToString()).ToList();

            var fileName = $"appmanifest_1245620.acf";
            foreach (var p in paths)
            {
                var manifestPath = System.IO.Path.Combine(p, "steamapps", fileName);
                if (!File.Exists(manifestPath)) continue;

                var manifestFileJson = VdfConvert.Deserialize(File.ReadAllText(manifestPath)).ToJson();
                if (manifestFileJson == null) continue;

                var installDir = manifestFileJson.First()["installdir"].ToString();
                
                if (!Directory.Exists(System.IO.Path.Combine(p, "steamapps", "common", installDir, "Game"))) return false;

                gamePath = System.IO.Path.Combine(p, "steamapps", "common", installDir, "Game");
                return true;
            }

            return false;
        }

        private void copyToGameDirectory()
        {

            string gamePath;
            if (tryGetGamePath(out gamePath))
            {
                if (_installed)
                {
                    var activatedPath = System.IO.Path.Combine(gamePath, "dinput8.dll");
                    var deactivatedPath = System.IO.Path.Combine(gamePath, "dinput8.dll.deactivated");

                    if (File.Exists(activatedPath))     File.Delete(activatedPath);
                    if (File.Exists(deactivatedPath))   File.Delete(deactivatedPath);

                    File.Delete(System.IO.Path.Combine(gamePath, "dinput8.dll.deactivated"));
                    File.Delete(System.IO.Path.Combine(gamePath, "mod_loader_config.ini")) ;
                    File.Delete(System.IO.Path.Combine(gamePath, "mods", "EldenWarfare.dll"));
                    File.Delete(System.IO.Path.Combine(gamePath, "mods", "EldenWarfare.ini"));
                    Directory.Delete(System.IO.Path.Combine(gamePath, "mods", "EldenWarfare"), true);
                    _installed = false;
                }
                else
                {
                    if (!File.Exists(_zipPath)) return;
                    ZipFile.ExtractToDirectory(_zipPath, gamePath, true);
                    _installed = true;
                }
            }
        }
        private bool toggleMod()
        {
            string gamePath;
            if (tryGetGamePath(out gamePath))
            {
                var activatedPath = System.IO.Path.Combine(gamePath, "dinput8.dll");
                var deactivatedPath = System.IO.Path.Combine(gamePath, "dinput8.dll.deactivated");

                if (!File.Exists(activatedPath) && !File.Exists(deactivatedPath))
                {
                    _modDeactivated = true;
                    return false;
                }

                if (_modDeactivated)
                {
                    if (File.Exists(activatedPath)) { _modDeactivated = false; return true; }
                    File.Move(deactivatedPath, activatedPath);
                    _modDeactivated = true;
                }
                else
                {
                    if (File.Exists(deactivatedPath)) { _modDeactivated = true; return false; }
                    File.Move(activatedPath, deactivatedPath);
                    _modDeactivated = false;
                    return false;
                }
            }
            return true;
        }
    }

}
