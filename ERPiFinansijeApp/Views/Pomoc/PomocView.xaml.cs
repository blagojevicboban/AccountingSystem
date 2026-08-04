using System;
using System.Linq;
using System.Windows.Controls;

namespace ERPiFinansijeApp.Views.Pomoc;

public partial class PomocView : UserControl
{
    private readonly List<PomocTema> _teme = new()
    {
        new PomocTema
        {
            Naslov = "📜 Komercijala — Ponude, Predračuni i Narudžbenice",
            Sadrzaj =
                "1. PONUDE I PREDRAČUNI (PROFORMA FAKTURE):\n" +
                "• Izdavanje ponuda kupcima sa automatskim obračunom rabata i PDV-a.\n" +
                "• Klikom na dugme '🚀 Pretvori u Fakturu & SEF', sistem u 1 klik kreira novi izlazni račun (RacunOtpremnica) spreman za slanje na e-Fakture (SEF).\n\n" +
                "2. NARUDŽBENICE DOBAVLJAČIMA (PURCHASE ORDERS):\n" +
                "• Evidencija ugovorene robe sa dobavljačima i praćenje rokova isporuke.\n" +
                "• Klikom na dugme '📥 Pretvori u Kalkulaciju', naručeni artikli se automatski prenose u novu ulaznu kalkulaciju sa poređenjem naručenih i pristiglih količina."
        },
        new PomocTema
        {
            Naslov = "🔍 AI / OCR Čitač skeniranih računa u DMS-u",
            Sadrzaj =
                "1. EKSITRAKCIJA PODATAKA SA ULAZNIH RAČUNA:\n" +
                "• Klikom na dugme '🔍 OCR Nalog' uz bilo koji skenirani PDF ili slikovni ulazni račun u DMS-u, sistem vrši automatsko parsiranje teksta.\n" +
                "• Pametni OCR mehanizam izvuče PIB dobavljača, broj računa, datum izdavanja, valutu dospelosti, osnovicu (neto), PDV iznos (20%/10%) i ukupan iznos za uplatu (bruto).\n\n" +
                "2. UPARIVANJE SA PARTNERIMA I PRIPREMA NALOGA KNJIŽENJA:\n" +
                "• Izvučeni PIB se automatski uparuje sa šifarnikom Partneri u bazi.\n" +
                "• Klikom na dugme '🚀 Pripremi nalog knjiženja', sistem automatski kreira uravnotežene stavke naloga:\n" +
                "  - Duguje Konto 5010 / 5390: Nabavna vrednost / Usluge (Osnovica / Neto)\n" +
                "  - Duguje Konto 2700: Prethodni PDV po opštoj stopi (PDV iznos)\n" +
                "  - Potražuje Konto 4350: Dobavljači u zemlji (Ukupan bruto iznos uz povezivanje PartnerId)."
        },
        new PomocTema
        {
            Naslov = "🏦 Uvoz elektronskih bankarskih izvoda",
            Sadrzaj =
                "1. PODRŽANI FORMATI BANKARSKIH IZVODA:\n" +
                "• Halcom E-Bank XML (*.xml)\n" +
                "• Asseco / Office Banking XML (*.xml)\n" +
                "• ISO 20022 CAMT.053 XML (*.xml)\n" +
                "• SWIFT MT940 tekstualni izvod (*.txt, *.sta, *.940)\n\n" +
                "2. AUTOMATSKI MATCHING ENGINE (PAMETNO UPARIVANJE):\n" +
                "Sistem automatski analizira svaku stavku uvezenog izvoda i primenjuje 3 nivoa prepoznavanja:\n" +
                "• Nivo 1: Uparivanje nalogodavca/primaoca po PIB-u ili tekućem računu sa šifarnikom partnera.\n" +
                "• Nivo 2: Prepoznavanje poziva na broj / svrhe doznake i uparivanje sa izdatim fakturama ili stavkama naloga.\n" +
                "• Nivo 3: Prepoznavanje provizije i troškova platnog prometa (automatski dodeljuje Konto 5530).\n\n" +
                "3. AUTOMATSKO KNJIŽENJE I IOS ZATVARANJE:\n" +
                "Klikom na dugme 'Proknjiži izvod i zatvori stavke':\n" +
                "• Kreira se proknjiženi nalog vrste 'IZV' sa uravnoteženim stavkama za tekući račun (Konto 2410).\n" +
                "• Automatski se zatvaraju otvorena dugovanja i potraživanja kupaca/dobavljača u sistemu otvorenih stavki (IOS)."
        },
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
                "• Maloprodajna kalkulacija: Obračun ukalkulisane marže i PDV-a za prodaju fizičkim licima.\n" +
                "• Artikal se bira iz šifarnika (lista 'šifra - naziv'), a konto dobavljača iz kontnog plana — traži se i po broju i po nazivu. Unose se tri datuma: kalkulacije, otpremnice i računa.\n" +
                "• Knjiženje pravi i nalog u Glavnoj knjizi. Veleprodaja: roba (1320) duguje po prodajnoj vrednosti BEZ PDV, razlika u ceni (1329) i dobavljač potražuju. Maloprodaja ima 'korak više' — roba (1340) duguje po ceni SA PDV, a potražuju ukalkulisani PDV (1344), ukalkulisana razlika u ceni (1348) i dobavljač.\n" +
                "• Nalog pokriva robnu stranu i obavezu prema dobavljaču u neto iznosu. Pretporez i bruto obaveza po ulaznom računu knjiže se zasebno.\n" +
                "• Kalkulacija bez konta dobavljača se knjiži u magacin, ali bez naloga — bez protivstavke nalog ne bi bio u ravnoteži.\n" +
                "• Rasknjižavanje uklanja i taj nalog.\n\n" +
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
            Naslov = "🧾 PDV Evidencija (KPR, KIR i PP-PDV XML)",
            Sadrzaj =
                "1. PDV EVIDENCIJA (KPR i KIR):\n" +
                "• Knjiga primljenih računa (KPR) — uvoz nabavki i ulaznog PDV-a koji se odbija.\n" +
                "• Knjiga izdatih računa (KIR) — uvoz izlaznih faktura i izlaznog PDV-a.\n" +
                "• Automatska priprema podataka za POPDV prijavu Poreskoj upravi.\n\n" +
                "2. IZVOZ XML PRIJAVE ZA ePOREZI (PP-PDV):\n" +
                "• Dugme '📄 Izvezi XML za ePorezi (PP-PDV)' kreira zvanični XML fajl Obrasca PP-PDV po specifikaciji Poreske uprave RS.\n" +
                "• XML obuhvata sve propisane pozicije Obrasca PP-PDV: Promet i obračunati PDV (Polja 001-008, 101-108), Prethodni PDV (Polja 009-010, 109-110) i Konačni obračun obaveze ili povraćaja (Polja 111-113).\n" +
                "• Pri izvozu, ako postoji preplata PDV-a, sistem nudi opredeljenje za povraćaj novca (Polje 113) ili vođenje iznosa kao poreskog kredita.\n" +
                "• Izvezeni XML fajl se direktno učitava na portalu ePorezi (eporezi.purs.gov.rs) bez potrebe za ručnim kucanjem brojeva.\n\n" +
                "3. ZVANIČNI APR BILANSI:\n" +
                "• Za Bilans stanja i Bilans uspeha pogledajte posebnu temu '🏛️ Bilansi (APR)'.",
            Kljuc = "Pdv"
        },
        new PomocTema
        {
            Naslov = "🏛️ Bilansi (APR) i Poreski Bilans (PB-1 / PDP / OA)",
            Sadrzaj =
                "Meni '🏛️ Bilansi (APR)' generiše zvanične finansijske izveštaje za predaju Agenciji za privredne registre i Poreskoj upravi RS.\n\n" +
                "1. FINANSIJSKI IZVEŠTAJI ZA APR (5 TABOVA):\n" +
                "• Bilans stanja — Imovina, Kapital i Obaveze na dan izveštavanja po AOP pozicijama.\n" +
                "• Bilans uspeha — Prihodi, Rashodi i Finansijski rezultat (Dobitak/Gubitak).\n" +
                "• Statistički izveštaj (SI) — Opšti i statistički podaci za APR i Poresku upravu.\n" +
                "• Tokovi gotovine (Cash Flow) — Prilivi i odlivi iz poslovnih, investicionih i finansijskih aktivnosti.\n" +
                "• Promene na kapitalu — Matrica promena osnovnog kapitala, rezervi i neraspoređene dobiti u toku godine.\n\n" +
                "2. PORESKI BILANS I PRIJAVA POREZA NA DOBIT:\n" +
                "• Klikom na dugme '📜 Poreski Bilans (PB-1 / PDP / OA)' otvara se prozor za usklađivanje dobiti i obračun poreza na dobit (15%).\n" +
                "• Obrazac PB-1 — Usklađivanje rashoda (nepriznati rashodi po čl. 7, 7a, 8, 9, 15, 16 Zakona o porezu na dobit) i prihoda.\n" +
                "• Obrazac OA — Obračun poreske amortizacije po I–V grupama (Stopa 2.5% do 30%) i poređenje sa računovodstvenom amortizacijom.\n" +
                "• Obrazac PDP — Poreska prijava sa obračunatim porezom na dobit i predloženim mesečnim akontacijama.\n\n" +
                "3. OSVEŽAVANJE I IZVOZ:\n" +
                "• Dugme '🔄 Osveži obračun' izračunava sve vrednosti iz proknjiženih naloga, a dugmad '🖨️ PDF' i 'X Excel' izvoze izveštaje.",
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
            Naslov = "📎 DMS — Prilozi uz naloge i skenirani dokumenti",
            Sadrzaj =
                "Sistem za upravljanje dokumentima (DMS — Document Management System) omogućava prilaganje i pregled skeniranih ulaznih računa, ugovora i prateće dokumentacije.\n\n" +
                "1. PRILAGANJE DOKUMENATA UZ NALOG:\n" +
                "• U prozoru naloga knjiženja kliknite na dugme '📎 Prilozi (DMS)'.\n" +
                "• Dugme '➕ Priloži dokument' omogućava izbor PDF fajlova ili slika (JPG/PNG) sa vašeg računara.\n\n" +
                "2. SKLADIŠTENJE I BEZBEDNOST:\n" +
                "• Svi priloženi dokumenti se automatski kopiraju u bezbedni podfolder 'DMS/Dokumenti' i povezuju sa nalogom u bazi podataka.\n\n" +
                "3. PREGLED I BRISANJE:\n" +
                "• Klikom na '👁️ Otvori' dokument se prikazuje direktno u sistemskom PDF/Pregledniku slika.\n" +
                "• Klikom na '🗑️ Briši' prilog se uklanja iz baze i sa diska.",
            Kljuc = "Dms"
        },
        new PomocTema
        {
            Naslov = "🌐 REST API & Web Dashboard (Mobilni uvid)",
            Sadrzaj =
                "Ugrađeni Web Server omogućava uvid u finansijske pokazatelje, naloge i partnere sa mobilnih telefona, tableta i web pregledača.\n\n" +
                "1. POKRETANJE SERVERA:\n" +
                "• U meniju '⚙️ Podešavanja' izaberite tab '🌐 REST API & Web Dashboard'.\n" +
                "• Kliknite na '▶️ Pokreni Web Server' (podrazumevani port: 5050).\n\n" +
                "2. WEBDASHBOARD SA TELEFONA:\n" +
                "• U bilo kom pregledaču (Chrome, Safari, Firefox) otvorite URL prikazan u podešavanjima (npr. http://localhost:5050 ili IP adresa računara na mreži).\n" +
                "• Web Dashboard prikazuje finansije uživo (prihode, rashode, neto dobit, broj naloga i partnera).\n\n" +
                "3. REST API ENDPOINTS:\n" +
                "• GET /api/status — Status servisa.\n" +
                "• GET /api/dashboard — Financijski KPI indikatori.\n" +
                "• GET /api/partneri — Lista partnera.",
            Kljuc = "WebApi"
        },
        new PomocTema
        {
            Naslov = "💱 Devizno knjiženje i Obračun kursnih razlika",
            Sadrzaj =
                "Modul deviznog knjigovodstva omogućava evidenciju naloga i faktura u inostranim valutama (EUR, USD, CHF, GBP) uz praćenje dinarske protivvrednosti.\n\n" +
                "1. VIŠEVALUTNO KNJIŽENJE:\n" +
                "• U nalogu knjiženja unesite valutu (npr. EUR), važeći kurs i devizni iznos (Duguje/Potražuje).\n" +
                "• Sistem automatski preračunava i knjiži odgovarajući dinarski iznos po NBS kursu.\n\n" +
                "2. VALVIRANJE I KURSNE RAZLIKE:\n" +
                "• Na dan bilansa (31.12. ili kraj meseca) u dijalogu 'Devizno valviranje' pokrenite automatski proračun kursnih razlika.\n" +
                "• Pozitivne kursne razlike se automatski knjiže na Konto 6630 (Prihodi od kursnih razlika).\n" +
                "• Negativne kursne razlike se knjiže na Konto 5630 (Rashodi od kursnih razlika).",
            Kljuc = "Devizno"
        },
        new PomocTema
        {
            Naslov = "🛃 Ino-fakture i Uvozne kalkulacije",
            Sadrzaj =
                "Uvozne kalkulacije omogućavaju uvoz robe i materijala iz inostranstva sa kompletnim obračunom uvoznih troškova.\n\n" +
                "1. UNOS INO-FAKTURE:\n" +
                "• Unos broja ino-fakture, ino-dobavljača, deviznog iznosa i kursa na dan carinjenja.\n\n" +
                "2. ZAVISNI TROŠKOVI UVOZA:\n" +
                "• Unos iznosa carine (po stopi ili fiksno), špedicije, prevoza i ostalih troškova.\n" +
                "• Zavisni troškovi se automatski raspoređuju na uvozne nabavne cene artikala po vrednosti.\n\n" +
                "3. KNJIŽENJE U MAGACIN I GLAVNU KNJIGU:\n" +
                "• Automatsko knjiženje zaduženja magacina (Konto 1300 / 1010) i obaveza prema ino-dobavljaču (Konto 4350) i špediteru (Konto 4330).",
            Kljuc = "Uvoz"
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
            Naslov = "⚡ SEF e-Fakture (Sistem Elektronskih Faktura)",
            Sadrzaj =
                "Aplikacija poseduje ugrađenu direktnu integraciju sa državnim SEF portalom Ministarstva finansija RS.\n\n" +
                "1. PODEŠAVANJE SEF KONEKCIJE:\n" +
                "• U meniju '⚙️ Podešavanja' -> tab '⚡ SEF e-Fakture' unesite SEF API Ključ (ApiKey) izdat za vašu firmu.\n" +
                "• Izaberite okruženje: 'Demo' za testiranje ili 'Production' za slanje pravih zvaničnih faktura.\n" +
                "• Po potrebi unesite JBKJS broj (za budžetske korisnike) i E-mail adresu.\n" +
                "• Kliknite '⚡ Testiraj SEF Konekciju' da potvrdite ispravnost ključa.\n\n" +
                "2. SLANJE IZLAZNIH E-FAKTURA NA SEF:\n" +
                "• U meniju 'Trgovina / Fakture' selektujte željeni račun-otpremnicu i kliknite dugme '📤 Pošalji na SEF'.\n" +
                "• Aplikacija automatski generiše zvanični UBL 2.1 XML dokument po srpskom profilu e-Faktura i šalje ga na SEF.\n" +
                "• Polje 'SEF Status' u tabeli prikazuje trenutno stanje (Poslata, Odobrena, Odbijena).\n\n" +
                "3. PROVERA STATUSA I UBL XML IZVOZ:\n" +
                "• Dugme '🔄 SEF Status' proverava da li je kupac prihvatio ili odbio e-fakturu na SEF-u.\n" +
                "• Dugme '📄 UBL XML' sačuvava XML fajl fakture lokalno na vašem računaru.\n\n" +
                "4. PREUZIMANJE ULAZNIH E-FAKTURA DOBAVLJAČA:\n" +
                "• Klikom na dugme '📥 Ulazne SEF' otvara se prozor za preuzimanje i uvid u fakture koje su vam poslali dobavljači sa SEF-a.",
            Kljuc = "Sef"
        },
        new PomocTema
        {
            Naslov = "💱 Kursna lista NBS i Registar partnera",
            Sadrzaj =
                "Aplikacija je povezana sa web servisima Narodne banke Srbije (NBS) za kursne liste i registar računa.\n\n" +
                "1. PREGLED I PREUZIMANJE KURSNE LISTE:\n" +
                "• Klikom na dugme '💱 Kursna lista NBS' (u meniju Partneri) otvara se ekran sa zvaničnim dnevnim srednjim, kupovnim i prodajnim kursevima deviza (EUR, USD, CHF, GBP, BAM, RUB, JPY, itd.).\n" +
                "• Dugme '🔄 Preuzmi sa NBS' preuzima najsvežiju dnevnu kursnu listu direktno sa servera NBS i sačuvava je u bazi.\n\n" +
                "2. KALKULATOR KONVERZIJE VALUTA:\n" +
                "• U desnom delu ekrana dostupan je brzi kalkulator deviza: unesite iznos (npr. 1.000 EUR) i aplikacija će izračunati tačnu dinarsku protivvrednost (RSD) po srednjem kursu NBS za taj datum.\n\n" +
                "3. PROVERA TEKUĆIH RAČUNA PARTNERA U REGISTRU NBS:\n" +
                "• Selektujte partnera na listi i kliknite '🔍 Verifikuj račun (NBS)'. Aplikacija vrši proveru u Jedinstvenom registru računa NBS i prikazuje verifikovani žiro-račun i trenutni status naloga (npr. Aktivan ili U blokadi).",
            Kljuc = "Nbs"
        },
        new PomocTema
        {
            Naslov = "🧾 e-Fiskalizacija (ESIR / PFR) i Izdavanje Računa",
            Sadrzaj =
                "Aplikacija poseduje ugrađenu podršku za komunikaciju sa PFR / LPFR servisom (Procesor Fiskalnih Računa) Poreske uprave RS.\n\n" +
                "1. PODEŠAVANJE PFR KONEKCIJE:\n" +
                "• U meniju '⚙️ Podešavanja' -> tab '🧾 e-Fiskalizacija (PFR / ESIR)' unesite PFR URL (npr. http://localhost:8443) i PAC kod Bezbednosnog Elementa (BE).\n" +
                "• Kliknite '⚡ Testiraj PFR Konekciju' da potvrdite ispravnost veze sa kasenim uređajem ili lokalnim PFR servisom.\n\n" +
                "2. FISKALIZACIJA PROMET ROBA I USLUGA:\n" +
                "• U meniju 'Trgovina / Fakture' selektujte račun i kliknite dugme '🧾 Fiskalizuj (PFR)'.\n" +
                "• Izaberite način plaćanja: Gotovina (Cash), Platna kartica (Card) ili Prenos na račun (WireTransfer).\n" +
                "• Nakon slanja PFR vrši verifikaciju i vraća zvanični fiskalni broj računa, PFR brojač, fiskalni žurnal i verifikacioni URL sa QR kodom Poreske uprave RS (`suf.purs.gov.rs`).\n\n" +
                "3. PREGLED I ŠTAMPA FISKALNOG ISEČKA:\n" +
                "• Fiskalizovani računi dobijaju jedinstveni fiskalni broj u tabeli i status 'Fiskalizovan'. Prozor prikazuje žurnal i URL QR koda koji se štampa na fiskalnom računu.",
            Kljuc = "Pfr"
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
