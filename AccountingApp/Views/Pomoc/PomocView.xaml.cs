using System;
using System.Linq;
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
                "Preko dugmadi na radnoj tabli možete jednim klikom otvoriti unos novog naloga, pregled kartica ili generisati bruto bilans.",
            Kljuc = "Dashboard"
        },
        new PomocTema
        {
            Naslov = "📋 Kontni plan",
            Sadrzaj =
                "Meni '📋 Kontni plan' (u sekciji MATIČNI PODACI) sadrži šifarnik svih konta koja se koriste u knjiženju.\n\n" +
                "1. HIJERARHIJA KONTA:\n" +
                "• Konta su organizovana po broju cifara: klasa (1 cifra), grupa (2 cifre), sintetika (3 cifre) i analitika (4+ cifara, npr. konto partnera ili artikla).\n" +
                "• Analitička konta (npr. 204015 — konkretan kupac) nasleđuju naziv i tip od svoje sintetike.\n\n" +
                "2. DODAVANJE, IZMENA I BRISANJE:\n" +
                "• Dugme '➕ Novi konto' otvara formu za unos broja i naziva konta i njegovog tipa (aktiva/pasiva/prihod/rashod).\n" +
                "• Izmena i brisanje su dozvoljeni samo dok na kontu nema proknjiženog prometa — konta sa istorijom knjiženja se ne mogu obrisati radi očuvanja tačnosti izveštaja.\n\n" +
                "3. PRETRAGA I IZVOZ:\n" +
                "• Polje za pretragu filtrira po broju ili nazivu konta u realnom vremenu.\n" +
                "• Dugmad '🖨️ PDF' i '📊 Excel' izvoze trenutno prikazan kontni plan.\n\n" +
                "Kontni plan je osnovni šifarnik na koji se oslanjaju Nalozi, Kartice i Bilansi — bez definisanog konta nije moguće knjižiti niti generisati izveštaje za njega.",
            Kljuc = "Konta"
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
                "• Dugme 'Nova godina' kreira nalog početnog stanja na dan 01.01. naredne godine sa preneta 6 kolona salda iz tekuće godine.\n\n" +
                "4. MASOVNO PREKNJIŽAVANJE:\n" +
                "• Za reklasifikaciju više naloga odjednom (npr. promena konta na većem broju stavki) koristi se prozor za preknjižavanje, dostupan iz liste naloga.",
            Kljuc = "Nalozi"
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
                "• Dugme '📊 Excel' izvozi trenutno prikazanu karticu u Excel tabelu.",
            Kljuc = "Kartice"
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
                "• Dugme '📥 Izvezi sve (PDF)': Generiše zbirni PDF za sve prikazane partnere sa liste.\n\n" +
                "3. OBRAČUN ZATEZNE KAMATE:\n" +
                "• Za partnere sa neplaćenim otvorenim stavkama, prozor za obračun kamate izračunava zateznu kamatu na osnovu unetog perioda i stope.",
            Kljuc = "Partneri"
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
                "3. KARTICE PARTNERA I IOS ZBIRNI IZVEŠTAJI.",
            Kljuc = "Izvestaji"
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
                "• Dostupno samo administratorima. Rasknjižavanje bezbedno poništava samo promet koji je taj dokument upisao — ako je u međuvremenu nešto knjiženo posle njega za isti artikal/magacin, rasknjižavanje se odbija radi zaštite tačnosti zaliha.\n\n" +
                "6. ŠIFARNIK ARTIKALA I PORESKIH TARIFA:\n" +
                "• Artikli (naziv, jedinica mere, cena) i poreske tarife (stope PDV-a) koje se koriste u kalkulacijama i fakturama uređuju se u posebnim formama dostupnim iz šifarnika robe.",
            Kljuc = "Robno"
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
                "• Unos stvarnog popisanog stanja i automatski proračun viškova i manjkova materijala.\n\n" +
                "7. PROVERA KARTICA:\n" +
                "• Alat za proveru integriteta podataka — upoređuje izračunato stanje po karticama materijala sa evidentiranim prometom i prijavljuje eventualna odstupanja.",
            Kljuc = "Magacin"
        },
        new PomocTema
        {
            Naslov = "🧾 PDV Evidencija (KPR i KIR)",
            Sadrzaj =
                "1. PDV EVIDENCIJA (KPR i KIR):\n" +
                "• Knjiga primljenih računa (KPR) — uvoz nabavki i ulaznog PDV-a koji se odbija.\n" +
                "• Knjiga izdatih računa (KIR) — uvoz izlaznih faktura i izlaznog PDV-a.\n" +
                "• Automatska priprema podataka za POPDV prijavu Poreskoj upravi.\n\n" +
                "2. ZVANIČNI APR BILANSI:\n" +
                "• Za Bilans stanja i Bilans uspeha pogledajte posebnu temu '🏛️ Bilansi (APR)'.",
            Kljuc = "Pdv"
        },
        new PomocTema
        {
            Naslov = "🏛️ Bilansi (APR)",
            Sadrzaj =
                "Meni '🏛️ Bilansi (APR)' generiše zvanične finansijske izveštaje za predaju Agenciji za privredne registre.\n\n" +
                "1. DVA TABA:\n" +
                "• Bilans stanja — Imovina, Kapital i Obaveze na dan izveštavanja.\n" +
                "• Bilans uspeha — Prihodi, Rashodi i Finansijski rezultat za period.\n\n" +
                "2. OSVEŽAVANJE OBRAČUNA:\n" +
                "• Dugme '🔄 Osveži obračun' ponovo izračunava vrednosti na osnovu svih proknjiženih (ne i nacrt) naloga glavne knjige.\n\n" +
                "3. AOP KOLONE:\n" +
                "• Svaka pozicija bilansa (AOP) mapirana je na opseg konta iz Kontnog plana — izmena kontnog plana može uticati na koje AOP pozicije se sabira konto.\n\n" +
                "4. IZVOZ:\n" +
                "• Dugmad '🖨️ PDF' i '📊 Excel' izvoze trenutno prikazan tab (Bilans stanja ili Bilans uspeha) posebno.",
            Kljuc = "Bilansi"
        },
        new PomocTema
        {
            Naslov = "🏢 Upravljanje firmama",
            Sadrzaj =
                "Meni '🏢 Upravljanje firmama' (u sekciji PODEŠAVANJA I SISTEM) služi za rad sa više pravnih lica u istoj instalaciji aplikacije.\n\n" +
                "1. IZOLOVANE BAZE PODATAKA:\n" +
                "• Svaka firma ima sopstvenu, potpuno izolovanu SQLite bazu — podaci jedne firme nikada nisu vidljivi u drugoj.\n\n" +
                "2. NOVA FIRMA:\n" +
                "• Dugme '➕ Nova firma' kreira novu praznu bazu sa unetim nazivom i šifrom firme.\n\n" +
                "3. AKTIVACIJA I PROMENA:\n" +
                "• Dugme '⭐ Aktiviraj' postavlja firmu kao trenutno aktivnu za rad — isto se postiže klikom na karticu 'Aktivna firma' u bočnom meniju.\n\n" +
                "4. IZMENA I BRISANJE:\n" +
                "• '✏️ Izmeni' menja naziv/šifru firme. '🗑️ Briši' trajno uklanja firmu i njenu bazu podataka — akcija je nepovratna, pre brisanja obavezno napraviti rezervnu kopiju (vidi temu 'Rezervne kopije').",
            Kljuc = "Firme"
        },
        new PomocTema
        {
            Naslov = "⚙️ Podešavanja",
            Sadrzaj =
                "Meni '⚙️ Podešavanja' sadrži osnovna podešavanja aplikacije, podeljena u nekoliko celina:\n\n" +
                "1. GENERALNA PODEŠAVANJA:\n" +
                "• Putanja do SQLite baze podataka i opcija 'Pokreni maksimizovano' pri sledećem startu aplikacije.\n\n" +
                "2. PODACI ZA ŠTAMPU/PDF:\n" +
                "• Naziv firme i ime ovlašćenog lica koji se ispisuju u zaglavlju/podnožju svih PDF izveštaja (bilansi, kartice, IOS, fakture).\n\n" +
                "3. BEZBEDNOSNE PROVERE:\n" +
                "• Uključivanje/isključivanje potvrde pre rasknjižavanja i potvrde pre brisanja stavki.\n\n" +
                "4. INFORMACIJE O APLIKACIJI:\n" +
                "• Prikaz verzije aplikacije i tehničkih podataka (bez mogućnosti izmene).\n\n" +
                "5. UVOZ PODATAKA IZ LEGACY DOS SISTEMA:\n" +
                "• Dugme koje otvara čarobnjak za uvoz — detaljno objašnjeno u temi '🔄 Uvoz iz legacy DOS / Clipper sistema'.",
            Kljuc = "Podesavanja"
        },
        new PomocTema
        {
            Naslov = "💾 Rezervne kopije (Backup & Restore)",
            Sadrzaj =
                "Meni '💾 Rezervne kopije' omogućava zaštitu podataka pravljenjem i vraćanjem rezervnih kopija baze.\n\n" +
                "1. RUČNI BACKUP:\n" +
                "• Dugme '💾 Napravi ručni backup' odmah kreira kopiju tekuće baze podataka.\n\n" +
                "2. AUTOMATSKI BACKUP:\n" +
                "• Učestalost se bira između tri opcije: 'Nikada', 'Pri svakom izlasku iz aplikacije' i 'Jednom dnevno'.\n\n" +
                "3. VRAĆANJE (RESTORE):\n" +
                "• Dugme '📥 Vrati iz fajla' učitava proizvoljnu rezervnu kopiju sa diska.\n" +
                "• Lista istorije backup-a nudi 'Vrati' i 'Izbriši' za svaku pojedinačnu kopiju.\n\n" +
                "4. LOKACIJA KOPIJA:\n" +
                "• Kopije se čuvaju u podfolderu 'Baze\\RezervneKopije' unutar foldera aplikacije.\n\n" +
                "5. PRISTUP I PREPORUKA:\n" +
                "• Ova funkcija je dostupna samo administratorima. Preporučuje se ručni backup pre rizičnih operacija poput rasknjižavanja, prenosa u novu poslovnu godinu ili uvoza iz DOS sistema.",
            Kljuc = "Backup"
        },
        new PomocTema
        {
            Naslov = "👤 Korisnici i Uloge — administracija naloga",
            Sadrzaj =
                "Meni '👤 Korisnici i Uloge' služi za upravljanje korisničkim nalozima. Za opis šta svaka uloga sme da radi pogledajte temu '🔐 Prijava, korisnici i bezbednost' — ova tema objašnjava samo kako se nalozi kreiraju i menjaju na ovom ekranu.\n\n" +
                "1. NOVI KORISNIK:\n" +
                "• Dugme '➕ Novi korisnik' otvara formu: korisničko ime, ime i prezime, izbor uloge (Administrator / Knjigovođa / Gledalac), lozinka i CheckBox 'Nalog je aktivan'.\n\n" +
                "2. IZMENA POSTOJEĆEG KORISNIKA:\n" +
                "• Polje lozinke se pri izmeni po pravilu ostavlja prazno — u tom slučaju postojeća lozinka ostaje nepromenjena; unosi se samo ako se lozinka zaista menja.\n\n" +
                "3. DEAKTIVACIJA:\n" +
                "• Umesto brisanja naloga, preporučuje se skidanje CheckBox-a 'Nalog je aktivan' čime korisnik gubi mogućnost prijave, ali istorija njegovih radnji u audit logu ostaje sačuvana.",
            Kljuc = "Korisnici"
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

    public PomocView(string? initijalnaTema = null)
    {
        InitializeComponent();
        LstTeme.ItemsSource = _teme;

        var tema = initijalnaTema is not null ? _teme.FirstOrDefault(t => t.Kljuc == initijalnaTema) : null;
        LstTeme.SelectedItem = tema ?? (_teme.Count > 0 ? _teme[0] : null);
    }

    private void LstTeme_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LstTeme.SelectedItem is PomocTema tema)
        {
            TxtNaslovTeme.Text = tema.Naslov;
            TxtSadrzajTeme.Text = tema.Sadrzaj;
        }
    }

    private void TxtPretragaTema_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        var upit = TxtPretragaTema.Text?.Trim() ?? string.Empty;
        var prethodnaSelekcija = LstTeme.SelectedItem as PomocTema;

        var filtrirano = upit.Length == 0
            ? _teme
            : _teme.Where(t =>
                t.Naslov.Contains(upit, StringComparison.OrdinalIgnoreCase) ||
                t.Sadrzaj.Contains(upit, StringComparison.OrdinalIgnoreCase)).ToList();

        LstTeme.ItemsSource = filtrirano;

        if (prethodnaSelekcija is not null && filtrirano.Contains(prethodnaSelekcija))
            LstTeme.SelectedItem = prethodnaSelekcija;
        else if (filtrirano.Count > 0)
            LstTeme.SelectedIndex = 0;
        else
        {
            TxtNaslovTeme.Text = "Nema rezultata";
            TxtSadrzajTeme.Text = "Nijedna tema pomoći ne odgovara pretrazi.";
        }
    }
}
