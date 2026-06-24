// ChatClientMockTests.cs
// Testy z mockowaniem IChatClient (Microsoft.Extensions.AI).
// Pokazuje jak testować integrację z AI bez wysyłania prawdziwych zapytań.

using FluentAssertions;
using Microsoft.Extensions.AI;
using Moq;
using Xunit;

namespace KawaRAG.Tests;

public class ChatClientMockTests
{
    // ── Helper: tworzy mock IChatClient zwracający podany tekst ─────────────

    private static Mock<IChatClient> UtworzMockAI(string odpowiedz)
    {
        var mock = new Mock<IChatClient>();

        var chatResponse = new ChatResponse(
            new ChatMessage(ChatRole.Assistant, odpowiedz));

        mock.Setup(c => c.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(chatResponse);

        // Metadata wymagana przez interfejs
        mock.Setup(c => c.GetService(It.IsAny<Type>(), It.IsAny<object?>()))
            .Returns((object?)null);

        return mock;
    }

    // ── IChatClient zwraca odpowiedź ─────────────────────────────────────────

    [Fact]
    public async Task ChatClient_PytanieOMenu_ZwracaOdpowiedz()
    {
        var mockAI  = UtworzMockAI("Mamy espresso, latte i cappuccino.");
        var client  = mockAI.Object;

        var historia = new List<ChatMessage>
        {
            new(ChatRole.User, "Co macie w menu?")
        };

        var response = await client.GetResponseAsync(historia);

        response.Text.Should().Contain("espresso");
    }

    [Fact]
    public async Task ChatClient_ZostajePytany_HistoriaPrzekazanaPrawidlowo()
    {
        var mockAI = UtworzMockAI("Poproszę szczegółów.");

        var historia = new List<ChatMessage>
        {
            new(ChatRole.System, "Jesteś barista."),
            new(ChatRole.User,   "Jakie macie kawy?")
        };

        await mockAI.Object.GetResponseAsync(historia);

        // Verify: mock otrzymał dokładnie 2 wiadomości
        mockAI.Verify(c => c.GetResponseAsync(
            It.Is<IEnumerable<ChatMessage>>(msgs => msgs.Count() == 2),
            It.IsAny<ChatOptions?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ChatClient_NullOdpowiedz_ObslugiwanaBezWyjatku()
    {
        var mockAI = UtworzMockAI("");

        var historia = new List<ChatMessage> { new(ChatRole.User, "Cześć") };
        var response = await mockAI.Object.GetResponseAsync(historia);

        // Pusta odpowiedź nie powinna rzucać wyjątku
        var text = response.Text ?? "";
        text.Should().BeEmpty();
    }

    // ── Weryfikacja systemu promptu ──────────────────────────────────────────

    [Fact]
    public async Task ChatClient_SystemPrompt_ZawieraNazweKawiarni()
    {
        var mockAI         = UtworzMockAI("Hej!");
        const string prompt = "Jesteś barista w kawiarni \"Bit & Bean\".";

        var historia = new List<ChatMessage>
        {
            new(ChatRole.System, prompt),
            new(ChatRole.User,   "Cześć")
        };

        await mockAI.Object.GetResponseAsync(historia);

        // Verify: system prompt zawiera nazwę kawiarni
        mockAI.Verify(c => c.GetResponseAsync(
            It.Is<IEnumerable<ChatMessage>>(msgs =>
                msgs.Any(m => m.Role == ChatRole.System &&
                              m.Text != null &&
                              m.Text.Contains("Bit & Bean"))),
            It.IsAny<ChatOptions?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── Symulacja błędu sieci ────────────────────────────────────────────────

    [Fact]
    public async Task ChatClient_BladSieci_RzucaWyjatek()
    {
        var mockAI = new Mock<IChatClient>();
        mockAI.Setup(c => c.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Brak połączenia"));

        var historia = new List<ChatMessage> { new(ChatRole.User, "Hej") };

        Func<Task> act = async () => await mockAI.Object.GetResponseAsync(historia);

        await act.Should().ThrowAsync<HttpRequestException>()
            .WithMessage("*Brak połączenia*");
    }

    // ── Wielokrotne wywołania ────────────────────────────────────────────────

    [Fact]
    public async Task ChatClient_WielokrotneWywolania_ZachowujeHistorie()
    {
        var mockAI   = UtworzMockAI("Rozumiem.");
        var historia = new List<ChatMessage>
        {
            new(ChatRole.System, "Jesteś barista.")
        };

        // Symulacja 3 tur rozmowy
        var pytania = new[] { "Cześć", "Co macie?", "Poproszę latte L" };
        foreach (var pytanie in pytania)
        {
            historia.Add(new ChatMessage(ChatRole.User, pytanie));
            var resp = await mockAI.Object.GetResponseAsync(historia);
            historia.Add(new ChatMessage(ChatRole.Assistant, resp.Text ?? ""));
        }

        // Po 3 turach historia powinna mieć: 1 system + 3×(user+assistant) = 7
        historia.Should().HaveCount(7);
    }
}
