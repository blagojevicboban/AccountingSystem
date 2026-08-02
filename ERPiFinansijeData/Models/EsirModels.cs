using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ERPiFinansijeData.Models;

/// <summary>
/// Status fiskalizacije u e-Fiskalizacija (ESIR / PFR) sistemu.
/// </summary>
public enum FiskalniStatus
: int
{
    NijeFiskalizovan = 0,
    Fiskalizovan = 1,
    Greska = 2,
    Storniran = 3,
    /// <summary>
    /// Račun je "izdat" kroz lokalni simulator, BEZ stvarne fiskalizacije u PFR-u.
    /// Nema pravnu vrednost - koristi se isključivo za testiranje i obuku.
    /// </summary>
    Simulacija = 4
}

/// <summary>
/// Log evidencija svih izdate i verifikovane fiskalne račune u PFR sistemu.
/// </summary>
public class FiskalniRacunLog
{
    [Key]
    public int FiskalniRacunLogId { get; set; }

    public int? RacunOtpremnicaId { get; set; }

    [Required]
    [MaxLength(100)]
    public string InvoiceNumber { get; set; } = string.Empty; // Npr. 88372-91823-1284

    [MaxLength(100)]
    public string InvoiceCounter { get; set; } = string.Empty; // Npr. 12/1284S

    public DateTime SdcDateTime { get; set; }

    [MaxLength(100)]
    public string InvoiceType { get; set; } = "Normal"; // Normal ili Refund

    [MaxLength(100)]
    public string TransactionType { get; set; } = "Sale"; // Sale ili Refund

    [Column(TypeName = "decimal(18, 2)")]
    public decimal TotalAmount { get; set; }

    [MaxLength(50)]
    public string PaymentType { get; set; } = "Cash"; // Cash, Card, WireTransfer...

    [MaxLength(1000)]
    public string QrCodeData { get; set; } = string.Empty;

    [MaxLength(500)]
    public string VerificationUrl { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Kasir { get; set; } = string.Empty;

    public string RawJsonResponse { get; set; } = string.Empty;
}

public class PfrPostavke
{
    public string PfrUrl { get; set; } = "http://localhost:8443";
    public string PacKod { get; set; } = "123456";
    public string Kasir { get; set; } = "Glavni Kasir";

    /// <summary>
    /// Kada je true, rad bez priključenog PFR-a generiše SIMULIRANI račun (za testiranje i obuku).
    /// Kada je false (podrazumevano), svaki neuspeh komunikacije sa PFR-om se prijavljuje kao greška
    /// i račun se NE označava kao fiskalizovan.
    /// </summary>
    public bool SimulatorMod { get; set; }
}

// PFR REST API Zahtev
public class PfrZahtev
{
    [JsonPropertyName("invoiceType")]
    public string InvoiceType { get; set; } = "Normal";

    [JsonPropertyName("transactionType")]
    public string TransactionType { get; set; } = "Sale";

    [JsonPropertyName("cashier")]
    public string Cashier { get; set; } = "Kasir 1";

    [JsonPropertyName("buyerId")]
    public string? BuyerId { get; set; }

    [JsonPropertyName("items")]
    public List<PfrZahtevStavka> Items { get; set; } = new();

    [JsonPropertyName("payment")]
    public List<PfrZahtevPlacanje> Payment { get; set; } = new();
}

public class PfrZahtevStavka
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("quantity")]
    public decimal Quantity { get; set; }

    [JsonPropertyName("unitPrice")]
    public decimal UnitPrice { get; set; }

    [JsonPropertyName("totalAmount")]
    public decimal TotalAmount { get; set; }

    [JsonPropertyName("labels")]
    public List<string> Labels { get; set; } = new() { "Đ" }; // Đ = 20%, E = 10%, А = 0%
}

public class PfrZahtevPlacanje
{
    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("paymentType")]
    public string PaymentType { get; set; } = "Cash"; // Cash, Card, WireTransfer, Voucher, Other
}

// PFR REST API Odgovor
public class PfrOdgovor
{
    [JsonPropertyName("invoiceNumber")]
    public string InvoiceNumber { get; set; } = string.Empty;

    [JsonPropertyName("invoiceCounter")]
    public string InvoiceCounter { get; set; } = string.Empty;

    [JsonPropertyName("sdcDateTime")]
    public DateTime SdcDateTime { get; set; }

    [JsonPropertyName("totalAmount")]
    public decimal TotalAmount { get; set; }

    [JsonPropertyName("verificationUrl")]
    public string VerificationUrl { get; set; } = string.Empty;

    [JsonPropertyName("journal")]
    public string Journal { get; set; } = string.Empty;
}
