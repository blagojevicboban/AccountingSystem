using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ERPiFinansijeApp.Views.Pomoc;
using Microsoft.EntityFrameworkCore;
using ERPiFinansijeData;
using ERPiFinansijeData.Models;

namespace ERPiFinansijeApp.Views.Trgovina;

public partial class NivelacijaEditWindow : Window
{
    private readonly AccountingDbContext _db;
    public NivelacijaCena Nivelacija { get; private set; }
    public ObservableCollection<NivelacijaStavka> StavkeCollection { get; set; }

    public NivelacijaEditWindow(AccountingDbContext db, NivelacijaCena? nivelacija = null)
    {
        InitializeComponent();
        _db = db;

        Nivelacija = nivelacija ?? new NivelacijaCena
        {
            BrojNivelacije = (db.NivelacijeCena.Select(n => (int?)n.BrojNivelacije).Max() ?? 0) + 1,
            DatumNivelacije = DateTime.Now
        };

        StavkeCollection = new ObservableCollection<NivelacijaStavka>(Nivelacija.Stavke);
        DgStavke.ItemsSource = StavkeCollection;

        UcitajMagacine();
        PopuniPolja();
        PracunajUkupno();
    }

    private void UcitajMagacine()
    {
        var magacini = _db.Magacini.ToList();
        CmbMagacin.ItemsSource = magacini;
        if (Nivelacija.MagacinId.HasValue)
        {
            CmbMagacin.SelectedValue = Nivelacija.MagacinId.Value;
        }
        else if (magacini.Count > 0)
        {
            CmbMagacin.SelectedIndex = 0;
        }
    }

    private void PopuniPolja()
    {
        TxtBrojNivelacije.Text = Nivelacija.BrojNivelacije.ToString();
        DpDatum.SelectedDate = Nivelacija.DatumNivelacije;
        TxtOpis.Text = Nivelacija.Opis;
    }

    private void BtnDodajStavku_Click(object sender, RoutedEventArgs e)
    {
        var nova = new NivelacijaStavka
        {
            RedniBroj = StavkeCollection.Count + 1,
            KolicinaZaliha = 1,
            StaraCena = 0,
            NovaCena = 0
        };
        StavkeCollection.Add(nova);
        PracunajUkupno();
    }

    private void BtnUkloniStavku_Click(object sender, RoutedEventArgs e)
    {
        if (DgStavke.SelectedItem is NivelacijaStavka selected)
        {
            StavkeCollection.Remove(selected);
            RenumerisiStavke();
            PracunajUkupno();
        }
    }

    private void RenumerisiStavke()
    {
        int rbr = 1;
        foreach (var st in StavkeCollection)
        {
            st.RedniBroj = rbr++;
        }
    }

    private void DgStavke_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
    {
        if (e.Row.Item is NivelacijaStavka st)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (!string.IsNullOrWhiteSpace(st.SifraArtikla))
                {
                    var art = _db.Artikli.FirstOrDefault(a => a.SifraArtikla == st.SifraArtikla);
                    if (art != null)
                    {
                        st.ArtikalId = art.ArtikalId;
                        st.NazivArtikla = art.Naziv;
                        st.JedinicaMere = art.JedinicaMere;
                        if (st.StaraCena == 0) st.StaraCena = art.ProdajnaCena;
                    }
                }

                st.RazlikaPoJedinici = st.NovaCena - st.StaraCena;
                st.UkupnaRazlika = st.KolicinaZaliha * st.RazlikaPoJedinici;
                PracunajUkupno();
            }), System.Windows.Threading.DispatcherPriority.Background);
        }
    }

    private void PracunajUkupno()
    {
        decimal ukupno = StavkeCollection.Sum(s => s.UkupnaRazlika);
        TxtUkupno.Text = $"Ukupna razlika: {ukupno:N2} RSD";
    }

    private void BtnSacuvaj_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(TxtBrojNivelacije.Text.Trim(), out int brojNivelacije))
        {
            MessageBox.Show("Molimo unesite ispravan broj nivelacije.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Nivelacija.BrojNivelacije = brojNivelacije;
        Nivelacija.DatumNivelacije = DpDatum.SelectedDate ?? DateTime.Now;
        Nivelacija.Opis = TxtOpis.Text.Trim();
        if (CmbMagacin.SelectedValue != null)
        {
            Nivelacija.MagacinId = (int)CmbMagacin.SelectedValue;
        }

        Nivelacija.Stavke = StavkeCollection.ToList();
        Nivelacija.UkupnoRazlika = StavkeCollection.Sum(s => s.UkupnaRazlika);

        DialogResult = true;
        Close();
    }

    private void BtnOdustani_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F1)
        {
            OtvoriPomoc();
        }
    }

    private void OtvoriPomoc()
    {
        new EditHelpWindow(
            "🏷️ Pomoć — Nivelacija cena",
            "Zapisnik o promeni prodajnih cena artikala po magacinu.",
            new (string, string)[]
            {
                ("Esc", "Odustaje od unosa bez čuvanja."),
                ("➕ Dodaj stavku", "Dodaje artikal u zapisnik nivelacije."),
                ("🗑️ Ukloni stavku", "Uklanja selektovanu stavku."),
            },
            "Uk. Razlika po stavci = (Nova cena − Stara cena) × Zaliha. Nivelacija automatski svodi vrednost zaliha artikla na novu cenu i evidentira razliku."
        ) { Owner = this }.ShowDialog();
    }
}
