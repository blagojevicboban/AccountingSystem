using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ERPiFinansijeData.Services;

public class SefApiResponse<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
    public int StatusCode { get; set; }
}

public class SefInvoiceStatusResponse
{
    public long SalesInvoiceId { get; set; }
    public string? Status { get; set; }
    public string? Comment { get; set; }
    public DateTime? StatusDate { get; set; }
}

public class SefUlaznaFakturaSummary
{
    public long PurchaseInvoiceId { get; set; }
    public string? InvoiceNumber { get; set; }
    public string? SupplierPib { get; set; }
    public string? SupplierName { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime IssueDate { get; set; }
    public string? Status { get; set; }
}

public class SefApiClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _baseUrl;

    public SefApiClient(string apiKey, string environment = "Demo", HttpClient? httpClient = null)
    {
        _apiKey = apiKey;
        _httpClient = httpClient ?? new HttpClient();

        string envLower = (environment ?? "Demo").Trim().ToLowerInvariant();
        _baseUrl = envLower == "production" || envLower == "produkcija"
            ? "https://efaktura.mfin.gov.rs/api/publicApi"
            : "https://demoefaktura.mfin.gov.rs/api/publicApi";

        if (!_httpClient.DefaultRequestHeaders.Contains("ApiKey"))
        {
            _httpClient.DefaultRequestHeaders.Add("ApiKey", _apiKey);
        }
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    /// <summary>
    /// Testira konekciju ka SEF API-ju slanjem jednostavnog upita.
    /// </summary>
    public async Task<SefApiResponse<bool>> TestConnectionAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                return new SefApiResponse<bool>
                {
                    Success = false,
                    Message = "API ključ za SEF nije podešen u postavkama firme."
                };
            }

            var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/company/check");
            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                return new SefApiResponse<bool>
                {
                    Success = true,
                    Data = true,
                    Message = "Uspešna konekcija sa SEF API portalom!"
                };
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized || response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                return new SefApiResponse<bool>
                {
                    Success = false,
                    StatusCode = (int)response.StatusCode,
                    Message = "Neuspešna autorizacija (401/403). Proverite da li je uneti API ključ ispravan."
                };
            }
            else
            {
                return new SefApiResponse<bool>
                {
                    Success = true, // Ako dobijemo odgovor od SEF-a sa npr. 200 ili 404, konekcija prolazi
                    Data = true,
                    Message = $"Konekcija sa SEF-om uspostavljena (HTTP {(int)response.StatusCode})."
                };
            }
        }
        catch (Exception ex)
        {
            return new SefApiResponse<bool>
            {
                Success = false,
                Message = $"Greška pri povezivanju sa SEF-om: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Šalje UBL 2.1 XML izlaznu e-fakturu na SEF.
    /// </summary>
    public async Task<SefApiResponse<long>> PosaljiFakturuUblAsync(string ublXml)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                return new SefApiResponse<long> { Success = false, Message = "API ključ nije podešen." };
            }

            string base64Xml = Convert.ToBase64String(Encoding.UTF8.GetBytes(ublXml));
            var payload = new
            {
                ubl = base64Xml
            };

            string jsonBody = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/sales-invoice/ubl?sendToCir=false", content);
            string responseText = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                long salesInvoiceId = 0;
                try
                {
                    using var doc = JsonDocument.Parse(responseText);
                    if (doc.RootElement.TryGetProperty("salesInvoiceId", out var idProp))
                    {
                        salesInvoiceId = idProp.GetInt64();
                    }
                    else if (doc.RootElement.ValueKind == JsonValueKind.Number)
                    {
                        salesInvoiceId = doc.RootElement.GetInt64();
                    }
                }
                catch
                {
                    // Fallback
                }

                return new SefApiResponse<long>
                {
                    Success = true,
                    Data = salesInvoiceId,
                    StatusCode = (int)response.StatusCode,
                    Message = "E-Faktura je uspešno poslata na SEF portal!"
                };
            }
            else
            {
                return new SefApiResponse<long>
                {
                    Success = false,
                    StatusCode = (int)response.StatusCode,
                    Message = $"SEF API Vraćena greška ({(int)response.StatusCode}): {responseText}"
                };
            }
        }
        catch (Exception ex)
        {
            return new SefApiResponse<long>
            {
                Success = false,
                Message = $"Greška pri slanju na SEF: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Proverava trenutni status izlazne e-fakture na SEF-u.
    /// </summary>
    public async Task<SefApiResponse<SefInvoiceStatusResponse>> ProveriStatusFaktureAsync(long sefSalesInvoiceId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/sales-invoice/status?invoiceId={sefSalesInvoiceId}");
            string responseText = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(responseText);
                var root = doc.RootElement;

                var result = new SefInvoiceStatusResponse
                {
                    SalesInvoiceId = sefSalesInvoiceId,
                    Status = root.TryGetProperty("status", out var s) ? s.GetString() : "Unknown",
                    Comment = root.TryGetProperty("comment", out var c) ? c.GetString() : null
                };

                return new SefApiResponse<SefInvoiceStatusResponse>
                {
                    Success = true,
                    Data = result,
                    StatusCode = (int)response.StatusCode
                };
            }
            else
            {
                return new SefApiResponse<SefInvoiceStatusResponse>
                {
                    Success = false,
                    Message = $"Greška pri proveri statusa ({(int)response.StatusCode}): {responseText}"
                };
            }
        }
        catch (Exception ex)
        {
            return new SefApiResponse<SefInvoiceStatusResponse>
            {
                Success = false,
                Message = $"Greška pri komunikaciji sa SEF-om: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Preuzima spisak ulaznih e-faktura dobavljača sa SEF-a.
    /// </summary>
    public async Task<SefApiResponse<List<SefUlaznaFakturaSummary>>> PreuzmiUlazneFaktureAsync(DateTime odDatuma)
    {
        try
        {
            string dateStr = odDatuma.ToString("yyyy-MM-dd");
            var response = await _httpClient.GetAsync($"{_baseUrl}/purchase-invoice/changes?dateFrom={dateStr}");
            string responseText = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var list = new List<SefUlaznaFakturaSummary>();
                using var doc = JsonDocument.Parse(responseText);
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var elem in doc.RootElement.EnumerateArray())
                    {
                        list.Add(new SefUlaznaFakturaSummary
                        {
                            PurchaseInvoiceId = elem.TryGetProperty("purchaseInvoiceId", out var id) ? id.GetInt64() : 0,
                            InvoiceNumber = elem.TryGetProperty("invoiceNumber", out var num) ? num.GetString() : "",
                            SupplierPib = elem.TryGetProperty("supplierPib", out var pib) ? pib.GetString() : "",
                            SupplierName = elem.TryGetProperty("supplierName", out var name) ? name.GetString() : "",
                            TotalAmount = elem.TryGetProperty("totalAmount", out var tot) ? tot.GetDecimal() : 0m,
                            Status = elem.TryGetProperty("status", out var st) ? st.GetString() : "New"
                        });
                    }
                }

                return new SefApiResponse<List<SefUlaznaFakturaSummary>>
                {
                    Success = true,
                    Data = list
                };
            }
            else
            {
                return new SefApiResponse<List<SefUlaznaFakturaSummary>>
                {
                    Success = false,
                    Message = $"Greška pri preuzimanju ulaznih faktura ({(int)response.StatusCode}): {responseText}"
                };
            }
        }
        catch (Exception ex)
        {
            return new SefApiResponse<List<SefUlaznaFakturaSummary>>
            {
                Success = false,
                Message = $"Greška pri preuzimanju ulaznih faktura: {ex.Message}"
            };
        }
    }
}
