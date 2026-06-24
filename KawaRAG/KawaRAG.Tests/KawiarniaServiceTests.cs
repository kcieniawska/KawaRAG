// KawiarniaServiceTests.cs
// Testy jednostkowe serwisu KawiarniaService z użyciem mocków (Moq).

using FluentAssertions;
using Moq;
using Xunit;
namespace KawaRAG.Tests;

public class KawiarniaServiceTests
{
    // ── Fixture — wspólny setup ──────────────────────────────────────────────

    private static (KawiarniaService Service, Mock<IZamowieniaRepository> RepoMock)
        UtworzSerwis(DateTime? czas = null)
    {
        var mock    = new Mock<IZamowieniaRepository>();
        var fixTime = czas ?? new DateTime(2025, 6, 13, 14, 0, 0); // piątek 14:00
        var serwis  = new KawiarniaService(mock.Object, () => fixTime);
        return (serwis, mock);
    }

    // ════════════════════════════════════════════════════════════════════════
    // SEKCJA 1: ZłożZamówienie
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ZlozZamowienie_PoprawnaKawa_ZwracaSukces()
    {
        var (serwis, _) = UtworzSerwis();

        var (sukces, komunikat, zamowienie) = serwis.ZlozZamowienie("latte", "L");

        sukces.Should().BeTrue();
        zamowienie.Should().NotBeNull();
        zamowienie!.Cena.Should().Be(14m);
    }

    [Theory]
    [InlineData("latte",      "S", 10)]
    [InlineData("latte",      "M", 12)]
    [InlineData("latte",      "L", 14)]
    [InlineData("espresso",   "S",  7)]
    [InlineData("cappuccino", "M", 12)]
    [InlineData("mocha",      "L", 16)]
    [InlineData("cold brew",  "S", 13)]
    public void ZlozZamowienie_PoprawnaKawaIRozmiar_ObiczaPoprawnieCene(
        string kawa, string rozmiar, decimal oczekiwanaCena)
    {
        var (serwis, _) = UtworzSerwis();

        var (sukces, _, zamowienie) = serwis.ZlozZamowienie(kawa, rozmiar);

        sukces.Should().BeTrue();
        zamowienie!.Cena.Should().Be(oczekiwanaCena);
    }

    [Fact]
    public void ZlozZamowienie_NieznanasKawa_ZwracaBlad()
    {
        var (serwis, _) = UtworzSerwis();

        var (sukces, komunikat, zamowienie) = serwis.ZlozZamowienie("herbata", "M");

        sukces.Should().BeFalse();
        zamowienie.Should().BeNull();
        komunikat.Should().Contain("herbata");
    }

    [Theory]
    [InlineData("XL")]
    [InlineData("XXL")]
    [InlineData("duzy")]
    [InlineData("")]
    public void ZlozZamowienie_NieprawidlowyRozmiar_ZwracaBlad(string rozmiar)
    {
        var (serwis, _) = UtworzSerwis();

        var (sukces, komunikat, _) = serwis.ZlozZamowienie("latte", rozmiar);

        sukces.Should().BeFalse();
        komunikat.Should().Contain("Nieprawidłowy");
    }

    [Theory]
    [InlineData("LATTE", "l")]
    [InlineData("Espresso", "M")]
    [InlineData("COLD BREW", "S")]
    public void ZlozZamowienie_RozneCase_AkceptujePoprawneDane(string kawa, string rozmiar)
    {
        var (serwis, _) = UtworzSerwis();

        var (sukces, _, _) = serwis.ZlozZamowienie(kawa, rozmiar);

        sukces.Should().BeTrue();
    }

    [Fact]
    public void ZlozZamowienie_KilkaZamowien_NumerujePo_Kolei()
    {
        var (serwis, _) = UtworzSerwis();

        serwis.ZlozZamowienie("latte", "S");
        serwis.ZlozZamowienie("mocha", "M");
        var (_, _, trzecie) = serwis.ZlozZamowienie("espresso", "L");

        trzecie!.Id.Should().Be(3);
    }

    [Fact]
    public void ZlozZamowienie_ZapisujeDoPersistencji()
    {
        var (serwis, repoMock) = UtworzSerwis();

        serwis.ZlozZamowienie("latte", "L");

        // Verify: mock repo musiał dostać wywołanie Zapisz
        repoMock.Verify(r => r.Zapisz(It.Is<Zamowienie>(z =>
            z.Kawa == "latte" && z.Rozmiar == "L" && z.Cena == 14m)), Times.Once);
    }

