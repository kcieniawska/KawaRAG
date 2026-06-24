// KawaRAG — kawiarnia "Grande Strategia"
// Rozszerzenie minimalRAG o Function Calling (Microsoft.Extensions.AI)
// Funkcje: ZlozZamowienie, SprawdzGodzinyOtwarcia, PokazHistorieZamowien, ObliczRabat

using Microsoft.Extensions.AI;
using System.ComponentModel;
using System.Text.Json;

// =============================================================================
//
// MENU
// Ile kosztuje dany rodzaj kawy?
//
// =============================================================================

var MENU = new Dictionary<string, Dictionary<string, decimal>>(StringComparer.OrdinalIgnoreCase)
{
    ["espresso"]   = new() { ["S"] = 7m,  ["M"] = 8m,  ["L"] = 9m  },
    ["americano"]  = new() { ["S"] = 8m,  ["M"] = 9m,  ["L"] = 10m },
    ["latte"]      = new() { ["S"] = 10m, ["M"] = 12m, ["L"] = 14m },
    ["cappuccino"] = new() { ["S"] = 10m, ["M"] = 12m, ["L"] = 14m },
    ["flat white"] = new() { ["S"] = 11m, ["M"] = 13m, ["L"] = 15m },
    ["mocha"]      = new() { ["S"] = 12m, ["M"] = 14m, ["L"] = 16m },
    ["cold brew"]  = new() { ["S"] = 13m, ["M"] = 15m, ["L"] = 17m },
};

// =============================================================================
//
// PROMPTY
// AI wczuwa się w baristę, zna rodzaje kaw i ich rozmiary
//
// =============================================================================

const string SYSTEM_PROMPT = """
    Jesteś barista w kawiarni "Grande Strategia" — miejscu dla programistów,
    gdzie każda kawa ma swoją legendę. Rozmawiasz po polsku, z lekkością
    i humorem (ale bez przesady). 
    
    Znasz menu na pamięć:
    - Espresso:   S=7zł,  M=8zł,  L=9zł
    - Americano:  S=8zł,  M=9zł,  L=10zł
    - Latte:      S=10zł, M=12zł, L=14zł
    - Cappuccino: S=10zł, M=12zł, L=14zł
    - Flat White: S=11zł, M=13zł, L=15zł
    - Mocha:      S=12zł, M=14zł, L=16zł
    - Cold Brew:  S=13zł, M=15zł, L=17zł
    
    Rozmiary: S (small/mały), M (medium/średni), L (large/duży).
    Gdy klient zamawia — ZAWSZE wywołaj ZlozZamowienie.
    Gdy pyta o godziny — wywołaj SprawdzGodzinyOtwarcia.
    Gdy pyta o historię zamówień — wywołaj PokazHistorieZamowien.
    Gdy pyta o rabat lub lojalność — wywołaj ObliczRabat.
    
    Jeśli klient poda niepełne dane (np. brak rozmiaru), zapytaj — 
    nie zakładaj nic samodzielnie.
    Odpowiadaj krótko i zwięźle. Nigdy nie używaj znaków \n w tekście — 
    używaj normalnych enterów. Nie powtarzaj surowego tekstu z funkcji.
    """;

// =============================================================================
//
// FUNKCJE (FUNCTION CALLING)
// Złożenie zamówień (zapisuje do pliku zamowienia.txt)
// Godziny otwarcia,  Historia zamowien, Rabaty
//
// =============================================================================

// Przechowuje historię zamówień w pamięci (+ plik)
var zamowienia = new List<Zamowienie>();
var plikZamowien = Path.Combine(AppContext.BaseDirectory, "zamowienia.txt");

[Description("Składa zamówienie na kawę, oblicza cenę i zapisuje do historii.")]
string ZlozZamowienie(
    [Description("Rodzaj kawy: espresso, americano, latte, cappuccino, flat white, mocha, cold brew")]
    string kawa,
    [Description("Rozmiar: S, M lub L")]
    string rozmiar)
{
    rozmiar = rozmiar.ToUpper().Trim();
    kawa    = kawa.Trim().ToLower();

    if (!MENU.TryGetValue(kawa, out var ceny))
        return $"Nie mamy '{kawa}' w menu. Dostępne: {string.Join(", ", MENU.Keys)}.";

    if (!ceny.TryGetValue(rozmiar, out var cena))
        return $"Nieprawidłowy rozmiar '{rozmiar}'. Dostępne: S, M, L.";

    var z = new Zamowienie(
        Id:       zamowienia.Count + 1,
        Kawa:     kawa,
        Rozmiar:  rozmiar,
        Cena:     cena,
        Czas:     DateTime.Now
    );
    zamowienia.Add(z);
    File.AppendAllText(plikZamowien, z.ToString() + Environment.NewLine);

    return $"✅ Zamówienie #{z.Id}: {kawa} ({rozmiar}) — {cena:F2} zł. " +
           $"Gotowe za chwilę! ☕";
}

[Description("Sprawdza aktualne godziny otwarcia kawiarni i czy jest teraz otwarta.")]
string SprawdzGodzinyOtwarcia()
{
    var teraz = DateTime.Now;
    bool weekend = teraz.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
    int otwarcie = weekend ? 9 : 7;
    int zamkniecie = weekend ? 20 : 22;
    bool otwarta = teraz.Hour >= otwarcie && teraz.Hour < zamkniecie;

    if (otwarta)
        return $"Tak, jesteśmy otwarci! Dzisiaj pracujemy do {zamkniecie}:00.";
    else
        return $"Niestety, jesteśmy teraz zamknięci. Jutro otwieramy o {otwarcie}:00.";
}

