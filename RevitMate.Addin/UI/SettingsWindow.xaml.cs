using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json.Linq;

namespace RevitMate.Addin.UI
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            ModelBox.Text = "claude-sonnet-4-5";
            MaxTokensBox.Text = "4096";
            LanguageBox.SelectedIndex = 0;
            LoadCurrentSettings();
        }

        private void LoadCurrentSettings()
        {
            try
            {
                string path = GetConfigPath();
                if (!File.Exists(path)) return;

                JObject obj = JObject.Parse(File.ReadAllText(path));

                string encB64 = obj["api_key_encrypted"]?.Value<string>();
                if (!string.IsNullOrEmpty(encB64))
                {
                    byte[] enc = Convert.FromBase64String(encB64);
                    byte[] dec = ProtectedData.Unprotect(enc, null, DataProtectionScope.CurrentUser);
                    ApiKeyBox.Password = Encoding.UTF8.GetString(dec);
                }

                string model = obj["model"]?.Value<string>();
                if (!string.IsNullOrWhiteSpace(model))
                    ModelBox.Text = model;

                int maxTokens = obj["max_tokens"]?.Value<int>() ?? 0;
                if (maxTokens > 0)
                    MaxTokensBox.Text = maxTokens.ToString();

                string lang = obj["language"]?.Value<string>() ?? "auto";
                foreach (ComboBoxItem item in LanguageBox.Items)
                {
                    if (string.Equals((string)item.Tag, lang, StringComparison.Ordinal))
                    {
                        LanguageBox.SelectedItem = item;
                        break;
                    }
                }
            }
            catch { }
        }

        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
            try
            {
                string apiKey = ApiKeyBox.Password;
                string model = ModelBox.Text?.Trim();
                if (!int.TryParse(MaxTokensBox.Text?.Trim(), out int maxTokens) || maxTokens <= 0)
                    maxTokens = 4096;
                string language = "auto";
                if (LanguageBox.SelectedItem is ComboBoxItem langItem)
                    language = (string)langItem.Tag ?? "auto";

                var obj = new JObject();
                if (!string.IsNullOrWhiteSpace(apiKey))
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(apiKey);
                    byte[] enc = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
                    obj["api_key_encrypted"] = Convert.ToBase64String(enc);
                }
                obj["model"] = string.IsNullOrWhiteSpace(model) ? "claude-sonnet-4-5" : model;
                obj["max_tokens"] = maxTokens;
                obj["language"] = language;

                string path = GetConfigPath();
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, obj.ToString(Newtonsoft.Json.Formatting.Indented));

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        internal static string GetConfigPath()
            => Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "RevitMate", "config.json");
    }
}
