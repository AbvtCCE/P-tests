using Microsoft.Playwright;
using NUnit.Framework;

namespace TestPlayrights
{
    /// <summary>
    /// Tests Smoke SN Nestor (nestor.groupe-e.ch).
    /// Ouvre le dashboard Nestor et met en pause pour inspection.
    /// </summary>
    [TestFixture]
    [Category("CI_SMOKE_NESTOR")]
    public class Nestor_Smoke : TestBaseNestor
    {
        #region Configuration

        protected override Dictionary<string, string?> criteresSelection { get; set; } = new();

        #endregion

        #region Test Smoke

        /// <summary>
        /// Test Smoke Nestor : ouvre l'URL du dashboard et pause pour inspection.
        /// </summary>
        [Test]
        [TestCaseSource(nameof(GenererCombinaisonsNestor))]
        public async Task SmokeNestor(string navigateur, bool mobile, string lg)
        {
            // --- Configuration ---
            testName = "SmokeNestor";
            await Setup(navigateur, mobile, lg);
            string label = $"Nestor {lg}";
            LogGitHubActions(label);

            // --- Ouverture de Nestor ---
            await OuvrirNestor();

            // --- Pause pour inspection manuelle ---
            await page!.PauseAsync();


        await page.GetByRole(AriaRole.Textbox, new() { Name = "someone@example.com" }).FillAsync(Serializer.EmailIvy);
        await page.GetByRole(AriaRole.Textbox, new() { Name = "someone@example.com" }).PressAsync("Enter");

        await page.GetByRole(AriaRole.Textbox, new() { Name = "Entrer le mot de passe pour" }).FillAsync(Serializer.MdpIvy);
        await page.GetByRole(AriaRole.Button, new() { Name = "Se connecter" }).ClickAsync();
        await page.GotoAsync("https://login.microsoftonline.com/common/SAS/ProcessAuth");

        await page.GetByRole(AriaRole.Button, new() { Name = "Oui" }).ClickAsync();
        await page.GotoAsync("https://nestor.groupe-e.ch/now/platform-analytics-workspace/dashboards/params/edit/false/sys-id/bf0b23e43731244cc0c590218ddd6202/__state__/b64~eyI5MjNhOTEzYjUzMmMwMTEwYWFiOWRkZWVmZjdiMTI0MyI6eyJsaXN0X2NvbnRyb2xsZXJfMyI6eyJfIjp7InF1ZXJ5IjoiYWN0aXZlPXRydWVeYXNzaWdubWVudF9ncm91cExJS0VDQ0UtXk9SREVSQllERVNDc3lzX2NyZWF0ZWRfb24ifX19fQ%3D%3D");
       // await Expect(page.GetByRole(AriaRole.Main, new() { Name = "Contenu de l'écran" })).ToBeVisibleAsync();

        await page.GetByRole(AriaRole.Tab, new() { Name = "Changes" }).ClickAsync();
       // await Expect(page.GetByRole(AriaRole.Row, new() { Name = "row Actions Numéro Périmètre" })).ToBeVisibleAsync();

        await page.GetByLabel("Interactive chart", new() { Exact = true }).GetByText("Évaluer").ClickAsync();
        await page.GetByLabel("Chart. Highcharts interactive").GetByText("22", new() { Exact = true }).ClickAsync();
       // await Expect(page.Locator("iframe[name=\"gsft_main\"]").ContentFrame.GetByRole(AriaRole.Main, new() { Name = "Demandes de changement" })).ToBeVisibleAsync();

        await page.Locator("iframe[name=\"gsft_main\"]").ContentFrame.GetByRole(AriaRole.Gridcell, new() { Name = "Ouvrir l'enregistrement: CHG0037792" }).ClickAsync();
        await page.GotoAsync("https://nestor.groupe-e.ch/now/nav/ui/classic/params/target/change_request.do%3Fsys_id%3D45167b4a877abe10dd1f33f80cbb355e%26sysparm_view%3Dundefined%26sysparm_record_target%3Dchange_request%26sysparm_record_row%3D2%26sysparm_record_rows%3D22%26sysparm_record_list%3DstateNOT%2BIN3%252C4%255Eu_change_scope%253Dcc_energie%255Estate%253D-1%255Esys_created_by%253Dmichael.hofer%2540ccenergie.ch%255EORDERBYtype%26sysparm_view%3Dundefined");


        }

        #endregion
    }
}
