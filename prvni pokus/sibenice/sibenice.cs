using System.Text;

Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

const int MaxChyb = 7;
const string SouborSlov = "slova.txt";

string[] slova = NactiSlova();
if (slova.Length == 0)
{
    Console.WriteLine($"Soubor {SouborSlov} je prázdný nebo neexistuje.");
    return;
}

var random = new Random();
bool hrat = true;

while (hrat)
{
    Hra(slova[random.Next(slova.Length)]);

    Console.Write("\nChceš hrát znovu? (a/n): ");
    string? odpoved = Console.ReadLine()?.Trim().ToLower();
    hrat = odpoved == "a" || odpoved == "ano";
}

Console.WriteLine("Díky za hru!");

// ---------------------------------------------------------------

string[] NactiSlova()
{
    string[] cesty =
    {
        Path.Combine(AppContext.BaseDirectory, SouborSlov),
        Path.Combine(Directory.GetCurrentDirectory(), SouborSlov)
    };

    foreach (string cesta in cesty)
    {
        if (File.Exists(cesta))
        {
            return File.ReadAllLines(cesta, Encoding.UTF8)
                       .Select(s => s.Trim().ToUpper())
                       .Where(s => s.Length > 0)
                       .Distinct()
                       .ToArray();
        }
    }
    return Array.Empty<string>();
}

void Hra(string tajenka)
{
    var uhodnute = new HashSet<char>();
    var spatne = new List<char>();

    while (true)
    {
        Console.Clear();
        Console.WriteLine("=== ŠIBENICE ===\n");
        Console.WriteLine(Nakresli(spatne.Count));

        string zobrazeni = string.Join(" ", tajenka.Select(c => uhodnute.Contains(c) ? c : '_'));
        Console.WriteLine($"\nSlovo: {zobrazeni}");
        Console.WriteLine($"Špatná písmena: {(spatne.Count > 0 ? string.Join(", ", spatne) : "-")}");
        Console.WriteLine($"Zbývá pokusů: {MaxChyb - spatne.Count}");

        if (tajenka.All(uhodnute.Contains))
        {
            Console.WriteLine("\nVyhrál jsi! 🎉");
            return;
        }
        if (spatne.Count >= MaxChyb)
        {
            Console.WriteLine($"\nProhrál jsi. Slovo bylo: {tajenka}");
            return;
        }

        Console.Write("\nZadej písmeno (nebo celé slovo): ");
        string vstup = (Console.ReadLine() ?? "").Trim().ToUpper();

        if (vstup.Length == 0)
            continue;

        if (vstup.Length > 1)
        {
            // tip na celé slovo
            if (vstup == tajenka)
            {
                foreach (char c in tajenka) uhodnute.Add(c);
            }
            else
            {
                spatne.Add('?');
            }
            continue;
        }

        char pismeno = vstup[0];
        if (!char.IsLetter(pismeno) || uhodnute.Contains(pismeno) || spatne.Contains(pismeno))
            continue;

        if (tajenka.Contains(pismeno))
            uhodnute.Add(pismeno);
        else
            spatne.Add(pismeno);
    }
}

string Nakresli(int chyby)
{
    var sb = new StringBuilder();

    char h = chyby >= 3 ? 'O' : ' ';
    char t = chyby >= 4 ? '|' : ' ';
    char lr = chyby >= 5 ? '/' : ' ';
    char pr = chyby >= 6 ? '\\' : ' ';
    char ln = chyby >= 7 ? '/' : ' ';
    char pn = chyby >= 7 ? '\\' : ' ';

    sb.AppendLine(chyby >= 2 ? "  +---+" : "       ");
    sb.AppendLine(chyby >= 2 ? $"  |   {(chyby >= 3 ? "|" : " ")}" : (chyby >= 1 ? "  |" : ""));
    sb.AppendLine(chyby >= 1 ? $"  |   {h}" : "");
    sb.AppendLine(chyby >= 1 ? $"  |  {lr}{t}{pr}" : "");
    sb.AppendLine(chyby >= 1 ? $"  |  {ln} {pn}" : "");
    sb.AppendLine(chyby >= 1 ? "  |" : "");
    sb.Append(chyby >= 1 ? "=========" : "");
    return sb.ToString();
}
