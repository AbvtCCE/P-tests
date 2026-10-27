namespace TestPlayrights
{
    /// <summary>
    /// Configuration centralisée pour tous les tests.
    /// Lit les variables d'environnement une seule fois au démarrage.
    ///
    /// Variables d'environnement supportées :
    /// - CI           : "true" en GitHub Actions (headless automatique)
    /// - HEADLESS     : "true"/"false" pour forcer le mode headless
    /// - DEBUG_PAUSE  : "true" pour activer page.PauseAsync() en local
    /// - SLOW_MO      : millisecondes de délai entre chaque action Playwright
    /// </summary>
    public static class TestConfig
    {
        #region Environnement

        /// <summary>
        /// True si on tourne dans GitHub Actions (variable CI=true)
        /// </summary>
        public static bool IsCI { get; } =
            !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI"));

        /// <summary>
        /// True si le navigateur doit tourner en headless.
        /// Automatiquement true en CI, sinon lit HEADLESS env var.
        /// En local sans variable : false (on voit le navigateur).
        /// </summary>
        public static bool IsHeadless { get; } =
            !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI"))
            || string.Equals(
                Environment.GetEnvironmentVariable("HEADLESS"),
                "true",
                StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// True si on veut insérer des page.PauseAsync() pour debug local.
        /// Ignoré en CI.
        /// </summary>
        public static bool IsDebugPause { get; } =
            string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI"))
            && string.Equals(
                Environment.GetEnvironmentVariable("DEBUG_PAUSE"),
                "true",
                StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Délai en ms entre chaque action Playwright (0 = pas de délai).
        /// Utile pour le debug visuel en local.
        /// </summary>
        public static int SlowMo { get; } =
            int.TryParse(Environment.GetEnvironmentVariable("SLOW_MO"), out var val) ? val : 0;

        #endregion
    }
}
