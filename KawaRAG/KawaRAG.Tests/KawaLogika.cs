// KawaLogika.cs
// Logika biznesowa kawiarni "Grande Strategia" wyodrębniona z Program.cs
// w celu umożliwienia testowania jednostkowego.

namespace KawaRAG.Tests;

using Xunit;

// ─── MODEL ───────────────────────────────────────────────────────────────────

public record Zamowienie(int Id, string Kawa, string Rozmiar, decimal Cena, DateTime Czas)
{
    public override string ToString() =>
        $"[{Czas:yyyy-MM-dd HH:mm:ss}] #{Id} | {Kawa} ({Rozmiar}) | {Cena:F2} zł";
}

// ─── CENNIK ──────────────────────────────────────────────────────────────────

public static class Cennik
{
    public static readonly Dictionary<string, Dictionary<string, decimal>> Menu =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["espresso"]   = new() { ["S"] = 7m,  ["M"] = 8m,  ["L"] = 9m  },
            ["americano"]  = new() { ["S"] = 8m,  ["M"] = 9m,  ["L"] = 10m },
            ["latte"]      = new() { ["S"] = 10m, ["M"] = 12m, ["L"] = 14m },
            ["cappuccino"] = new() { ["S"] = 10m, ["M"] = 12m, ["L"] = 14m },
            ["flat white"] = new() { ["S"] = 11m, ["M"] = 13m, ["L"] = 15m },
            ["mocha"]      = new() { ["S"] = 12m, ["M"] = 14m, ["L"] = 16m },
            ["cold brew"]  = new() { ["S"] = 13m, ["M"] = 15m, ["L"] = 17m },
        };
}

// ─── SERWIS ZAMÓWIEŃ ─────────────────────────────────────────────────────────

public interface IZamowieniaRepository
{
    void Zapisz(Zamowienie zamowienie);
}

public class FileZamowieniaRepository : IZamowieniaRepository
{
    private readonly string _sciezka;
    public FileZamowieniaRepository(string sciezka) => _sciezka = sciezka;
    public void Zapisz(Zamowienie z) => File.AppendAllText(_sciezka, z.ToString() + Environment.NewLine);
}

public class KawiarniaService
{
    private readonly List<Zamowienie> _zamowienia = new();
    private readonly IZamowieniaRepository _repo;
    private readonly Func<DateTime> _teraz;

    public KawiarniaService(IZamowieniaRepository repo, Func<DateTime>? teraz = null)
    {
        _repo  = repo;
        _teraz = teraz ?? (() => DateTime.Now);
    }

    public IReadOnlyList<Zamowienie> Zamowienia => _zamowienia.AsReadOnly();

    // ── ZłożZamówienie ───────────────────────────────────────────────────────
    public (bool Sukces, string Komunikat, Zamowienie? Zamowienie) ZlozZamowienie(string kawa, string rozmiar)
    {
        kawa    = kawa.Trim().ToLower();
        rozmiar = rozmiar.Trim().ToUpper();

        if (!Cennik.Menu.TryGetValue(kawa, out var ceny))
            return (false, $"Nie mamy '{kawa}' w menu.", null);

        if (!ceny.TryGetValue(rozmiar, out var cena))
            return (false, $"Nieprawidłowy rozmiar '{rozmiar}'. Dostępne: S, M, L.", null);

        var z = new Zamowienie(_zamowienia.Count + 1, kawa, rozmiar, cena, _teraz());
        _zamowienia.Add(z);
        _repo.Zapisz(z);

        return (true, $"Zamówienie #{z.Id}: {kawa} ({rozmiar}) — {cena:F2} zł", z);
    }

    // ── Godziny ──────────────────────────────────────────────────────────────
    public (bool Otwarta, string Komunikat) SprawdzGodzinyOtwarcia()
    {
        var teraz   = _teraz();
        bool weekend = teraz.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
        int otw = weekend ? 9 : 7;
        int zam = weekend ? 20 : 22;
        bool otwarta = teraz.Hour >= otw && teraz.Hour < zam;

        var komunikat = otwarta
            ? $"Jesteśmy otwarci! Zamykamy o {zam}:00."
            : $"Jesteśmy zamknięci. Otwieramy o {otw}:00.";

        return (otwarta, komunikat);
    }

    // ── Historia ─────────────────────────────────────────────────────────────
    public (decimal Suma, string Komunikat) PokazHistorie()
    {
        if (_zamowienia.Count == 0)
            return (0m, "Brak zamówień w tej sesji.");

        var linie = _zamowienia.Select(z => $"#{z.Id} {z.Kawa} ({z.Rozmiar}) — {z.Cena:F2} zł");
        var suma  = _zamowienia.Sum(z => z.Cena);
        return (suma, string.Join("\n", linie) + $"\nSuma: {suma:F2} zł");
    }

    // ── Rabat ────────────────────────────────────────────────────────────────
    public (decimal Procent, decimal Rabat, decimal DoZaplaty) ObliczRabat()
    {
        int ile = _zamowienia.Count;
        decimal procent = ile switch
        {
            >= 5 => 15m,
            >= 3 => 10m,
            >= 2 => 5m,
            _    => 0m
        };
        var suma    = _zamowienia.Sum(z => z.Cena);
        var rabat   = suma * procent / 100m;
        return (procent, rabat, suma - rabat);
    }
}
