using System;
using System.IO;
using System.IO.Packaging;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMate.Addin.Commands;
using RevitMate.Addin.Executor;
using RevitMate.Addin.UI;
using RevitMate.Core.Api;
using RevitMate.Resources;

namespace RevitMate.Addin
{
    public class Application : IExternalApplication
    {
        private const string PanelInternalName = "RevitMate.MainPanel";

        [DllImport("user32.dll")]
        private static extern IntPtr GetFocus();

        private static IntPtr _paneHwnd = IntPtr.Zero;
        private static MainPaneProvider _paneProvider;

        internal static void SetPaneHwnd(IntPtr hwnd) => _paneHwnd = hwnd;

        /// <summary>
        /// Live UIApplication reference. Set the first time the user clicks the
        /// Open RevitMate ribbon button (via <see cref="SetRevitApp"/>).
        /// </summary>
        public static UIApplication RevitApp { get; private set; }

        /// <summary>
        /// Called from <see cref="Commands.OpenRevitMateCommand"/> and
        /// <see cref="Executor.RevitExternalEventHandler"/> to capture the
        /// UIApplication and subscribe to SelectionChanged once.
        /// </summary>
        internal static void SetRevitApp(UIApplication uiApp)
        {
            if (RevitApp != null || uiApp == null) return;
            RevitApp = uiApp;
            try
            {
                uiApp.SelectionChanged += (s, e) =>
                {
                    var ids = e.GetSelectedElements();
                    if (ids.Count == 0 && _paneHwnd != IntPtr.Zero && GetFocus() == _paneHwnd)
                        return;
                    _paneProvider?.ViewModel?.UpdateSelectionSnapshot(ids);
                };
            }
            catch { /* SelectionChanged may not be available in all Revit versions */ }
        }

        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                LocalizationManager.LoadFromConfig();
                RevitCommandDispatcher.Initialize();

                string apiKey = LoadApiKey();
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    var settings = new SettingsWindow();
                    settings.ShowDialog();
                    apiKey = LoadApiKey();
                }

                ClaudeApiClient client = string.IsNullOrWhiteSpace(apiKey)
                    ? null
                    : new ClaudeApiClient(apiKey, LoadModel());

                _paneProvider = new MainPaneProvider(client);
                CreateRibbon(application);
                RegisterDockablePane(application, _paneProvider);

                return Result.Succeeded;
            }
            catch (Exception)
            {
                return Result.Failed;
            }
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }

        private static void CreateRibbon(UIControlledApplication application)
        {
            string tabName = Strings.RibbonTabName;
            try
            {
                application.CreateRibbonTab(tabName);
            }
            catch (Autodesk.Revit.Exceptions.ArgumentException)
            {
                // Tab already exists (e.g. when re-loading); reuse it.
            }

            RibbonPanel panel = application.CreateRibbonPanel(tabName, Strings.RibbonPanelName);
            string assemblyPath = Assembly.GetExecutingAssembly().Location;

            var openButtonData = new PushButtonData(
                "OpenRevitMate",
                Strings.OpenButtonText,
                assemblyPath,
                typeof(OpenRevitMateCommand).FullName)
            {
                ToolTip = Strings.PanelTitle,
                LargeImage = LoadEmbeddedImage("Resources/Icons/RevitMate_32.png"),
                Image = LoadEmbeddedImage("Resources/Icons/RevitMate_16.png")
            };

            var settingsButtonData = new PushButtonData(
                "OpenSettings",
                Strings.SettingsButtonText,
                assemblyPath,
                typeof(OpenSettingsCommand).FullName)
            {
                ToolTip = Strings.SettingsTitle,
                LargeImage = LoadEmbeddedImage("Resources/Icons/RevitMateSettings_32.png"),
                Image = LoadEmbeddedImage("Resources/Icons/RevitMateSettings_16.png")
            };

            panel.AddItem(openButtonData);
            panel.AddItem(settingsButtonData);
        }

        private static void RegisterDockablePane(UIControlledApplication application, MainPaneProvider provider)
        {
            if (!DockablePane.PaneIsRegistered(PaneIds.MainPaneDockablePaneId))
            {
                application.RegisterDockablePane(
                    PaneIds.MainPaneDockablePaneId,
                    MainPaneProvider.Title,
                    provider);
            }
        }

        private static string LoadApiKey()
        {
            try
            {
                string path = SettingsWindow.GetConfigPath();
                if (!File.Exists(path)) return null;
                JObject obj = JObject.Parse(File.ReadAllText(path));
                string encB64 = obj["api_key_encrypted"]?.Value<string>();
                if (string.IsNullOrEmpty(encB64)) return null;
                byte[] dec = ProtectedData.Unprotect(
                    Convert.FromBase64String(encB64), null, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(dec);
            }
            catch { return null; }
        }

        private static string LoadModel()
        {
            try
            {
                string path = SettingsWindow.GetConfigPath();
                if (!File.Exists(path)) return "claude-sonnet-4-5";
                JObject obj = JObject.Parse(File.ReadAllText(path));
                return obj["model"]?.Value<string>() ?? "claude-sonnet-4-5";
            }
            catch { return "claude-sonnet-4-5"; }
        }

        private static ImageSource LoadEmbeddedImage(string resourceName)
        {
            // Force the pack:// scheme to register even when WPF's Application
            // singleton has not been constructed (which is the case inside Revit
            // before any WPF window is shown).
            _ = PackUriHelper.UriSchemePack;

            var assembly = typeof(Application).Assembly;
            var uri = new Uri(
                $"pack://application:,,,/{assembly.GetName().Name};component/{resourceName}",
                UriKind.Absolute);
            return new BitmapImage(uri);
        }
    }
}
