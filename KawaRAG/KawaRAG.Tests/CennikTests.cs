// CennikTests.cs
// Testy jednostkowe cennika i modelu Zamowienie.
using Xunit;
using FluentAssertions;

namespace KawaRAG.Tests;

public class CennikTests
{
    // ── Menu zawiera oczekiwane kawy ─────────────────────────────────────────

    [Theory]
    [InlineData("espresso")]
    [InlineData("americano")]
    [InlineData("latte")]
    [InlineData("cappuccino")]
    [InlineData("flat white")]
    [InlineData("mocha")]
    [InlineData("cold brew")]
    public void Menu_ZawieraWszystkieKawy(string kawa)
    {
        Cennik.Menu.Should().ContainKey(kawa);
    }

    // ── Każda kawa ma trzy rozmiary ──────────────────────────────────────────

    [Theory]
    [InlineData("latte")]
    [InlineData("espresso")]
    [InlineData("mocha")]
    public void Menu_KazdeKawaMaprzeRozmiary_S_M_L(string kawa)
    {
        var rozmiary = Cennik.Menu[kawa];
        rozmiary.Should().ContainKeys("S", "M", "L");
    }

    // ── Ceny rosną wraz z rozmiarem ──────────────────────────────────────────

    [Theory]
    [InlineData("latte",      10, 12, 14)]
    [InlineData("espresso",    7,  8,  9)]
    [InlineData("cappuccino", 10, 12, 14)]
    [InlineData("mocha",      12, 14, 16)]
    [InlineData("cold brew",  13, 15, 17)]
    public void Menu_CenyRosnaPrzyRozmiarzeSML(string kawa, decimal s, decimal m, decimal l)
    {
        var ceny = Cennik.Menu[kawa];
        ceny["S"].Should().Be(s);
        ceny["M"].Should().Be(m);
        ceny["L"].Should().Be(l);
        ceny["S"].Should().BeLessThan(ceny["M"]);
        ceny["M"].Should().BeLessThan(ceny["L"]);
    }

    // ── Liczba pozycji w menu ────────────────────────────────────────────────

    [Fact]
    public void Menu_ZawieraSiemPozycji()
    {
        Cennik.Menu.Should().HaveCount(7);
    }

    // ── Menu jest case-insensitive ───────────────────────────────────────────

    [Theory]
    [InlineData("LATTE")]
    [InlineData("Latte")]
    [InlineData("LaTTe")]
    public void Menu_CaseInsensitive(string kawa)
    {
        Cennik.Menu.Should().ContainKey(kawa);
    }

    // ── Model Zamowienie — ToString ──────────────────────────────────────────

    [Fact]
    public void Zamowienie_ToString_ZawieraPotrzebneInformacje()
    {
        var czas = new DateTime(2025, 6, 13, 14, 30, 0);
        var z    = new Zamowienie(1, "latte", "L", 14m, czas);

        var str = z.ToString();

        str.Should().Contain("2025-06-13");
        str.Should().Contain("#1");
        str.Should().Contain("latte");
        str.Should().Contain("L");
        str.Should().Contain("14,00");
    }

    // ── Model Zamowienie — immutowalność (record) ────────────────────────────

    [Fact]
    public void Zamowienie_DwaIdentyczneRekordy_SaRowne()
    {
        var czas = new DateTime(2025, 1, 1);
        var z1   = new Zamowienie(1, "latte", "L", 14m, czas);
        var z2   = new Zamowienie(1, "latte", "L", 14m, czas);

        z1.Should().Be(z2);
    }

    [Fact]
    public void Zamowienie_RozneId_NieSaRowne()
    {
        var czas = new DateTime(2025, 1, 1);
        var z1   = new Zamowienie(1, "latte", "L", 14m, czas);
        var z2   = new Zamowienie(2, "latte", "L", 14m, czas);

        z1.Should().NotBe(z2);
    }
}
