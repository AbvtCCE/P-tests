
using System.Globalization;
using System.Resources;
using System.Reflection;

public static class ResourceHelper
{
    private static readonly ResourceManager resourceManagerPortail =
    new ResourceManager("TestPlayrights.data.Portail", Assembly.GetExecutingAssembly());

    private static readonly ResourceManager resourceManagerIvy =
    new ResourceManager("TestPlayrights.data.Ivy", Assembly.GetExecutingAssembly());

    public static string GetStringPortail(string key, string culture)
    {
        var cultureInfo = new CultureInfo(culture);
        return resourceManagerPortail.GetString(key, cultureInfo);
    }


    public static string GetStringIvy(string key, string culture)
    { 
        var cultureInfo = new CultureInfo(culture);
        return resourceManagerIvy .GetString(key, cultureInfo);
    }

}