[Description("Pokazuje historię zamówień złożonych w tej sesji.")]
string PokazHistorieZamowien()
{
    if (zamowienia.Count == 0)
        return "Brak zamówień w tej sesji. Może coś zamówimy? ☕";

    var linie = zamowienia.Select(z =>
        $"  #{z.Id} [{z.Czas:HH:mm}] {z.Kawa} ({z.Rozmiar}) — {z.Cena:F2} zł");
    var suma = zamowienia.Sum(z => z.Cena);

    return $"📋 Historia zamówień tej sesji:\n" +
           string.Join("\n", linie) +
           $"\n  ─────────────────────\n" +
           $"  Suma: {suma:F2} zł";
}

[Description("Oblicza ewentualny rabat lojalnościowy na podstawie liczby zamówień w sesji.")]
string ObliczRabat()
{
    int ile = zamowienia.Count;
    decimal procent = ile switch
    {
        >= 5 => 15m,
        >= 3 => 10m,
        >= 2 => 5m,
        _    => 0m
    };

    if (procent == 0)
        return $"Masz {ile} zamówienie(a). Złóż jeszcze {2 - ile} więcej, " +
               "żeby odblokować 5% rabat dla stałego klienta! 🎯";

    var suma    = zamowienia.Sum(z => z.Cena);
    var rabat   = suma * procent / 100m;
    var doZapl  = suma - rabat;

    return $"🎉 Jesteś stałym klientem! Masz {ile} zamówień.\n" +
           $"  Rabat: {procent}% → -{rabat:F2} zł\n" +
           $"  Do zapłaty: {doZapl:F2} zł (zamiast {suma:F2} zł)";
}

// =============================================================================
//
// REJESTRACJA FUNKCJI
// ZlozZamowienie(), SprawdzGodzinyOtwarcia(), PokazHistorieZamowien(), ObliczRabat()
//
// =============================================================================

var tools = new[]
{
    AIFunctionFactory.Create(ZlozZamowienie,        "ZlozZamowienie"),
    AIFunctionFactory.Create(SprawdzGodzinyOtwarcia,"SprawdzGodzinyOtwarcia"),
    AIFunctionFactory.Create(PokazHistorieZamowien, "PokazHistorieZamowien"),
    AIFunctionFactory.Create(ObliczRabat,         "ObliczRabat"),
};

// =============================================================================
//
// WYBÓR VENDORA
//
// =============================================================================

var vendor = (Environment.GetEnvironmentVariable("VENDOR") ?? "ollama").ToLowerInvariant();

(IChatClient rawClient, string modelId) = vendor switch
{
    "openai" => BuildOpenAI(),
    "anthropic" => BuildAnthropic(),
    "ollama" => BuildOllama(),
    _ => BuildOllama()
};

// Automatycznie wywołuje funkcje:
IChatClient client = rawClient
   
var chatOptions = new ChatOptions
{
    ModelId = modelId,
    MaxOutputTokens = 1024,
    Tools = tools,
};

// =============================================================================
//
// KONWERSACJA 
//
// =============================================================================

Console.Clear();
Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
Console.WriteLine("║   ☕  Grande Strategia — kawiarnia dla koderów     ║");
Console.WriteLine("║   Wpisz zamówienie lub pytanie. [Enter]=quit ║");
Console.WriteLine($"║   Vendor: {vendor,-10} Model: {modelId,-15} ║");
Console.WriteLine("╚════════════════════════════════════════════════════════════╝");

var history = new List<ChatMessage>
{
    new(ChatRole.System, SYSTEM_PROMPT)
};

while (true)
{
    Console.Write("\n☕ > ");
    var input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input)) break;

    history.Add(new ChatMessage(ChatRole.User, input));
    var response = await client.GetResponseAsync(history, chatOptions);
    var answer   = response.Text ?? "";
    history.Add(new ChatMessage(ChatRole.Assistant, answer));

    Console.WriteLine($"\n🤖 {answer}");
}

Console.WriteLine("\nDziękujemy za odwiedziny w Grande Strategia! Do zobaczenia ☕");
return 0;

// =============================================================================
//
// KLIENCI
//
// =============================================================================

static (IChatClient, string) BuildOpenAI()
{
    var key   = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                ?? throw new InvalidOperationException("Brak OPENAI_API_KEY");
    var model = Environment.GetEnvironmentVariable("OPENAI_MODEL") ?? "gpt-4o-mini";
    IChatClient c = new OpenAI.Chat.ChatClient(model, key).AsIChatClient();
    return (c, model);
}

static (IChatClient, string) BuildOllama()
{
    var url = Environment.GetEnvironmentVariable("OLLAMA_URL") ?? "http://localhost:11434/v1";
    var model = Environment.GetEnvironmentVariable("OLLAMA_MODEL") ?? "gemma4:latest";
    var openAIClient = new OpenAI.OpenAIClient(
        new System.ClientModel.ApiKeyCredential("ollama"),
        new OpenAI.OpenAIClientOptions { Endpoint = new Uri(url) }
    );
    IChatClient c = openAIClient.GetChatClient(model).AsIChatClient();
    return (c, model);
}

static (IChatClient, string) BuildAnthropic()
{
    var model = Environment.GetEnvironmentVariable("ANTHROPIC_MODEL")
                ?? Anthropic.SDK.Constants.AnthropicModels.Claude37Sonnet;
    IChatClient c = new Anthropic.SDK.AnthropicClient()
        .Messages
        .AsBuilder()
        .Build();
    return (c, model);
}

// =============================================================================
//
// MODEL ZAMÓWIENIA
//
// =============================================================================

record Zamowienie(int Id, string Kawa, string Rozmiar, decimal Cena, DateTime Czas)
{
    public override string ToString() =>
        $"[{Czas:yyyy-MM-dd HH:mm:ss}] #{Id} | {Kawa} ({Rozmiar}) | {Cena:F2} zł";
}
