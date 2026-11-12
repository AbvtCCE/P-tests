using System.Text.Json;
using Microsoft.Playwright;
using NUnit.Framework;

namespace TestPlayrights
{
    public static class TestEvidence
    {
        public static async Task CaptureAsync(
            IPage? page,
            byte[]? screenshotBytes = null,
            string? pageUrl = null)
        {
            var test = TestContext.CurrentContext.Test;
            var evidenceDirectory = Environment.GetEnvironmentVariable("TEST_EVIDENCE_DIRECTORY");
            if (string.IsNullOrWhiteSpace(evidenceDirectory))
            {
                evidenceDirectory = Path.Combine(
                    TestContext.CurrentContext.WorkDirectory,
                    "TestResults",
                    "Evidence");
            }

            Directory.CreateDirectory(evidenceDirectory);

            var fileStem = $"{SanitizeFileName(test.Name)}_{Guid.NewGuid():N}";
            var screenshotName = $"{fileStem}.png";
            var screenshotPath = Path.Combine(evidenceDirectory, screenshotName);
            var captureError = (string?)null;

            try
            {
                if (screenshotBytes == null && page != null && !page.IsClosed)
                {
                    screenshotBytes = await page.ScreenshotAsync(new PageScreenshotOptions
                    {
                        FullPage = true,
                        Animations = ScreenshotAnimations.Disabled,
                        Timeout = 15000
                    });
                    pageUrl = page.Url;
                }

                if (screenshotBytes != null)
                {
                    await File.WriteAllBytesAsync(screenshotPath, screenshotBytes);
                    TestContext.AddTestAttachment(screenshotPath, "Capture d'écran de fin de test");
                }
                else
                {
                    TestContext.Out.WriteLine(
                        "PREUVE: aucune capture d'écran disponible (page absente ou déjà fermée).");
                }
            }
            catch (Exception exception)
            {
                captureError = $"{exception.GetType().Name}: {exception.Message}";
                TestContext.Out.WriteLine($"PREUVE: échec de la capture d'écran: {captureError}");
            }

            var manifest = new
            {
                TestName = test.Name,
                FullName = test.FullName,
                Outcome = TestContext.CurrentContext.Result.Outcome.Status.ToString(),
                CapturedAt = DateTimeOffset.Now,
                PageUrl = SanitizePageUrl(pageUrl),
                Screenshot = screenshotBytes == null ? null : screenshotName,
                CaptureError = captureError
            };
            var manifestPath = Path.Combine(evidenceDirectory, $"{fileStem}.json");
            await File.WriteAllTextAsync(
                manifestPath,
                JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));
        }

        private static string SanitizeFileName(string value)
        {
            var invalidCharacters = Path.GetInvalidFileNameChars();
            return string.Concat(value.Select(character =>
                    invalidCharacters.Contains(character) ? '_' : character))
                .Replace(' ', '_');
        }

        private static string? SanitizePageUrl(string? pageUrl)
        {
            if (!Uri.TryCreate(pageUrl, UriKind.Absolute, out var uri))
            {
                return pageUrl;
            }

            return new UriBuilder(uri)
            {
                Query = string.Empty,
                Fragment = string.Empty
            }.Uri.AbsoluteUri;
        }
    }
}
