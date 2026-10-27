using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

public static class DataLoader
{
    static List<Partenaire> clients = new List<Partenaire>();
    public static List<Partenaire> GetPartenaires(string filepath)
    {
        clients = new List<Partenaire>();
        var lines = File.ReadAllLines(filepath).ToList();
        if (lines.Count < 2) return clients;
        var header = lines[0].Split(';');
        var properties = typeof(Partenaire).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        for (int i = 1; i < lines.Count; i++)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;
            var values = line.Split(';');
            if (values.Length < header.Length) continue;
            var data = header.Zip(values, (col, val) => new { col, val }).ToDictionary(x => x.col.Trim(), x => x.val.Trim());
            var client = new Partenaire();

            foreach (var prop in properties)
            {
                if (data.TryGetValue(prop.Name, out var value))
                {
                    try
                    {
                        var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                        if (targetType == typeof(bool))
                        {
                            if (bool.TryParse(value, out bool boolVal))
                                prop.SetValue(client, boolVal);
                            else if (value == "0")
                                prop.SetValue(client, false);
                            else if (value == "1")
                                prop.SetValue(client, true);
                            else
                                prop.SetValue(client, null);
                        }
                        else if (targetType == typeof(int))
                        {
                            if (int.TryParse(value.Replace("'", ""), out int intVal))
                                prop.SetValue(client, intVal);
                        }
                        else if (targetType == typeof(float))
                        {
                            if (float.TryParse(value.Replace("'", "").Replace(",", "."), out float floatVal))
                                prop.SetValue(client, floatVal);
                        }
                        else
                        {
                            var convertedValue = Convert.ChangeType(value, targetType);
                            prop.SetValue(client, convertedValue);
                        }
                    }
                    catch
                    {
                        // Ignore conversion errors
                    }
                }
            }

            clients.Add(client);
        }

        return clients;
    }

    public static Partenaire? FindClientWithCriteria(List<Partenaire> clients, Dictionary<string, string?> criteres)
    {
        var properties = typeof(Partenaire).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var client in clients)
        {
            bool match = true;
            foreach (var critere in criteres)
            {
                var prop = properties.FirstOrDefault(p => p.Name.Equals(critere.Key, StringComparison.OrdinalIgnoreCase));
                if (prop == null) continue;
                var value = prop.GetValue(client)?.ToString()?.ToLowerInvariant();
                var expected = critere.Value?.ToLowerInvariant();
                if (value != expected)
                {
                    match = false;
                    break;
                }
            }
            if (match)
                return client;
        }
        return null;
    }

    public static void SaveClientsToCsv(string filepath, List<Partenaire> clients)
    {
        var properties = typeof(Partenaire).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        using (var writer = new StreamWriter(filepath, false, Encoding.UTF8))
        {
            writer.WriteLine(string.Join(";", properties.Select(p => p.Name)));
            foreach (var client in clients)
            {
                var values = properties.Select(p =>
                {
                    var val = p.GetValue(client);
                    return val != null ? val.ToString()?.Replace(";", ",") : "";
                });
                writer.WriteLine(string.Join(";", values));
            }
        }
    }

    public static void MettreAJourPartenaire(string filepath, Partenaire partenaireModifie)
    {
        var partenaires = GetPartenaires(filepath);
        for (int i = 0; i < partenaires.Count; i++)
        {
            if (partenaires[i].NumeroPartenaire == partenaireModifie.NumeroPartenaire)
            {
                partenaires[i] = partenaireModifie;
                break;
            }
        }
        SaveClientsToCsv(filepath, partenaires);
    }

    
public static void AppendUniqueClientsToCsv(string filepath, List<Partenaire> nouveauxClients)
{
    var existants = GetPartenaires(filepath);
    var properties = typeof(Partenaire).GetProperties(BindingFlags.Public | BindingFlags.Instance);

    // Filtrer les nouveaux clients pour éviter les doublons
    var uniques = nouveauxClients.Where(nc =>
        !existants.Any(e =>
            (e.Email != null && e.Email.Equals(nc.Email, StringComparison.OrdinalIgnoreCase)) ||
            (e.NumeroPartenaire != null && e.NumeroPartenaire == nc.NumeroPartenaire)
        )).ToList();

    if (!File.Exists(filepath) || new FileInfo(filepath).Length == 0)
    {
        // Si le fichier n'existe pas ou est vide, écrire l'en-tête
        using (var writer = new StreamWriter(filepath, true, Encoding.UTF8))
        {
            writer.WriteLine(string.Join(";", properties.Select(p => p.Name)));
        }
    }

    using (var writer = new StreamWriter(filepath, true, Encoding.UTF8))
    {
        foreach (var client in uniques)
        {
            var values = properties.Select(p =>
            {
                var val = p.GetValue(client);
                return val != null ? val.ToString()?.Replace(";", ",") : "";
            });
            writer.WriteLine(string.Join(";", values));
        }
    }
}

}
