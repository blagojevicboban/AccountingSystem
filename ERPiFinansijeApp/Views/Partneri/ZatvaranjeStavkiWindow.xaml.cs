using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using ERPiFinansijeApp.Services;
using ERPiFinansijeData;
using ERPiFinansijeData.Models;
using ERPiFinansijeData.Services;
using Microsoft.EntityFrameworkCore;

namespace ERPiFinansijeApp.Views.Partneri;

public class ZatvaranjeIzborRed : INotifyPropertyChanged
{
    private bool _isSelected;
    private decimal _iznosZaZatvaranje;

    public int StavkaNalogaId { get; set; }
    public DateTime Datum { get; set; }
    public string? BrojDokumenta { get; set; }
    public string? Opis { get; set; }
    public decimal Preostalo { get; set; }

    public bool IsSelected
    {
        get => _isSelected;
        set { _isSelected = value; OnPropertyChanged(); }
    }

    public decimal IznosZaZatvaranje
    {
        get => _iznosZaZatvaranje;
        set { _iznosZaZatvaranje = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public static ZatvaranjeIzborRed IzOtvoreneStavke(OtvorenaStavkaRed s, bool preselektovano)
        => new()
        {
            StavkaNalogaId = s.StavkaNalogaId,
            Datum = s.Datum,
            BrojDokumenta = s.BrojDokumenta,
            Opis = s.Opis,
            Preostalo = s.Preostalo,
            IsSelected = preselektovano,
            IznosZaZatvaranje = s.Preostalo
        };
}

public partial class ZatvaranjeStavkiWindow : Window
{
    private readonly Partner _partner;
    private List<ZatvaranjeIzborRed> _dugujeRedovi = new();
    private List<ZatvaranjeIzborRed> _potrazujeRedovi = new();

    public bool Zatvoreno { get; private set; }

    public ZatvaranjeStavkiWindow(Partner partner, IEnumerable<int>? preselektovaniIds = null)
    {
        InitializeComponent();
        _partner = partner;
        TxtNaslov.Text = $"🔗 Zatvaranje otvorenih stavki — {partner.Naziv}";
        DpDatum.SelectedDate = DateTime.Now;

        LoadStavke(preselektovaniIds?.ToHashSet() ?? new HashSet<int>());
    }

    private async void LoadStavke(HashSet<int> preselektovaniIds)
    {
        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;
            using var db = new AccountingDbContext(options);
            var service = new ZatvaranjeStavkiService(db);

            var otvorene = await service.GetOtvoreneStavkeZaPartneraAsync(_partner.PartnerId, samoOtvorene: true);

            _dugujeRedovi = otvorene.Where(s => s.Strana == "Duguje")
                .Select(s => ZatvaranjeIzborRed.IzOtvoreneStavke(s, preselektovaniIds.Contains(s.StavkaNalogaId)))
                .ToList();
            _potrazujeRedovi = otvorene.Where(s => s.Strana == "Potrazuje")
                .Select(s => ZatvaranjeIzborRed.IzOtvoreneStavke(s, preselektovaniIds.Contains(s.StavkaNalogaId)))
                .ToList();

            foreach (var red in _dugujeRedovi.Concat(_potrazujeRedovi))
            {
                red.PropertyChanged += (_, _) => AzurirajZbirove();
            }

            DgDuguje.ItemsSource = _dugujeRedovi;
            DgPotrazuje.ItemsSource = _potrazujeRedovi;
            AzurirajZbirove();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju otvorenih stavki: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void AzurirajZbirove()
    {
        decimal zbirDuguje = _dugujeRedovi.Where(r => r.IsSelected).Sum(r => r.IznosZaZatvaranje);
        decimal zbirPotrazuje = _potrazujeRedovi.Where(r => r.IsSelected).Sum(r => r.IznosZaZatvaranje);
        TxtZbirDuguje.Text = zbirDuguje.ToString("N2");
        TxtZbirPotrazuje.Text = zbirPotrazuje.ToString("N2");
    }

    private async void BtnPotvrdi_Click(object sender, RoutedEventArgs e)
    {
        var izabraneDuguje = _dugujeRedovi.Where(r => r.IsSelected && r.IznosZaZatvaranje > 0).ToList();
        var izabranePotrazuje = _potrazujeRedovi.Where(r => r.IsSelected && r.IznosZaZatvaranje > 0).ToList();

        if (izabraneDuguje.Count == 0 || izabranePotrazuje.Count == 0)
        {
            MessageBox.Show("Izaberite bar jednu stavku sa obe strane (duguje i potražuje).", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        decimal zbirDuguje = izabraneDuguje.Sum(r => r.IznosZaZatvaranje);
        decimal zbirPotrazuje = izabranePotrazuje.Sum(r => r.IznosZaZatvaranje);
        if (Math.Abs(zbirDuguje - zbirPotrazuje) > 0.01m)
        {
            MessageBox.Show($"Zbir duguje ({zbirDuguje:N2}) mora biti jednak zbiru potražuje ({zbirPotrazuje:N2}).", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (DpDatum.SelectedDate is not DateTime datum)
        {
            MessageBox.Show("Izaberite datum zatvaranja.", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        string vrsta = (CmbVrstaZatvaranja.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content as string ?? "Rucno";

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;
            using var db = new AccountingDbContext(options);
            var service = new ZatvaranjeStavkiService(db);

            await service.ZatvoriGrupnoAsync(
                izabraneDuguje.Select(r => (r.StavkaNalogaId, r.IznosZaZatvaranje)).ToList(),
                izabranePotrazuje.Select(r => (r.StavkaNalogaId, r.IznosZaZatvaranje)).ToList(),
                datum, vrsta,
                string.IsNullOrWhiteSpace(TxtNapomena.Text) ? null : TxtNapomena.Text.Trim(),
                AppSession.TrenutniKorisnik?.KorisnikId, AppSession.TrenutniKorisnik?.KorisnickoIme);

            Zatvoreno = true;
            DialogResult = true;
            Close();
        }
        catch (InvalidOperationException ex)
        {
            MessageBox.Show(ex.Message, "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri zatvaranju stavki: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