    [Fact]
    public void ZlozZamowienie_Nieudane_NieZapisujeDoRepo()
    {
        var (serwis, repoMock) = UtworzSerwis();

        serwis.ZlozZamowienie("herbata", "M"); // błędna kawa

        repoMock.Verify(r => r.Zapisz(It.IsAny<Zamowienie>()), Times.Never);
    }

    // ════════════════════════════════════════════════════════════════════════
    // SEKCJA 2: SprawdzGodzinyOtwarcia
    // ════════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(7)]
    [InlineData(12)]
    [InlineData(21)]
    public void SprawdzGodziny_DzienRoboczy_WGodzinachOtwarcia_ZwracaOtwarte(int godzina)
    {
        // Piątek
        var (serwis, _) = UtworzSerwis(new DateTime(2025, 6, 13, godzina, 0, 0));

        var (otwarta, _) = serwis.SprawdzGodzinyOtwarcia();

        otwarta.Should().BeTrue();
    }

    [Theory]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(22)]
    [InlineData(23)]
    public void SprawdzGodziny_DzienRoboczy_PozaGodzinami_ZwracaZamkniete(int godzina)
    {
        var (serwis, _) = UtworzSerwis(new DateTime(2025, 6, 13, godzina, 0, 0));

        var (otwarta, _) = serwis.SprawdzGodzinyOtwarcia();

        otwarta.Should().BeFalse();
    }

    [Theory]
    [InlineData(9)]
    [InlineData(14)]
    [InlineData(19)]
    public void SprawdzGodziny_Weekend_WGodzinachOtwarcia_ZwracaOtwarte(int godzina)
    {
        // Sobota
        var (serwis, _) = UtworzSerwis(new DateTime(2025, 6, 14, godzina, 0, 0));

        var (otwarta, _) = serwis.SprawdzGodzinyOtwarcia();

        otwarta.Should().BeTrue();
    }

    [Theory]
    [InlineData(7)]  // za wcześnie w weekend
    [InlineData(20)] // za późno w weekend
    public void SprawdzGodziny_Weekend_PozaGodzinami_ZwracaZamkniete(int godzina)
    {
        var (serwis, _) = UtworzSerwis(new DateTime(2025, 6, 14, godzina, 0, 0));

        var (otwarta, _) = serwis.SprawdzGodzinyOtwarcia();

        otwarta.Should().BeFalse();
    }

    [Fact]
    public void SprawdzGodziny_Komunikat_ZawieraGodzineZamkniecia()
    {
        // Piątek 14:00 — otwarte do 22
        var (serwis, _) = UtworzSerwis(new DateTime(2025, 6, 13, 14, 0, 0));

        var (_, komunikat) = serwis.SprawdzGodzinyOtwarcia();

        komunikat.Should().Contain("22");
    }

    // ════════════════════════════════════════════════════════════════════════
    // SEKCJA 3: Historia zamówień
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void PokazHistorie_BrakZamowien_ZwracaKomunikatOBraku()
    {
        var (serwis, _) = UtworzSerwis();

        var (suma, komunikat) = serwis.PokazHistorie();

        suma.Should().Be(0m);
        komunikat.Should().Contain("Brak");
    }

    [Fact]
    public void PokazHistorie_PoDwochZamoiweniach_ZwracaPoprawnaSume()
    {
        var (serwis, _) = UtworzSerwis();
        serwis.ZlozZamowienie("latte", "L");     // 14 zł
        serwis.ZlozZamowienie("espresso", "S");  //  7 zł

        var (suma, _) = serwis.PokazHistorie();

        suma.Should().Be(21m);
    }

    [Fact]
    public void PokazHistorie_KomunikatZawieraWszystkieZamowienia()
    {
        var (serwis, _) = UtworzSerwis();
        serwis.ZlozZamowienie("latte", "L");
        serwis.ZlozZamowienie("mocha", "S");

        var (_, komunikat) = serwis.PokazHistorie();

        komunikat.Should().Contain("latte");
        komunikat.Should().Contain("mocha");
    }

    // ════════════════════════════════════════════════════════════════════════
    // SEKCJA 4: System rabatowy
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ObliczRabat_BrakZamowien_BrakRabatu()
    {
        var (serwis, _) = UtworzSerwis();

        var (procent, rabat, doZaplaty) = serwis.ObliczRabat();

        procent.Should().Be(0m);
        rabat.Should().Be(0m);
        doZaplaty.Should().Be(0m);
    }

    [Fact]
    public void ObliczRabat_DwaZamowienia_Rabat5Procent()
    {
        var (serwis, _) = UtworzSerwis();
        serwis.ZlozZamowienie("latte", "L");     // 14 zł
        serwis.ZlozZamowienie("latte", "L");     // 14 zł  → suma 28 zł

        var (procent, rabat, doZaplaty) = serwis.ObliczRabat();

        procent.Should().Be(5m);
        rabat.Should().Be(1.4m);    // 5% z 28
        doZaplaty.Should().Be(26.6m);
    }

    [Fact]
    public void ObliczRabat_TrzyZamowienia_Rabat10Procent()
    {
        var (serwis, _) = UtworzSerwis();
        serwis.ZlozZamowienie("espresso", "S"); // 7
        serwis.ZlozZamowienie("espresso", "S"); // 7
        serwis.ZlozZamowienie("espresso", "S"); // 7 → suma 21

        var (procent, rabat, _) = serwis.ObliczRabat();

        procent.Should().Be(10m);
        rabat.Should().Be(2.1m); // 10% z 21
    }

    [Fact]
    public void ObliczRabat_PiecZamowien_Rabat15Procent()
    {
        var (serwis, _) = UtworzSerwis();
        for (int i = 0; i < 5; i++)
            serwis.ZlozZamowienie("espresso", "S"); // 5 × 7 = 35 zł

        var (procent, rabat, doZaplaty) = serwis.ObliczRabat();

        procent.Should().Be(15m);
        rabat.Should().Be(5.25m);    // 15% z 35
        doZaplaty.Should().Be(29.75m);
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(2, 5)]
    [InlineData(3, 10)]
    [InlineData(4, 10)]
    [InlineData(5, 15)]
    [InlineData(6, 15)]
    public void ObliczRabat_ProgiRabatoweSaPoprawne(int liczbaZamowien, decimal oczekiwanyProcent)
    {
        var (serwis, _) = UtworzSerwis();
        for (int i = 0; i < liczbaZamowien; i++)
            serwis.ZlozZamowienie("espresso", "S");

        var (procent, _, _) = serwis.ObliczRabat();

        procent.Should().Be(oczekiwanyProcent);
    }

    // ════════════════════════════════════════════════════════════════════════
    // SEKCJA 5: Integracja — scenariusze end-to-end
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Scenariusz_KlientZamawia_PytaORachunekIRabat()
    {
        // Arrange
        var (serwis, _) = UtworzSerwis();

        // Act — klient składa 3 zamówienia
        serwis.ZlozZamowienie("latte",     "L"); // 14 zł
        serwis.ZlozZamowienie("cappuccino","M"); // 12 zł
        serwis.ZlozZamowienie("espresso",  "S"); //  7 zł

        var (suma, _)           = serwis.PokazHistorie();
        var (procent, rabat, _) = serwis.ObliczRabat();

        // Assert
        suma.Should().Be(33m);
        procent.Should().Be(10m); // 3 zamówienia → 10%
        rabat.Should().Be(3.3m);
    }

    [Fact]
    public void Scenariusz_ZamowienieNiedostepnejKawy_NieZmieniaStanuSerwisu()
    {
        var (serwis, _) = UtworzSerwis();
        serwis.ZlozZamowienie("latte", "L");

        // Błędne zamówienie
        serwis.ZlozZamowienie("zielona herbata", "L");

        // Serwis powinien mieć tylko 1 zamówienie
        serwis.Zamowienia.Should().HaveCount(1);
    }

    [Fact]
    public void Scenariusz_PelnaDroga_OdZamowieniaDoPlikuNaDysku()
    {
        // Arrange — tymczasowy plik
        var tmpFile = Path.GetTempFileName();
        try
        {
            var repo   = new FileZamowieniaRepository(tmpFile);
            var serwis = new KawiarniaService(repo, () => new DateTime(2025, 6, 13, 10, 0, 0));

            // Act
            serwis.ZlozZamowienie("mocha", "M");
            serwis.ZlozZamowienie("latte", "L");

            // Assert — plik istnieje i zawiera dane
            var zawartosc = File.ReadAllText(tmpFile);
            zawartosc.Should().Contain("mocha");
            zawartosc.Should().Contain("latte");
            zawartosc.Should().Contain("14,00");
        }
        finally
        {
            File.Delete(tmpFile);
        }
    }
}
