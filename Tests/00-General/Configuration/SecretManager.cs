using System;
using System.IO;
using Newtonsoft.Json.Linq;

namespace TestPlayrights
{
    public static class SecretManager
    {
        private static JObject _secrets = new JObject();
        private static JObject? _settings;
        private static readonly string _configPath;

        static SecretManager()
        {
            string basePath = AppContext.BaseDirectory;
            string rootPath = Path.GetFullPath(Path.Combine(basePath, "..", "..", ".."));
            _configPath = Path.Combine(rootPath, "Config");

            LoadConfiguration();
        }

        private static void LoadConfiguration()
        {
            try
            {
                string secretsPath = Path.Combine(_configPath, "secrets.json");
                if (File.Exists(secretsPath))
                {
                    string secretsJson = File.ReadAllText(secretsPath);
                    _secrets = JObject.Parse(secretsJson);
                }
                else
                {
                    _secrets = new JObject();
                }

                string settingsPath = Path.Combine(_configPath, "settings.json");
                if (File.Exists(settingsPath))
                {
                    string settingsJson = File.ReadAllText(settingsPath);
                    _settings = JObject.Parse(settingsJson);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur chargement config: {ex.Message}");
                _secrets = new JObject();
            }
        }

        public static string GetPortalPassword()
            => Environment.GetEnvironmentVariable("PORTAL_PASSWORD")
               ?? _secrets["Portal"]?["DefaultPassword"]?.ToString()
               ?? throw new Exception("Portal password non configuré");

        public static string GetIvyEmail()
            => Environment.GetEnvironmentVariable("IVY_EMAIL")
               ?? _secrets["Ivy"]?["Email"]?.ToString()
               ?? throw new InvalidOperationException(
                   "Adresse IVY non configurée. Définir IVY_EMAIL ou Config/secrets.json (Ivy.Email).");

        public static string GetIvyPassword()
            => Environment.GetEnvironmentVariable("IVY_PASSWORD")
               ?? _secrets["Ivy"]?["Password"]?.ToString()
               ?? throw new InvalidOperationException(
                   "Mot de passe IVY non configuré. Définir IVY_PASSWORD ou Config/secrets.json (Ivy.Password).");

        public static string GetKeycloakEmail()
            => Environment.GetEnvironmentVariable("KEYCLOAK_EMAIL")
               ?? _secrets["Keycloak"]?["Email"]?.ToString()
               ?? throw new Exception("Keycloak email non configuré");

        public static string GetKeycloakPassword()
            => Environment.GetEnvironmentVariable("KEYCLOAK_PASSWORD")
               ?? _secrets["Keycloak"]?["Password"]?.ToString()
               ?? throw new Exception("Keycloak password non configuré");
    }
}
