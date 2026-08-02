using Microsoft.EntityFrameworkCore;

namespace ERPiFinansijeData.Services;

/// <summary>Šifarnik opisa promena (PROMENE.DBF) je po firmi, pa se učitava iz tekuće baze — vidi Promena model.</summary>
public class PromenaService
{
    private readonly AccountingDbContext _db;

    public PromenaService(AccountingDbContext db)
    {
        _db = db;
    }

    public async Task<Dictionary<int, string>> GetMapAsync()
        => await _db.Promene.ToDictionaryAsync(p => p.Sifra, p => p.Opis);
}
