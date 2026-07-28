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
                "AccountingSystem je savremena desktop ERP aplikacija za finansijsko, robno i materijalno knjigovodstvo, " +
                "razvijena po uzoru na legacy DOS/Clipper sistem (moduli FIN, ANAL, ROB, MAT).\n\n" +
                "Sa leve strane izaberite temu da biste videli detaljno uputstvo za odabranu funkciju. Svaka firma ima sopstvenu " +
                "SQLite bazu podataka — trenutno aktivna firma je prikazana u gornjem levom uglu sidebar-a.\n\n" +
                "Podrazumevana prijava (na novoj bazi) je korisničko ime „admin“ i lozinka „admin“."
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
                "• „Brzi šifrarnik opisa promena (... / F2)“ — direktan unos i izbor šifara opisa u toku knjiženja.\n" +
                "• „Izmeni“ — dozvoljeno samo za neproknjižene naloge (nacrte).\n" +
                "• „Proknjiži“ — knjiži nalog; odbija ako Duguje ≠ Potražuje.\n" +
                "• „Rasknjiži“ (samo Administrator) — vraća proknjižen nalog u status nacrta radi ispravke.\n" +
                "• „Nova godina“ (samo Administrator) — prenosi zaključni saldo svih konta u nalog za početno " +
                "stanje naredne godine (01.01.)."
        },
        new PomocTema
        {
            Naslov = "📋 Kartice konta & Bruto bilans",
            Sadrzaj =
                "Hronološki pregled prometa i tekućeg salda za kontni plan.\n\n" +
                "• Kartice konta: pretraga po broju konta, prikaz duguje/potražuje i kumulativnog salda.\n" +
                "• Bruto bilans (PDF): prikaz sa tačno 6 kolona (Promet Duguje, Promet Potražuje, Saldo Duguje, Saldo Potražuje) " +
                "sa međuzbirovima po 3-cifrenim sintetičkim kontima i klasama, potpuno usaglašeno sa Clipper FIN2.PRG izveštajima."
        },
        new PomocTema
        {
            Naslov = "🛒 Trgovina, Fakture i Kalkulacije (ROB)",
            Sadrzaj =
                "Modul za robno poslovanje i trgovinu (odgovara Clipper modulima MAT1–MAT7):\n\n" +
                "• Računopolagači (MAT1) i Šifarnik artikala (MAT2): samostalni CRUD ekrani za magacine i artikle, sa PDF štampom.\n" +
                "• Kalkulacije: izbor Veleprodaje (MAT6) sa zavisnim troškovima i maržom, ili Maloprodaje (MAT3) sa ukalkulisanom maržom i PDV-om.\n" +
                "• Računi - Otpremnice / Fakture (MAT5): izdavanje faktura kupcima sa rokom dospelosti, rabatom %, PDV-om, unosom stavki po šifri artikla, automatskim razduživanjem robe i generisanjem finansijskog naloga u Glavnoj knjizi.\n" +
                "• Nivelacije cena (MAT7): promena prodajnih cena artikala po magacinu, uz automatsku generaciju zapisnika svođenjem na prosečnu nabavnu cenu i masovno knjiženje svih nezaknjiženih nivelacija.\n" +
                "• Poreske tarife: šifarnik poreskih stopa (tarifni broj, porez %, poseban porez %) sa CRUD ekranom i PDF štampom.\n" +
                "• Robni bruto bilans: početno stanje/ulaz/izlaz/stanje po magacinu i artiklu, količinski i vrednosno.\n" +
                "• PDF Štampa: izvoz zvaničnih faktura, kalkulacija, zapisnika o nivelaciji cena, šifarnika i robnog bruto bilansa u PDF format."
        },
        new PomocTema
        {
            Naslov = "📦 Magacin i zalihe (MAT)",
            Sadrzaj =
                "Praćenje materijala po magacinima po uzoru na Clipper M1–M4 modula:\n\n" +
                "• Kartice materijala (M1): praćenje zaliha po ponderisanoj prosečnoj ceni (Weighted Average Cost).\n" +
                "• Ulazi (M2): prijem materijala u magacin po unetim dobavljačkim cenama.\n" +
                "• Trebovanja (M3): izdavanje materijala na konto troška po trenutnoj prosečnoj ceni.\n" +
                "• Primopredaje (M4): interni prenos materijala iz dajućeg magacina u ulazni magacin sa automatskim proračunom prosečne vrednosti."
        },
        new PomocTema
        {
            Naslov = "💰 Kamate i partneri (ANAL)",
            Sadrzaj =
                "Pratite analitiku kupaca i dobavljača (otvorene stavke / IOS) i obračun zatezne kamate po važećim dnevnim stopama kašnjenja."
        },
        new PomocTema
        {
            Naslov = "⌨️ Korisne prečice i tasteri",
            Sadrzaj =
                "Brza i efikasna navigacija u radu sa aplikacijom:\n\n" +
                "• Esc — zatvara svaki otvoreni modalni dijalog (Fakture, Nivelacije, Unos naloga, Primopredaje, Opise promena).\n" +
                "• Tab / Enter — pomeranje fokusa između polja za brzi unos podataka bez upotrebe miša.\n" +
                "• F2 / ... — brzi pristup šifrarnicima unutar polja unosa."
        },
        new PomocTema
        {
            Naslov = "🔄 Uvoz iz DOS sistema",
            Sadrzaj =
                "Nalazi se u ⚙️ Podešavanja -> 🔄 Uvoz podataka iz legacy DOS sistema.\n\n" +
                "Alat automatski binarno čita dBase III / Clipper fajlove (KONTPLAN, NALOG, PROMENE, MAGACIN, ARTIKLI, ULAZ, TREBOV, RAC_OTP, KALKULAC) i uvozi ih u SQLite bazu."
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
