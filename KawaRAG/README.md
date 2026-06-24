# ☕ KawaRAG — Grande Strategia

> **Autorska modyfikacja projektu minimalRAG.**  
> Zamiast encyklopedii firm, temat: **kawiarnia dla programistów** z pełnym Function Calling.

---

## Co nowego względem oryginału?

| Cecha | minimalRAG (oryginał) | KawaRAG (ta wersja) |
|---|---|---|
| Baza wiedzy | `whatYouKnow.txt` (statyczny plik) | Zakodowana w systemPrompt + dynamiczna |
| Function Calling | ❌ brak | ✅ 4 funkcje z `AIFunctionFactory` |
| Zapis do pliku | ❌ | ✅ `zamowienia.txt` |
| Auto-wywołanie narzędzi | ❌ | ✅ `UseFunctionInvocation()` middleware |
| Rabaty lojalnościowe | ❌ | ✅ progresywny system 5/10/15% |
| Historia sesji | ❌ | ✅ per sesja, z sumą |
| Godziny otwarcia | ❌ | ✅ z rozróżnieniem dzień/weekend |
| UI | zwykłe `Console.Write` | ✅ ramka ASCII, emoji |

---

## Funkcje (Tools / Function Calling)

### `ZlozZamowienie(string kawa, string rozmiar)`
Oblicza cenę na podstawie słownikowego cennika i zapisuje zamówienie do:
- listy w pamięci (historia sesji)
- pliku `zamowienia.txt` (trwały log)

### `SprawdzGodzinyOtwarcia()`
Zwraca godziny i informuje czy kawiarnia jest **teraz** otwarta  
(weekendy 9–20, dni robocze 7–22).

### `PokazHistorieZamowien()`
Listuje wszystkie zamówienia złożone w tej sesji z sumą do zapłaty.

### `ObliczRabat()`
Progresywny rabat lojalnościowy:
- 2+ zamówienia → **5%**
- 3+ zamówienia → **10%**
- 5+ zamówień  → **15%**

---

## Jak uruchomić

```bash
# Domyślnie (Ollama lokalnie, model llama3.2):
dotnet run

# OpenAI:
VENDOR=openai OPENAI_API_KEY=sk-... dotnet run

# Anthropic:
VENDOR=anthropic ANTHROPIC_API_KEY=sk-ant-... dotnet run
```

## Przykładowe rozmowy

```
☕ > Poproszę duże Latte
🤖 ✅ Zamówienie #1: latte (L) — 14,00 zł. Gotowe za chwilę! ☕

☕ > Czy jesteście teraz otwarci?
🤖 🟢 Teraz otwarte! Zamykamy o 22:00.

☕ > Ile już wydałem?
🤖 📋 Historia zamówień tej sesji:
     #1 [14:32] latte (L) — 14,00 zł
     ─────────────────────
     Suma: 14,00 zł

☕ > Mam jakiś rabat?
🤖 Masz 1 zamówienie. Złóż jeszcze 1 więcej, żeby odblokować 5% rabat! 🎯
```

---

## Kluczowa technika: `UseFunctionInvocation()`

```csharp
IChatClient client = rawClient
    .AsBuilder()
    .UseFunctionInvocation()   // middleware: AI sama decyduje kiedy wywołać funkcję
    .Build();
```

Model otrzymuje opisy funkcji, a biblioteka `Microsoft.Extensions.AI` automatycznie:
1. Przekazuje definicje narzędzi do API
2. Wykrywa gdy model chce wywołać funkcję
3. Uruchamia lokalny kod C#
4. Zwraca wynik z powrotem do modelu
5. Model odpowiada finalnie użytkownikowi

Użytkownik widzi tylko gotową odpowiedź — bez żadnego kodu po swojej stronie.
