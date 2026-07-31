using System.Windows.Controls;

namespace AccountingApp.Views.Pomoc;

public partial class PomocView : UserControl
{
    private readonly List<PomocTema> _teme = new()
    {
        new PomocTema
        {
            Naslov = "👋 Dobrodošli u ERPi",
            Sadrzaj =
                "ERPi je savremena desktop ERP aplikacija za finansijsko, robno i materijalno knjigovodstvo, " +
                "razvijena po uzoru na legacy DOS/Clipper sisteme (FIN, ANAL, ROB, MAT) sa savremenom grafikom i bazom podataka.\n\n" +
                "KORISNIČKI KONCEPTI:\n" +
                "• Svaka firma ima sopstvenu izolovanu SQLite bazu podataka u folderu aplikacije.\n" +
                "• Naziv i šifra trenutno aktivne firme prikazani su u gornjem delu bočnog menija.\n" +
                "• Brza promena aktivne firme vrši se klikom na karticu 'Aktivna firma' u bočnom meniju ili kroz meni '🏢 Upravljanje firmama'.\n\n" +
                "PRATITE TEME POMOĆI:\n" +
                "Sa leve strane izaberite željenu oblast da biste pročitali detaljna uputstva za rad sa nalozima, karticama, IOS-om, robnim i materijalnim poslovanjem."
        },
        new PomocTema
        {
            Naslov = "🔐 Prijava, korisnici i bezbednost",
            Sadrzaj =
                "1. PRIJAVA NA SISTEM:\n" +
                "• Nakon pokretanja aplikacije prikazuje se ekran za prijavu.\n" +
                "• Podrazumevano korisničko ime za novu firmu je 'admin' sa lozinkom 'admin'.\n" +
                "• Preporučuje se promena podrazumevane lozinke u meniju '👤 Korisnici i Uloge'.\n\n" +
                "2. ULOGE I PRAVA PRISTUPA (RBAC):\n" +
                "• Administrator: Puni pristup svim funkcijama, uključujući rasknjižavanje naloga, prenos u novu poslovnu godinu, upravljanje korisnicima i restauraciju rezervnih kopija.\n" +
                "• Knjigovođa: Rad sa nalozima (unos, izmena nacrta, knjiženje), robnim i materijalnim poslovanjem, izveštajima i karticama.\n" +
                "• Gledalac (Auditor): Prikaz podataka i generisanje PDF izveštaja bez prava unosa ili izmene.\n\n" +
                "3. BEZBEDNOST LOZINKI:\n" +
                "Lozinke se čuvaju kriptovane osoljenim algoritmom PBKDF2 (100.000 iteracija HMAC-SHA256) i nikada se ne zapisuju u čistom tekstu."
        },
        new PomocTema
        {
            Naslov = "📊 Radna tabla (Dashboard)",
            Sadrzaj =
                "Radna tabla pruža brzi vizuelni pregled stanja u poslovanju firme:\n\n" +
                "KLJUČNI INDIKATORI (KPI):\n" +
                "• Broj proknjiženih naloga glavne knjige u tekućoj godini.\n" +
                "• Ukupan broj konta definisanih u Kontnom planu.\n" +
                "• Broj artikala u robnom i materijalnom šifarniku sa trenutnim stanjem zaliha.\n" +
                "• Broj registrovanih poslovnih partnera.\n\n" +
                "BRZE AKCIJE:\n" +
                "Preko dugmadi na radnoj tabli možete jednim klikom otvoriti unos novog naloga, pregled kartica ili generisati bruto bilans."
        },
        new PomocTema
        {
            Naslov = "📖 Glavna knjiga i Nalozi za knjiženje",
            Sadrzaj =
                "Meni '📖 Glavna knjiga i Nalozi' služi za dvostruko knjigovodstveno knjiženje.\n\n" +
                "1. UNOS NOVOG NALOGA:\n" +
                "• Kliknite na dugme '➕ Novi nalog'.\n" +
                "• Unesite broj naloga, datum i opis naloga.\n" +
                "• Dodajte stavke (Broj konta, Dokument, Opis stavke, Duguje, Potražuje, Partner).\n" +
                "• Tokom unosa stavki na dnu prozora se u realnom vremenu prikazuje ŽIVA PROVERA RAVNOTEŽE (Duguje = Potražuje). Knjiženje je dozvoljeno samo ako je saldo naloga 0,00 RSD (zelena indikacija).\n" +
                "• Taster 'F2' u polju opisa stavke otvara brzi šifarnik opisa promena.\n\n" +
                "2. KNJIŽENJE I RAS KNJIŽAVANJE:\n" +
                "• Dugme 'Proknjiži' zaključava nalog i upisuje stavke u glavnu knjigu.\n" +
                "• Dugme 'Rasknjiži' (dostupno administratorima) vraća proknjižen nalog u status nacrta radi ispravke grešaka, uz obavezno evidentiranje u audit logu.\n\n" +
                "3. PRENOS U NOVU POSLOVNU GODINU:\n" +
                "• Dugme 'Nova godina' kreira nalog početnog stanja na dan 01.01. naredne godine sa preneta 6 kolona salda iz tekuće godine."
        },
        new PomocTema
        {
            Naslov = "📋 Dnevnik i Kartice konta",
            Sadrzaj =
                "Meni '📋 Dnevnik i Kartice konta' omogućava detaljan hronološki uvid u promet konta.\n\n" +
                "1. PREGLED KARTICE JEDNOG KONTA:\n" +
                "• U levoj listi izaberite željeni konto ili unesite broj konta u pretragu (npr. '204015').\n" +
                "• Postavite opseg datuma 'Od:' i 'Do:'.\n" +
                "• Tabela sa desne strane prikazuje sve stavke, broj naloga, opis, dugovni i potražni promet, kao i tekući kumulativni saldo.\n\n" +
                "2. MASOVNA ŠTAMPA IZABRANIH KARTICA:\n" +
                "• U levoj listi konta štriklirajte CheckBox pored više konta koje želite štampati.\n" +
                "• Kliknite na dugme '🖨️ Štampaj izabrane (PDF)' — aplikacija će u jednom PDF dokumentu izgenerisati sve označene kartice pojedinačno po kontima.\n" +
                "• Dugme '📊 Excel' izvozi trenutno prikazanu karticu u Excel tabelu."
        },
        new PomocTema
        {
            Naslov = "👥 Partneri i Otvorene stavke (IOS)",
            Sadrzaj =
                "Meni '👥 Partneri i Otvorene stavke' pruža analitiku kupaca i dobavljača i rad sa IOS obrascima (legacy gk91).\n\n" +
                "1. PODEŠAVANJE I POKRETANJE IOS-A:\n" +
                "• Polja 'Od konta' i 'Do konta' podrazumevano ostavite prazna — tako će izveštaj obuhvatiti sve analitičke konta partnera (npr. 204 kupci, 435 dobavljači, 150 avansi).\n" +
                "• Po želji unesite '204' za samo kupce ili '435' za samo dobavljače.\n" +
                "• Kliknite na dugme '👁 Prikaži na ekranu' za otvaranje interaktivnog ekranskog pregleda.\n\n" +
                "2. RAD U EKRANSKOM PREGLEDU IOS-A (IosPreviewWindow):\n" +
                "• U levoj tabeli se prikazuju svi partneri sa učešćem i nazivom iz kontnog plana.\n" +
                "• Pomoću CheckBox-ova uz svakog partnera možete izabrati partnere za štampu.\n" +
                "• CheckBox 'Samo neusaglašeni (nenulti) saldo' filtrira partnere koji imaju nezatvoren saldo.\n" +
                "• Dugme '📄 Štampaj prikazanu (PDF)': Generiše zvanični IOS obrazac sa potvrdom/osporavanjem samo za trenutno izabranog partnera.\n" +
                "• Dugme '🖨️ Štampaj izabrane (PDF)': Generiše zbirni PDF sa IOS obrascima za sve štriklirane partnere.\n" +
                "• Dugme '📥 Izvezi sve (PDF)': Generiše zbirni PDF za sve prikazane partnere sa liste."
        },
        new PomocTema
        {
            Naslov = "📄 Finansijski izveštaji i PDF štampe",
            Sadrzaj =
                "Meni '📄 Finansijski izveštaji' sadrži zvanične štampane izveštaje sa QuestPDF generisanjem dokumentacije:\n\n" +
                "1. BRUTO BILANS (6 KOLONA):\n" +
                "• Generiše bilans sa kolona: Početno stanje (Duguje/Potražuje), Promet (Duguje/Potražuje) i Ukupan Saldo (Duguje/Potražuje).\n" +
                "• Sadrži međuzbirove po sintetičkim kontima (3 cifre) i celim klasama (0 do 9).\n\n" +
                "2. DNEVNIK GLAVNE KNJIGE:\n" +
                "• Hronološki štampani pregled svih proknjiženih stavki po datumu i broju naloga.\n\n" +
                "3. KARTICE PARTNERA I IOS ZBIRNI IZVEŠTAJI."
        },
        new PomocTema
        {
            Naslov = "📦 Robno knjigovodstvo (VP / MP, Fakture i Nivelacije)",
            Sadrzaj =
                "Meni '📊 Radna tabla' (u sekciji ROBNO KNJIGOVODSTVO) prikazuje vrednost zaliha (VP/MP), poslednje kalkulacije i nivelacije i brze akcije za nov unos.\n\n" +
                "Meni '📦 Kalkulacije i Nivelacije' pokriva robno poslovanje (Clipper MAT1–MAT7):\n\n" +
                "1. KALKULACIJE NABAVKE (MAT3 / MAT6):\n" +
                "• Veleprodajna kalkulacija: Ulaz po dobavljačkoj ceni, zavisni troškovi, marža i formiranje veleprodajne cene.\n" +
                "• Maloprodajna kalkulacija: Obračun ukalkulisane marže i PDV-a za prodaju fizičkim licima.\n\n" +
                "2. IZLAZNE FAKTURE I OTPREMNICE (MAT5):\n" +
                "• Izdavanje faktura kupcima sa automatskim proračunom PDV-a, rabata %, rokom dospelosti i štampom u PDF.\n" +
                "• Automatsko razduživanje zaliha i mogućnost generisanja naloga za knjiženje u Glavnoj knjizi.\n\n" +
                "3. NIVELACIJE CENA (MAT7):\n" +
                "• Promena prodajnih cena artikala po magacinu sa automatskim zapisnikom o nivelaciji i svođenjem na novu vrednost zaliha.\n\n" +
                "4. ZADUŽENJA, RAZDUŽENJA I PRIMOPREDAJE (MAT4):\n" +
                "• Interni prenosi robe između magacina, sa filterom 'Svi / Proknjiženi / Neproknjiženi' iznad svake tabele.\n\n" +
                "5. RASKNJIŽAVANJE (svi tabovi gde se knjiži — Zaduženja, Razduženja, Primopredaje, Kalkulacije, Računi-Otpremnice, Nivelacije):\n" +
                "• Klik na 'Izmeni' nad proknjiženim dokumentom nudi pitanje 'Da li želite da rasknjižite radi izmene?' (isto kao kod naloga glavne knjige).\n" +
                "• Dostupno samo administratorima. Rasknjižavanje bezbedno poništava samo promet koji je taj dokument upisao — ako je u međuvremenu nešto knjiženo posle njega za isti artikal/magacin, rasknjižavanje se odbija radi zaštite tačnosti zaliha."
        },
        new PomocTema
        {
            Naslov = "🏭 Materijalno knjigovodstvo i Skladište",
            Sadrzaj =
                "Meni '📊 Radna tabla' (u sekciji MATERIJALNO KNJIGOVODSTVO) prikazuje vrednost zaliha materijala, broj materijala na zalihi, upozorenje o negativnim stanjima, poslednje ulaze/trebovanja i brze akcije za nov unos.\n\n" +
                "Meni '🏭 Skladište i Zalihe' obezbeđuje precizno praćenje materijala (Clipper M1–M4):\n\n" +
                "1. KARTICE MATERIJALA (M1):\n" +
                "• Praćenje zaliha po ponderisanoj prosečnoj nabavnoj ceni (Weighted Average Cost).\n\n" +
                "2. PRIJEMNICE MATERIJALA (M2):\n" +
                "• Prijem sirovina i materijala u magacin sa ulaznom fakturnom cenom.\n\n" +
                "3. TREBOVANJA I IZDATNICE (M3):\n" +
                "• Razduženje materijala iz magacina i prenos na konto troškova po trenutnoj prosečnoj ceni.\n\n" +
                "4. PRIMOPREDAJE MATERIJALA (M4):\n" +
                "• Interni prenosi materijala između magacina, sa filterom 'Svi / Proknjiženi / Neproknjiženi' iznad svake tabele.\n\n" +
                "5. RASKNJIŽAVANJE (Ulazi, Trebovanja, Primopredaje):\n" +
                "• Klik na 'Izmeni' nad proknjiženim dokumentom nudi pitanje 'Da li želite da rasknjižite radi izmene?', dostupno samo administratorima — isti princip kao u Robnom knjigovodstvu i Glavnoj knjizi.\n\n" +
                "6. POPISNE LISTE I NIKAD VEĆA PRECIZNOST:\n" +
                "• Unos stvarnog popisanog stanja i automatski proračun viškova i manjkova materijala."
        },
        new PomocTema
        {
            Naslov = "🧾 PDV Evidencija & APR Bilansi",
            Sadrzaj =
                "1. PDV EVIDENCIJA (KPR i KIR):\n" +
                "• Knjiga primljenih računa (KPR) — uvoz nabavki i ulaznog PDV-a koji se odbija.\n" +
                "• Knjiga izdatih računa (KIR) — uvoz izlaznih faktura i izlaznog PDV-a.\n" +
                "• Automatska priprema podataka za POPDV prijavu Poreskoj upravi.\n\n" +
                "2. ZVANIČNI APR BILANSI:\n" +
                "• Bilans stanja (Imovina, Kapital i Obaveze).\n" +
                "• Bilans uspeha (Prihodi, Rashodi i Finansijski rezultat).\n" +
                "• Izvoz i štampa obrazaca za zvaničnu predaju APR-u."
        },
        new PomocTema
        {
            Naslov = "🔄 Uvoz iz legacy DOS / Clipper sistema",
            Sadrzaj =
                "Ukoliko prelazite sa starih DOS/Clipper programa (ARHIBEL / FIN2 / MAT):\n\n" +
                "1. Idite u meni '⚙️ Podešavanja' -> dugme '🔄 Uvoz podataka iz legacy DOS sistema'.\n" +
                "2. Izaberite folder sa dBase III / Clipper DBF fajlovima (npr. C:\\FIRME\\ARHIBEL\\Radni ili C:\\KNJIGE\\Radni\\KOR01).\n" +
                "3. Sistem automatski prepoznaje i uvozi fajlove:\n" +
                "   • KONTPLAN.DBF -> Kontni plan u SQLite bazi\n" +
                "   • NALOG.DBF & STAVKE -> Nalozi i stavke glavne knjige\n" +
                "   • PROMENE.DBF -> Šifarnik opisa promena\n" +
                "   • MAGACIN.DBF & ARTIKLI.DBF -> Magacini i robno-materijalni šifarnik\n" +
                "4. Nakon uvoza svi podaci su odmah spremni za rad i izveštavanje u novom sistemu!"
        },
        new PomocTema
        {
            Naslov = "⌨️ Korisne prečice i tasteri",
            Sadrzaj =
                "Za maksimalnu brzinu u radu bez miša podržane su standardne tastaturne prečice:\n\n" +
                "• Ctrl + F — Otvara pretragu glavnog menija aplikacije sa bilo kog ekrana.\n" +
                "• Ctrl + M — Sklapa ili proširuje bočni navigacioni meni.\n" +
                "• Esc — Zatvara bilo koji otvoreni modalni prozor ili dijalog (IOS pregled, faktura, pretraga, unos naloga).\n" +
                "• Tab / Shift + Tab — Kretanje napred/nazad kroz polja za unos.\n" +
                "• Enter — Potvrda unosa u tabelama i prelaze u sledeći red.\n" +
                "• F2 / ... — Otvara pomoćni šifarnik u poljima gde je omogućen izbor iz liste."
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
