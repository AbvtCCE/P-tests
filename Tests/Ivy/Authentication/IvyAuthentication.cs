using Microsoft.Playwright;

namespace TestPlayrights
{
    public static class IvyAuthentication
    {
        public static async Task SignInAsync(IPage page)
        {
            await page.GotoAsync(Serializer.GetUrlIvyQual());

            var loginPage = new PageLoginWindows(page);
            await loginPage.SignInAsync(Serializer.EmailIvy, Serializer.MdpIvy);
        }
    }
}
