using System;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Threading;

namespace RevitMate.Resources
{
    /// <summary>
    /// Centralized helper for switching the active UI language and reading
    /// localized strings from the <see cref="Strings"/> resource set.
    /// </summary>
    /// <remarks>
    /// On first access the manager attempts to load a persisted preference
    /// from <c>%AppData%\RevitMate\config.json</c>. If the file is absent or
    /// the value is unrecognized, English ("en") is used as the default.
    /// </remarks>
    public static class LocalizationManager
    {
        private const string EnglishCode = "en";
        private const string JapaneseCode = "ja";
        private const string DefaultLanguage = EnglishCode;

        private const string ConfigDirectoryName = "RevitMate";
        private const string ConfigFileName = "config.json";
        private const string LanguagePropertyName = "language";

        private static readonly object SyncRoot = new object();
        private static CultureInfo _currentCulture;

        static LocalizationManager()
        {
            string preferred = LoadPersistedLanguage() ?? DefaultLanguage;
            ApplyLanguageInternal(preferred);
        }

        /// <summary>
        /// Gets the culture currently used for resource lookups. Defaults to
        /// the persisted preference, or English if none has been saved.
        /// </summary>
        public static CultureInfo CurrentCulture
        {
            get
            {
                lock (SyncRoot)
                {
                    return _currentCulture;
                }
            }
        }

        /// <summary>
        /// Switches the active UI language for the current thread.
        /// Accepts the language codes "en" (English) and "ja" (Japanese);
        /// any other value falls back to English.
        /// </summary>
        public static void SetLanguage(string lang)
        {
            ApplyLanguageInternal(lang);
        }

        /// <summary>
        /// Re-reads the persisted language preference from
        /// <c>%AppData%\RevitMate\config.json</c> and applies it.
        /// Falls back to English when the file is missing or invalid.
        /// </summary>
        public static void LoadFromConfig()
        {
            string preferred = LoadPersistedLanguage() ?? DefaultLanguage;
            ApplyLanguageInternal(preferred);
        }

        /// <summary>
        /// Returns the localized string for <paramref name="key"/> using the
        /// current culture. Returns an empty string if the key is not found.
        /// </summary>
        public static string Get(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return string.Empty;
            }

            string value = Strings.ResourceManager.GetString(key, CurrentCulture);
            return value ?? string.Empty;
        }

        private static void ApplyLanguageInternal(string lang)
        {
            CultureInfo culture = ResolveCulture(lang);

            lock (SyncRoot)
            {
                _currentCulture = culture;
            }

            Strings.Culture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
        }

        private static CultureInfo ResolveCulture(string lang)
        {
            string normalized = string.IsNullOrWhiteSpace(lang)
                ? DefaultLanguage
                : lang.Trim().ToLowerInvariant();

            switch (normalized)
            {
                case JapaneseCode:
                    return CultureInfo.GetCultureInfo("ja-JP");
                case EnglishCode:
                default:
                    return CultureInfo.GetCultureInfo("en-US");
            }
        }

        private static string LoadPersistedLanguage()
        {
            try
            {
                string configPath = GetConfigPath();
                if (!File.Exists(configPath))
                {
                    return null;
                }

                using FileStream stream = File.OpenRead(configPath);
                using JsonDocument document = JsonDocument.Parse(stream);

                if (document.RootElement.ValueKind != JsonValueKind.Object)
                {
                    return null;
                }

                if (!document.RootElement.TryGetProperty(LanguagePropertyName, out JsonElement languageElement))
                {
                    return null;
                }

                if (languageElement.ValueKind != JsonValueKind.String)
                {
                    return null;
                }

                return languageElement.GetString();
            }
            catch (IOException)
            {
                return null;
            }
            catch (UnauthorizedAccessException)
            {
                return null;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static string GetConfigPath()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, ConfigDirectoryName, ConfigFileName);
        }
    }
}
