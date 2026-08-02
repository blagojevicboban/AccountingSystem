using AccountingData.Services;
using Xunit;

namespace AccountingData.Tests;

/// <summary>
/// Server izlaže promet firme i žiro-račune partnera na localhost-u. Pošto ga
/// pretraživač može dosegnuti sa bilo kog sajta, autorizacija i uska CORS politika
/// su bezbednosne kontrole - ovi testovi sprečavaju da se slučajno vrate unazad.
/// </summary>
public class AccountingWebServerTests : IDisposable
{
    private const int TestPort = 57311;
    private readonly HttpClient _client = new();

    public AccountingWebServerTests()
    {
        AccountingWebServer.Start(Path.Combine(Path.GetTempPath(), "ws_test.db"), TestPort);
    }

    public void Dispose()
    {
        AccountingWebServer.Stop();
        _client.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void Start_GenerisePristupniToken()
    {
        Assert.True(AccountingWebServer.IsRunning);
        Assert.False(string.IsNullOrWhiteSpace(AccountingWebServer.AccessToken));
        Assert.True(AccountingWebServer.AccessToken.Length >= 32);
    }

    [Fact]
    public async Task ApiPoziv_BezTokena_Vraca401()
    {
        var res = await _client.GetAsync($"http://localhost:{TestPort}/api/status");
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task ApiPoziv_SaPogresnimTokenom_Vraca401()
    {
        var req = new HttpRequestMessage(HttpMethod.Get, $"http://localhost:{TestPort}/api/status");
        req.Headers.TryAddWithoutValidation("Authorization", "Bearer pogresan-token");

        var res = await _client.SendAsync(req);
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task ApiPoziv_SaIspravnimTokenom_Prolazi()
    {
        var req = new HttpRequestMessage(HttpMethod.Get, $"http://localhost:{TestPort}/api/status");
        req.Headers.TryAddWithoutValidation("Authorization", $"Bearer {AccountingWebServer.AccessToken}");

        var res = await _client.SendAsync(req);
        Assert.Equal(System.Net.HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task Odgovor_NeSmeImatiWildcardCors()
    {
        var res = await _client.GetAsync($"http://localhost:{TestPort}/api/status");

        var acao = res.Headers.TryGetValues("Access-Control-Allow-Origin", out var v)
            ? string.Join(",", v)
            : "";

        Assert.NotEqual("*", acao);
    }
}
