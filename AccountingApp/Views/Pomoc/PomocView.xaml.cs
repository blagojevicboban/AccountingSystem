using System.Windows.Controls;

namespace AccountingApp.Views.Pomoc;

public partial class PomocView : UserControl
{
    private readonly List<PomocTema> _teme = new()
    {
        new PomocTema
        {
            Naslov = "👋 Dobrodošli",
            Sadrzaj =
                "AccountingSystem je desktop ERP aplikacija za finansijsko knjigovodstvo, razvijena po uzoru na " +
                "legacy DOS/Clipper sistem (moduli FIN, ANAL, ROB, MAT).\n\n" +
                "Sa leve strane izaberite temu da biste videli uputstvo za tu funkciju. Svaka firma ima sopstvenu " +
                "SQLite bazu podataka — trenutno aktivna firma je prikazana u gornjem levom uglu sidebar-a.\n\n" +
                "Podrazumevana prijava (na novoj bazi) je korisničko ime „admin“ i lozinka „admin123“."
        },
        new PomocTema
        {
            Naslov = "🔐 Prijava i korisnici",
            Sadrzaj =
                "Pristup aplikaciji zahteva prijavu korisničkim imenom i lozinkom. Lozinke se čuvaju osoljene " +
                "(PBKDF2, 100.000 iteracija) — nikada u čistom tekstu.\n\n" +
                "Uloga „Administrator“ ima dodatna ovlašćenja: rasknjižavanje naloga i prenos u novu godinu su " +
                "dozvoljeni samo administratoru, jer menjaju već proknjižene/zaključene podatke."
        },
        new PomocTema
        {
            Naslov = "📊 Radna tabla",
            Sadrzaj =
                "Početni ekran posle prijave. Prikazuje ključne brojke: broj proknjiženih naloga, broj konta u " +
                "kontnom planu, broj artikala na zalihama i broj partnera, kao i poslednje naloge za knjiženje i " +
                "osnovne podatke o firmi."
        },
        new PomocTema
        {
            Naslov = "📖 Glavna knjiga — nalozi za knjiženje",
            Sadrzaj =
                "Ovde se unose, izmenjuju i knjiže nalozi (dvostruko knjigovodstvo — Duguje/Potražuje).\n\n" +
                "• „Novi nalog“ — otvara dijalog za unos naloga: broj, datum, opis, i stavke (konto, dokument, " +
                "opis, duguje, potražuje, opciono partner). Dijalog prikazuje ŽIVU proveru ravnoteže (zeleno = " +
                "u ravnoteži, žuto = nema stavki, crveno = razlika).\n" +
                "• „Izmeni“ — dozvoljeno samo za neproknjižene naloge (nacrte).\n" +
                "• „Proknjiži“ — knjiži nalog; odbija ako Duguje ≠ Potražuje.\n" +
                "• „Rasknjiži“ (samo Administrator) — vraća proknjižen nalog u status nacrta radi ispravke, uz " +
                "potvrdu. Posle rasknjižavanja nalog se ponovo može izmeniti.\n" +
                "• „Nova godina“ (samo Administrator) — prenosi zaključni saldo svih konta u nalog za početno " +
                "stanje naredne godine (01.01.). Odbija prenos ako knjige nisu u ravnoteži — to je znak da " +
                "postoji neispravan nalog koji treba ispraviti (rasknjižiti i popraviti) pre prenosa."
        },
        new PomocTema
        {
            Naslov = "📋 Kartice konta",
            Sadrzaj =
                "Hronološki pregled prometa i tekućeg salda za izabrani konto iz kontnog plana. Pretražite konto " +
                "sa leve strane, a tabela prikazuje datum, nalog, opis, duguje/potražuje i kumulativni saldo posle " +
                "svake stavke. Dugme „Štampaj karticu (PDF)“ izvozi karticu u PDF."
        },
        new PomocTema
        {
            Naslov = "👥 Partneri (Analitika) i otvorene stavke",
            Sadrzaj =
                "Ovde se prati analitika kupaca i dobavljača — otvorene stavke (nenaplaćena/neplaćena dugovanja) " +
                "po partneru.\n\n" +
                "Napomena: da bi se stavka pojavila kod partnera, mora joj se dodeliti partner u polju „Partner“ " +
                "prilikom unosa naloga (u Glavnoj knjizi). Istorijski uvezeni nalozi iz DOS sistema nemaju " +
                "dodeljene partnere jer legacy ANAL modul za ovu firmu nije korišćen.\n\n" +
                "• „Izvezi IOS (PDF)“ — generiše Izvod Otvorenih Stavki za odabranog partnera.\n" +
                "• „Obračun kamate“ — otvara poseban prozor za obračun zatezne kamate (videti sledeću temu)."
        },
        new PomocTema
        {
            Naslov = "💰 Kamate",
            Sadrzaj =
                "Obračun zatezne kamate na dugovne otvorene stavke partnera, po danu kašnjenja i važećoj " +
                "kamatnoj stopi.\n\n" +
                "• Sa leve strane se vidi tabela kamatnih stopa (svaka važi od svog datuma do sledeće stope) i " +
                "forma za unos nove stope.\n" +
                "• Unesite datum obračuna i kliknite „Obračunaj“ — tabela prikazuje svaku dugovnu stavku, broj " +
                "dana kašnjenja i obračunatu kamatu.\n\n" +
                "VAŽNA NAPOMENA: uvezene istorijske stope (iz legacy sistema) datiraju iz 2004–2006. godine. Za " +
                "ispravan obračun na tekućim dugovanjima unesite AKTUELNU zvaničnu stopu zatezne kamate pre " +
                "obračuna — sistem ne pretpostavlja niti izmišlja trenutnu stopu."
        },
        new PomocTema
        {
            Naslov = "📦 Magacin i zalihe",
            Sadrzaj =
                "Praćenje materijala po magacinima, sa karticom po prosečnoj (ponderisanoj) ceni.\n\n" +
                "• Tab „Kartice materijala“ — izaberite magacin i artikal da vidite karticu (ulaz/izlaz/stanje/" +
                "cena/vrednost).\n" +
                "• Tab „Ulazi“ — prijem materijala. Pri knjiženju, prijem se vrednuje po UNETOJ ceni, a vrednost " +
                "zaliha se akumulira.\n" +
                "• Tab „Trebovanja“ — izdavanje materijala iz magacina. Pri knjiženju, izdavanje se vrednuje po " +
                "TRENUTNOJ prosečnoj ceni (ukupna vrednost zaliha / ukupna količina) — cena se ne unosi ručno. " +
                "Sistem odbija trebovanje ako bi izazvalo negativno stanje na zalihama."
        },
        new PomocTema
        {
            Naslov = "🛒 Trgovina i fakture",
            Sadrzaj =
                "Kalkulacija veleprodaje — obračun prodajne cene robe na osnovu nabavne vrednosti, zavisnih " +
                "troškova, trgovačke marže i poreza (PDV).\n\n" +
                "Formula: Svega troškovi = zbir zavisnih troškova (transport, uskladištenje, utovar/istovar, " +
                "osiguranje, ostalo). Svega nabavno = nabavna vrednost + svega troškovi. Razlika = svega nabavno " +
                "× marža%. Porez = (svega nabavno + razlika) × PDV%. Prodajna vrednost = svega nabavno + razlika " +
                "+ porez.\n\n" +
                "Obračun se prikazuje uživo dok unosite vrednosti. „Snimi kalkulaciju“ čuva nacrt, a „Proknjiži“ " +
                "ga zaključuje."
        },
        new PomocTema
        {
            Naslov = "📄 Izveštaji i PDF",
            Sadrzaj =
                "Dostupni PDF izveštaji:\n\n" +
                "• Dnevnik glavne knjige — hronološki spisak svih proknjiženih naloga.\n" +
                "• Bruto bilans — promet i saldo po kontu (finansijski).\n" +
                "• IOS — izvod otvorenih stavki (bira se partner na tabu Partneri).\n" +
                "• Izveštaj o zalihama.\n" +
                "• Bruto bilans analitike — promet i saldo po partneru (analitika kupaca/dobavljača)."
        },
        new PomocTema
        {
            Naslov = "📋 Kontni plan (Šifarnik konta)",
            Sadrzaj =
                "Modul za pregled, unos, izmenu i štampu kontnog plana (analogno DOS proceduri gk1).\n\n" +
                "• „Novi konto“ — otvara dijalog za unos novog konta (broj konta, naziv, vrsta konta). Klasa i sintetički/analitički status se automatski određuju.\n" +
                "• „Izmeni“ — izmena naziva ili vrste postojećeg konta.\n" +
                "• „Obriši“ — brisanje konta (dozvoljeno samo za konta bez proknjiženih stavki).\n" +
                "• „Štampaj Kontni plan (PDF)“ — generisanje i pregled kompletne liste konta u PDF formatu."
        },
        new PomocTema
        {
            Naslov = "🔄 Uvoz iz DOS sistema",
            Sadrzaj =
                "Dugme za pokretanje uvoza DOS podataka nalazi se u ekranu Podešavanja aplikacije (⚙️ Podešavanja -> 🔄 Uvoz podataka iz legacy DOS sistema).\n\n" +
                "Alat uvozi kontni plan, naloge knjiženja, partnere, materijale, magacine, ulaze i kartice iz dBase III / Clipper datoteka (C:\\KNJIGE\\Radni\\KORxx) direktno u SQLite bazu aktivne firme."
        }
    };

    public PomocView()
    {
        InitializeComponent();
        LstTeme.ItemsSource = _teme;
        if (_teme.Count > 0) LstTeme.SelectedIndex = 0;
    }

    private void LstTeme_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LstTeme.SelectedItem is PomocTema tema)
        {
            TxtNaslovTeme.Text = tema.Naslov;
            TxtSadrzajTeme.Text = tema.Sadrzaj;
        }
    }
}
