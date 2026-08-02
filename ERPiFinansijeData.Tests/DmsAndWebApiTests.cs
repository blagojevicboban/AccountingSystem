using ERPiFinansijeData.Models;
using ERPiFinansijeData.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ERPiFinansijeData.Tests;

public class DmsAndWebApiTests
{
    private AccountingDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AccountingDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AccountingDbContext(options);
    }

    [Fact]
    public async Task DmsService_DodajIObrisiPrilog_UspesnoUpravljaDokumentom()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        var service = new DmsService(db);

        string tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, "Test sadrzaj racuna");

        try
        {
            // Act — Dodavanje
            var (success, message, prilog) = await service.DodajPrilogAsync(nalogId: 10, racunId: null, kalkulacijaId: null, tempFile, tipDokumenta: "Ulazni Račun");

            // Assert — Dodavanje
            Assert.True(success);
            Assert.NotNull(prilog);
            Assert.Equal(10, prilog.NalogId);
            Assert.Equal("Ulazni Račun", prilog.TipDokumenta);

            var prilozi = await service.GetPriloziZaNalogAsync(10);
            Assert.Single(prilozi);

            // Act — Brisanje
            var (delSuccess, delMsg) = await service.ObrisiPrilogAsync(prilog.DokumentPrilogId);

            // Assert — Brisanje
            Assert.True(delSuccess);
            var priloziPosle = await service.GetPriloziZaNalogAsync(10);
            Assert.Empty(priloziPosle);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }
}
