using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace ERPiFinansijeData.Services;

public class AccountingWebServer
{
    private static HttpListener? _listener;
    private static CancellationTokenSource? _cts;
    private static bool _isRunning;
    private static string _dbPath = "";

    public static bool IsRunning => _isRunning;
    public static int Port { get; private set; } = 5050;

    /// <summary>
    /// Pristupni token, generisan nasumično pri svakom pokretanju servera.
    /// Svi /api/ pozivi ga moraju poslati kao "Authorization: Bearer &lt;token&gt;"
    /// ili kao ?token= parametar (za otvaranje dashboard-a u pretraživaču).
    /// </summary>
    public static string AccessToken { get; private set; } = "";

    public static string DashboardUrl => $"http://localhost:{Port}/?token={AccessToken}";

    public static void Start(string dbPath, int port = 5050)
    {
        if (_isRunning) return;

        _dbPath = dbPath;
        Port = port;
        AccessToken = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(24));
        _cts = new CancellationTokenSource();

        try
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://localhost:{Port}/");
            _listener.Prefixes.Add($"http://127.0.0.1:{Port}/");
            _listener.Start();
            _isRunning = true;

            Task.Run(() => ListenLoop(_cts.Token));
        }
        catch (Exception ex)
        {
            _isRunning = false;
            Serilog.Log.Error(ex, "Greška pri pokretanju Web Servera");
        }
    }

    public static void Stop()
    {
        if (!_isRunning) return;

        try
        {
            _cts?.Cancel();
            _listener?.Stop();
            _listener?.Close();
        }
        catch { }
        finally
        {
            _isRunning = false;
        }
    }

    private static async Task ListenLoop(CancellationToken token)
    {
        while (_listener != null && _listener.IsListening && !token.IsCancellationRequested)
        {
            try
            {
                var context = await _listener.GetContextAsync();
                _ = Task.Run(() => ObradiZahtev(context));
            }
            catch
            {
                if (token.IsCancellationRequested) break;
            }
        }
    }

    private static async Task ObradiZahtev(HttpListenerContext ctx)
    {
        var req = ctx.Request;
        var res = ctx.Response;

        // Namerno BEZ "Access-Control-Allow-Origin: *" - server sluša na localhost-u,
        // pa bi wildcard CORS dozvolio bilo kom sajtu u pretraživaču da pročita
        // finansije firme i žiro-račune partnera dok aplikacija radi.
        res.Headers.Add("Access-Control-Allow-Origin", $"http://localhost:{Port}");
        res.Headers.Add("Access-Control-Allow-Headers", "Authorization, Content-Type");
        res.Headers.Add("X-Content-Type-Options", "nosniff");

        try
        {
            string path = req.Url?.AbsolutePath.ToLowerInvariant() ?? "/";

            if (req.HttpMethod == "OPTIONS")
            {
                res.StatusCode = 204;
                res.Close();
                return;
            }

            if (!ZahtevJeAutorizovan(req))
            {
                res.StatusCode = 401;
                byte[] denied = Encoding.UTF8.GetBytes("{\"error\":\"Neautorizovan pristup.\"}");
                res.ContentType = "application/json; charset=utf-8";
                res.ContentLength64 = denied.Length;
                await res.OutputStream.WriteAsync(denied, 0, denied.Length);
                res.Close();
                return;
            }

            if (path == "/api/status")
            {
                await VratiJson(res, new { Status = "Active", Version = "1.0.43", Service = "ERPiFinansije REST API", ServerTime = DateTime.Now });
            }
            else if (path == "/api/dashboard")
            {
                var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={_dbPath}").Options;
                using var db = new AccountingDbContext(options);

                int godina = DateTime.Today.Year;
                var stavke = await db.StavkeNaloga.Include(s => s.Nalog).Where(s => s.Nalog != null && s.Nalog.IsKnjizen && s.Nalog.DatumNaloga.Year == godina).ToListAsync();

                decimal prihodi = stavke.Where(s => s.BrojKonta.StartsWith("6")).Sum(s => s.Potrazuje - s.Duguje);
                decimal rashodi = stavke.Where(s => s.BrojKonta.StartsWith("5")).Sum(s => s.Duguje - s.Potrazuje);
                int brojNaloga = await db.Nalozi.CountAsync(n => n.IsKnjizen && n.DatumNaloga.Year == godina);
                int brojPartnera = await db.Partneri.CountAsync();
                int brojArtikala = await db.Artikli.CountAsync();
                var firma = await db.Firme.FirstOrDefaultAsync();

                await VratiJson(res, new
                {
                    Firma = firma?.Naziv ?? "Moja Firma D.O.O.",
                    Pib = firma?.Pib ?? "-",
                    Godina = godina,
                    UkupnoPrihodi = prihodi,
                    UkupnoRashodi = rashodi,
                    NetoDobit = (prihodi - rashodi),
                    BrojNaloga = brojNaloga,
                    BrojPartnera = brojPartnera,
                    BrojArtikala = brojArtikala
                });
            }
            else if (path == "/api/partneri")
            {
                var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={_dbPath}").Options;
                using var db = new AccountingDbContext(options);
                var partneri = await db.Partneri.Take(50).Select(p => new { p.PartnerId, p.SifraPartnera, p.Naziv, p.Pib, p.ZiroRacun }).ToListAsync();

                await VratiJson(res, partneri);
            }
            else
            {
                // Vraćanje Responzivne HTML5 Web Dashboard aplikacije za mobilne telefone i web pregledače
                string html = GenerisiHtmlDashboard(AccessToken);
                byte[] buffer = Encoding.UTF8.GetBytes(html);
                res.ContentType = "text/html; charset=utf-8";
                res.ContentLength64 = buffer.Length;
                await res.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                res.Close();
            }
        }
        catch (Exception ex)
        {
            // Detalji greške (putanja baze, šema) ostaju lokalno, ne idu klijentu.
            Serilog.Log.Error(ex, "Greška pri obradi HTTP zahteva {Putanja}", req.Url?.AbsolutePath);
            try
            {
                res.StatusCode = 500;
                byte[] errBuffer = Encoding.UTF8.GetBytes("Greška na serveru.");
                await res.OutputStream.WriteAsync(errBuffer, 0, errBuffer.Length);
                res.Close();
            }
            catch { }
        }
    }

    /// <summary>
    /// Prihvata token iz "Authorization: Bearer &lt;token&gt;" zaglavlja ili iz ?token= parametra.
    /// Poređenje je vremenski konstantno da se token ne može pogoditi merenjem odziva.
    /// </summary>
    private static bool ZahtevJeAutorizovan(HttpListenerRequest req)
    {
        if (string.IsNullOrEmpty(AccessToken)) return false;

        string? dostavljen = null;

        string? auth = req.Headers["Authorization"];
        if (!string.IsNullOrWhiteSpace(auth) && auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            dostavljen = auth.Substring(7).Trim();

        if (string.IsNullOrEmpty(dostavljen))
            dostavljen = req.QueryString["token"];

        if (string.IsNullOrEmpty(dostavljen)) return false;

        return System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(dostavljen),
            Encoding.UTF8.GetBytes(AccessToken));
    }

    private static async Task VratiJson(HttpListenerResponse res, object obj)
    {
        string json = JsonSerializer.Serialize(obj);
        byte[] buffer = Encoding.UTF8.GetBytes(json);
        res.ContentType = "application/json; charset=utf-8";
        res.ContentLength64 = buffer.Length;
        await res.OutputStream.WriteAsync(buffer, 0, buffer.Length);
        res.Close();
    }

    private static string GenerisiHtmlDashboard(string token)
    {
        return @"<!DOCTYPE html>
<html lang='sr'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>ERPiFinansije — Mobile & Web Cloud Dashboard</title>
    <link href='https://cdn.jsdelivr.net/npm/tailwindcss@2.2.19/dist/tailwind.min.css' rel='stylesheet'>
</head>
<body class='bg-slate-100 min-h-screen text-slate-800 font-sans p-4 md:p-8'>
    <div class='max-w-4xl mx-auto'>
        <header class='flex justify-between items-center mb-6 bg-white p-6 rounded-xl shadow-sm border border-slate-200'>
            <div>
                <h1 class='text-2xl font-bold text-indigo-900'>🏛️ ERPiFinansije ERP</h1>
                <p id='firmaNaziv' class='text-slate-500 text-sm mt-1'>Učitavanje podataka o firmi...</p>
            </div>
            <span class='bg-emerald-100 text-emerald-800 text-xs font-semibold px-3 py-1.5 rounded-full'>● Live REST API (Port 5050)</span>
        </header>

        <div class='grid grid-cols-1 md:grid-cols-3 gap-6 mb-8'>
            <div class='bg-white p-6 rounded-xl shadow-sm border border-slate-200'>
                <p class='text-slate-400 text-xs font-semibold uppercase'>Prihodi (Godina)</p>
                <h2 id='txtPrihodi' class='text-2xl font-bold text-emerald-600 mt-2'>0,00 RSD</h2>
            </div>
            <div class='bg-white p-6 rounded-xl shadow-sm border border-slate-200'>
                <p class='text-slate-400 text-xs font-semibold uppercase'>Rashodi (Godina)</p>
                <h2 id='txtRashodi' class='text-2xl font-bold text-rose-600 mt-2'>0,00 RSD</h2>
            </div>
            <div class='bg-white p-6 rounded-xl shadow-sm border border-slate-200'>
                <p class='text-slate-400 text-xs font-semibold uppercase'>Neto Dobitak</p>
                <h2 id='txtDobit' class='text-2xl font-bold text-indigo-600 mt-2'>0,00 RSD</h2>
            </div>
        </div>

        <div class='bg-white p-6 rounded-xl shadow-sm border border-slate-200 mb-8'>
            <h3 class='text-lg font-bold text-slate-800 mb-4'>📊 Pokazatelji poslovanja</h3>
            <div class='grid grid-cols-3 gap-4 text-center'>
                <div class='p-4 bg-slate-50 rounded-lg'>
                    <p class='text-xs text-slate-500'>Broj Naloga</p>
                    <p id='txtBrojNaloga' class='text-xl font-bold text-slate-800 mt-1'>0</p>
                </div>
                <div class='p-4 bg-slate-50 rounded-lg'>
                    <p class='text-xs text-slate-500'>Partneri</p>
                    <p id='txtBrojPartnera' class='text-xl font-bold text-slate-800 mt-1'>0</p>
                </div>
                <div class='p-4 bg-slate-50 rounded-lg'>
                    <p class='text-xs text-slate-500'>Artikli na zalihi</p>
                    <p id='txtBrojArtikala' class='text-xl font-bold text-slate-800 mt-1'>0</p>
                </div>
            </div>
        </div>
    </div>

    <script>
        const API_TOKEN = '" + token + @"';
        async function loadDashboard() {
            try {
                const res = await fetch('/api/dashboard', { headers: { 'Authorization': 'Bearer ' + API_TOKEN } });
                if (!res.ok) { console.error('Neautorizovan pristup ili greška: ' + res.status); return; }
                const data = await res.json();

                document.getElementById('firmaNaziv').innerText = data.Firma + ' (PIB: ' + data.Pib + ') — Period: ' + data.Godina;
                document.getElementById('txtPrihodi').innerText = data.UkupnoPrihodi.toLocaleString('sr-RS', {minimumFractionDigits: 2}) + ' RSD';
                document.getElementById('txtRashodi').innerText = data.UkupnoRashodi.toLocaleString('sr-RS', {minimumFractionDigits: 2}) + ' RSD';
                document.getElementById('txtDobit').innerText = data.NetoDobit.toLocaleString('sr-RS', {minimumFractionDigits: 2}) + ' RSD';

                document.getElementById('txtBrojNaloga').innerText = data.BrojNaloga;
                document.getElementById('txtBrojPartnera').innerText = data.BrojPartnera;
                document.getElementById('txtBrojArtikala').innerText = data.BrojArtikala;
            } catch (err) {
                console.error(err);
            }
        }
        loadDashboard();
        setInterval(loadDashboard, 10000);
    </script>
</body>
</html>";
    }
}
