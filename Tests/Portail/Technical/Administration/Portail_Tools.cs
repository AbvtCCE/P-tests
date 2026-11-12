using System.Text.RegularExpressions;
using Microsoft.Playwright;
using NUnit.Framework;
using TestPlayrights.Pages.Portail;

namespace TestPlayrights
{
    /// <summary>
    /// Tests fonctionnels du Portail (my-q.groupe-e.ch).
    /// Organise par domaine : Bills, User, Moving.
    /// Chaque test utilise TestCaseSource pour les combinaisons navigateur x mobile x langue.
    /// </summary>
    [TestFixture]
    [Category("Portail_Tools")]
    [Category("LEGACY_MUTATION")]
    [Explicit("Anonymise les noms de comptes dans l'environnement; réservé à une demande contrôlée.")]
    public class Portail_Tools : TestBasePortail
    {
       
        [Test]
        [TestCaseSource(nameof(GenererCombinaisonsPortail))]
        public async Task AnonymizeUsers(string navigateur, bool mobile, string lg)
        {
            // --- Configuration ---
            testName = "Plan de paiement";
            descriptionTest = "Un plan de paiement peut etre cree";
            expectedResult = "Le plan de paiement se cree";
            await Setup(navigateur, mobile, lg);

            // --- Login et langue ---
            await LogKeyClock(page);
            await RemoveErrorKeyClock(page);
            await page.GetByRole(AriaRole.Link, new() { Name = "Users" }).ClickAsync();
            await TestTools.ClientViewBy10Page(page);



            

            while(await page.Locator("[aria-label='Go to next page']").Last.GetAttributeAsync("aria-disabled") == "false")
            {
                await TestTools.AnonymizeNameOnPage(page);
                await page.Locator("[aria-label='Go to next page']").Last.ClickAsync();
                await page.WaitForTimeoutAsync(1500);
                
            }
            
        
        }

        
    }
}
