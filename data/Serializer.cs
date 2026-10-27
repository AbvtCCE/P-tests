namespace TestPlayrights
{
    public static class Serializer
    {
        //*******************************Paramètres de tests******************************************************************************
        public static string pathToCheckFolder = GetRootPath() + "PlaywrightTests/01_ToCheck/" + DateTime.Now.ToString("yyyyMMdd");
        public static string pathToResultsBase = GetRootPath() + "PlaywrightTests/02_Results/" + DateTime.Now.ToString("yyyyMMdd");
        public static string mdp => SecretManager.GetPortalPassword();

        public static string EmailIvy => SecretManager.GetIvyEmail();
        public static string MdpIvy  => SecretManager.GetIvyPassword();
        public static string EmailKeycloak => SecretManager.GetKeycloakEmail();
        public static string MdpKeycloak   => SecretManager.GetKeycloakPassword();
        public static string basePath = AppContext.BaseDirectory;
        public static string rootPath = Path.GetFullPath(Path.Combine(basePath, "..", "..", "..")); // remonte à la racine du projet
        public static string PathToPartenaireCsv = Path.Combine(rootPath, "data", "Partenaires.csv");
        public static string PathToEnvironmentsCsv = Path.Combine(rootPath, "data", "Environments.csv");
        public static string pathToReportFolder =  "REPORTS/" + DateTime.Now.ToString("yyyyMMdd");

        //*****************************************DONNEE OBLIGATOIRE*****************************************************************
        /// <summary>
        /// Obtient l'URL du portail pour l'environnement actif
        /// </summary>
       public static string GetActiveUrl()
        {
            var environments = LoadEnvironments();
            var activeEnv = environments.FirstOrDefault(e => e.IsActive);
            return activeEnv?.Url ;
        }

        public static string GetUrlIvyQual()
        {
            return "https://ivy-qua.cce24x7.net/ivy/pro/CCE/PortalTemplate/1549F58C18A6C562/DefaultApplicationHomePage.ivp";
        }
        public static string GetUrlPortailQual()
        {
            return "https://my-q.groupe-e.ch/";
        }

        public static string GetUrlKeyClockQual()
        {
            return "https://login.my-q.groupe-e.ch/admin/master/console/#/my-groupe-e/users/a665c808-fcf0-46a5-b717-ac3ce9989c7a/credentials";
        }
        public static string GetUrlNestor()
        {
            return "https://nestor.groupe-e.ch/now/nav/ui/classic/params/target/%24pa_dashboard.do";
        }

        /// <summary>
        /// Charge tous les environnements depuis le fichier CSV
        /// </summary>
        public static List<Environments> LoadEnvironments()
        {
            var environments = new List<Environments>();
    
    if (!File.Exists(PathToEnvironmentsCsv))
    {
        CreateDefaultEnvironmentsFile();
    }

    var lines = File.ReadAllLines(PathToEnvironmentsCsv);
    
    // Lire TOUTES les lignes sauf la première (header)
    for (int i = 1; i < lines.Length; i++)
    {
        if (string.IsNullOrWhiteSpace(lines[i])) continue; // Skip lignes vides
        
        var parts = lines[i].Split(';');
        if (parts.Length >= 3)
        {
            environments.Add(new Environments
            {
                Name = parts[0].Trim(),
                Url = parts[1].Trim(),
                IsActive = bool.Parse(parts[2].Trim())
            });
        }
    }
    
    return environments;
        }


        /// <summary>
        /// Met à jour l'environnement actif
        /// </summary>
        public static void SetActiveEnvironment(string environmentName)
        {
            var environments = LoadEnvironments();

            // Désactiver tous les environnements
            foreach (var env in environments)
            {
                env.IsActive = false;
            }

            // Activer l'environnement sélectionné
            var selectedEnv = environments.FirstOrDefault(e => e.Name == environmentName);
            if (selectedEnv != null)
            {
                selectedEnv.IsActive = true;
            }

            SaveEnvironments(environments);
        }

        /// <summary>
        /// Sauvegarde la liste des environnements
        /// </summary>
        public static void SaveEnvironments(List<Environments> environments)
        {
            using (var writer = new StreamWriter(PathToEnvironmentsCsv))
            {
                writer.WriteLine("Environment;PortalUrl;IvyUrl;IsActive");
                foreach (var env in environments)
                {
                    writer.WriteLine($"{env.Name};{env.Url};{env.IsActive}");
                }
            }
        }

        /// <summary>
        /// Crée le fichier d'environnements par défaut
        /// </summary>
        private static void CreateDefaultEnvironmentsFile()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PathToEnvironmentsCsv));
    
    var lines = new string[]
    {
        "Environment;PortalUrl;IvyUrl;IsActive",
        "Portail Qual;https://my-q.groupe-e.ch/qwe;https://ivy-qua.cce24x7.net/;false",
        "Portail Dev;https://uvcerz0009.sys.cce24x7.net:8443/;https://dev-ivy.cce24x7.net/;true",
        "IVY Qual;https://my-q.groupe-e.ch/;https://ivy-qua.cce24x7.net/;false",
        "IVY Dev;https://my-dev.groupe-e.ch/;https://dev-ivy.cce24x7.net/;false"
    };
    
    File.WriteAllLines(PathToEnvironmentsCsv, lines);
        }

        public static string GetRootPath()
        {
            return AppDomain.CurrentDomain.BaseDirectory;
        }

        /// <summary>
        /// Génère le chemin pour les résultats d'un test spécifique
        /// </summary>
        /// <param name="testName">Nom du test</param>
        /// <returns>Chemin complet vers le dossier du test</returns>
        public static string GetTestResultPath(string testName)
        {
            string cleanTestName = testName?.Replace(":", "").Replace("/", "_").Replace("\\", "_") ?? "TestInconnu";
            
            string testFolder = Path.Combine(pathToReportFolder, cleanTestName);
            
            if (!Directory.Exists(pathToReportFolder))
            {
                Directory.CreateDirectory(pathToReportFolder);
            }
            
            if (!Directory.Exists(testFolder))
            {
                Directory.CreateDirectory(testFolder);
            }
            
            return testFolder;
        }


    }

            /// <summary>
            /// Classe représentant un environnement de test
            /// </summary>
            public class Environments
            {
                public string Name { get; set; }
                public string Url { get; set; }
                public bool IsActive { get; set; }
            }




    }
    
