# 📋 Istorija izmena (Changelog) — AccountingSystem

Sve značajne promene i novine u aplikaciji **AccountingSystem** dokumentovane su u ovom fajlu.

Format je zasnovan na [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) standardu i prati Semantic Versioning.

## [1.0.9] - 2026-07-25

### 🐛 Ispravka mapiranja DBF kolona pri uvozu (KONTPLAN, ANKONT, MAGACIN, ARTIKLI, NALOG)
- **Objedinjen uvoz u `DbfImportService`**: I uvoz iz aplikacije (`⚙️ Podešavanja`) i samostalni `AccountingMigration` alat sada koriste isti deljeni mapping kod, tako da se imena DBF kolona ne mogu razminuti između dva mesta.
- **Ispravljeno pogrešno mapiranje imena kolona**: Uvoz naloga (`NALOG.DBF`) i partnera (`ANKONT.DBF`) je tražio kolone koje ne postoje u ovim DBF fajlovima i tiho uvozio 0 redova; uvoz kontnog plana je čitao pogrešnu kolonu za broj konta (uvezeno je bilo samo ~42 sintetička konta umesto svih ~3200).
- **Dodate kolone koje su ranije bile odbačene**: Kontni plan sada čuva staru šifru konta, ulicu, mesto, žiro račun i telefon (iz KONTPLAN.DBF); artikli čuvaju klasifikacionu šifru i selektovan flag; stavke naloga čuvaju staru šifru konta i šifru promene.
- **Ispravljena UNIQUE constraint greška** u `AccountingMigration` alatu pri uvozu kontnog plana (KONTPLAN.DBF sadrži par dupliranih šifara konta) — dodata ista in-memory `HashSet` provera koja se već koristila u uvozu iz aplikacije.

---

## [1.0.8] - 2026-07-25

### 📋 Novi modul Kontni plan (`KontaView`) & Unos/Izmena/Štampa konta
- **Šifarnik kontnog plana (`📋 Kontni plan`)**: Novi zasebni modul u glavnoj navigaciji za rad sa kontnim planom (analogno DOS `gk1()` proceduri). Prikazuje celokupan šifarnik konta sa pretragom i sortiranjem.
- **Unos, izmena i brisanje konta (`KontoEditWindow`)**: Dodati dijalozi za unos novog konta (`➕ Novi konto`), izmenu naziva i vrste (`✏️ Izmeni`), kao i bezbedno brisanje konta (`🗑️ Obriši`) uz proveru postojeće proknjižene analitike.
- **PDF Štampa kontnog plana**: Implementirano generisanje i pregled PDF dokumenta celokupnog kontnog plana.

### 📖 Glavna knjiga & Nalozi knjiženja
- **Masovno knjiženje naloga (`⚡ Proknjiži sve`)**: Implementirana mogućnost knjiženja svih neproknjiženih naloga odjednom (analogno DOS `knjiz_f_naloga(0)`).
- **Preknjižavanje konta (`🔄 Preknjižavanje`)**: Implementiran alat za masovnu zamenu broja konta na stavkama naloga knjiženja u bazi (analogno DOS `prekm()` / `preknjizi()`).
- **Štampa selektovanih naloga (`🖨️ Štampa`)**: Dodato generisanje PDF naloga za knjiženje za jedan ili više izabranih naloga.
- **Filteri statusa naloga**: Zamenjen CheckBox RadioButton dugmadima (`Svi`, `Proknjiženi`, `Neproknjiženi`) uz prilagodljiv `WrapPanel` raspored bez preklapanja.

### 📋 Kartice konta & Podešavanja
- **Filter konta sa prometom (`[x] Samo konta sa knjiženjima`)**: Dodat filter na ekranu Kartice konta koji podrazumevano prikazuje samo konta koja imaju proknjiženi promet.
- **Premeštanje uvoza u Podešavanja**: Uvoz DOS podataka iz sporednog zaglavlja premešten u namensku sekciju u ekranu `⚙️ Podešavanja`.
- **Prečišćavanje uvoza naloga**: Filtriran interni brojač DOS Clipper sistema (Nalog `0`) i usmeren uvoz isključivo na `NALOG.DBF`.

---

## [1.0.7] - 2026-07-25

### 🚀 Zasebne SQLite baze po firmama (Per-firm Isolated Databases)
- **Izolacija baza po firmama (kao u SredstvaApp)**: Pri uvozu DOS podataka za svaku izabranu firmu kreira se ili osvežava **njena zasebna SQLite baza** u njenom folderu (npr. `C:\KNJIGE\Radni\KOR01\accounting_kor01.db`, `C:\KNJIGE\Radni\KOR06\accounting_kor06.db`).
- **Nezavisnost naloga i šifarnika**: Svi nalozi za knjiženje, konta, partneri, magacini i artikli se uvoze i skladište isključivo u posebnu bazu konkretne firme, bez mogućnosti ukrštanja ili preslikavanja podataka među firmama.

---

## [1.0.6] - 2026-07-25

### 🐛 Ispravke uvoza DOS/DBF podataka
- **Sprečavanje dupliranja šifara i UNIQUE constraint grešaka**: Ugrađena brza in-memory `HashSet` provera za sprečavanje dupliranja konta, partnera, magacina, artikala i naloga tokom uvoza više firmi.
- **Detaljniji prikaz izuzetaka**: Omogućen detaljan prikaz `InnerException` poruka u dijaloškom prozoru i dnevniku uvoza.

---

## [1.0.5] - 2026-07-25

### 🐛 Ispravke i vraćanje ikone
- **Vraćanje plave aktovke (`app.ico`)**: Vraćena originalna plava ikona sa slikom aktovke specifična za **AccountingSystem**.
- **Ispravka XAML greške pri pokretanju (`XamlParseException`)**: Uklonjen `<Content Include="app.ico">` iz `.csproj` koji je izazivao pad WPF loader-a pri čitanju resursa u `LoginWindow` i `MainWindow`.

---

## [1.0.4] - 2026-07-25

### 🎨 Poboljšanja ikone i prečica
- **High-Resolution ikona aplikacije (`app.ico`)**: Zamenjena ikona visoke rezolucije (sa svim dimenzijama od 16x16 do 256x256 piksela) i omoguceno njeno kopiranje u izlazni paket za pravilan prikaz prečica u Windows Start meniju i na traci zadataka.

---

## [1.0.3] - 2026-07-25

### ✨ Nove funkcionalnosti i poboljšanja
- **Automatski i Ručni Backup Sistem (`BackupService` & `BackupView`)**: Implementiran celoviti podsistem za ručno i automatsko pravljenje rezervnih kopija pri zatvaranju aplikacije ili na dnevnom nivou, rotaciju starih arhiva i sigurno obnavljanje baze podataka.
- **Ekran za Podešavanja Aplikacije (`PodesavanjaView`)**: Novi radni ekran za podešavanje lokacije baze, parametara štampanja PDF izveštaja (servis, potpisnik) i bezbednosnih upozorenja.
- **Administracija Korisnika i Uloga (RBAC)**: Ekrani `KorisniciView` i `KorisnikEditWindow` za upravljanje korisničkim nalozima, ulogama (*Administrator*, *Knjigovođa*, *Gledalac*), PBKDF2 heširanjem lozinki i sistemom ograničavanja prava pristupa.
- **Pravi Uvoz / Migracija DOS i DBF Podataka (`DosImportService` & `DosImportWindow`)**: Izrađen modul sa podrškom za **višestruki izbor i masovni uvoz više firmi odjednom (Multi-firm batch import)** iz starih FoxPro/DOS tabela (`KONTPLAN`, `ANKONT`, `MAGACIN`, `ARTIKLI`, `NALOGI`, `NALSTAV`).

---

## [1.0.2] - 2026-07-25

### ✨ Nove funkcionalnosti i poboljšanja
- **Automatsko ažuriranje (Velopack)**: Integrisana pozadinska provera i dijaloški prozor `UpdateDialog` za preuzimanje i instalaciju novih verzija pri pokretanju `AccountingApp` aplikacije.
- **Upravljanje firmama (`FirmeView`)**: Implementiran kompletan modul za pregled, filtriranje, unos i izmenu matičnih podataka firmi, te trenutni izbor aktivne firme.

---

## [1.0.1] - 2026-07-24

### 🔧 Poboljšanja i ispravke
- **Konfiguracija ikone aplikacije**: Povezana ikona `app.ico` u svim slojevima aplikacije (`.csproj`, WPF prozori, Velopack instalacioni paket).
- **Podešavanje okruženja za prevođenje i publikovanje**: Usaglašeni VS Code zadaci i `launch.json` za brzo pokretanje i debagovanje preko `F5`.
- **Integracija AI veština**: Iskopirane i prilagođene sve AI veštine iz `SredstvaSystem` radnog okruženja.

---

## [1.0.0] - 2026-07-24

### ✨ Nove funkcionalnosti

- **Temelji (prijava, sesija, baza)**:
  - EF Core migracije umesto `EnsureCreated` — šema baze se od sada isključivo upravlja preko migracija.
  - Prijava u sistem sa PBKDF2 (osoljenim) heš lozinkama, sesija sa trenutnim korisnikom i aktivnom firmom.
  - Podrazumevani administratorski nalog zasejan preko migracije (`admin` / `admin123`).

- **Glavna knjiga (Nalozi za knjiženje)**:
  - Unos i izmena naloga sa **živom proverom ravnoteže** (Duguje == Potražuje).
  - Knjiženje i **rasknjižavanje** naloga (samo Administrator, uz potvrdu) — omogućava ispravku već proknjiženih naloga.
  - **Prenos u novu godinu** — automatski obračun i knjiženje početnog stanja svih konta za narednu godinu, sa bezbednosnom proverom da knjige moraju biti u ravnoteži pre prenosa.

- **Kartice konta** — hronološki pregled prometa i kumulativnog salda po kontu, sa PDF izvozom.

- **Partneri (Analitika)**:
  - Otvorene stavke po partneru i PDF **IOS obrazac** (Izvod Otvorenih Stavki).
  - **Obračun zatezne kamate** po danu kašnjenja, sa podrškom za više kamatnih stopa kroz vreme.
  - **Bruto bilans analitike** — promet i saldo po partneru.

- **Magacin i zalihe**:
  - **Materijalne kartice po ponderisanoj (prosečnoj) ceni** — algoritam validiran replay-em stvarnih istorijskih podataka protiv legacy snapshota.
  - Ulazi i trebovanja materijala, sa knjiženjem i zaštitom od negativnog stanja na zalihama.

- **Trgovina i fakture**:
  - **Kalkulacija veleprodaje** (nabavna vrednost + zavisni troškovi → trgovačka marža → PDV → prodajna vrednost), sa live obračunom tokom unosa.

- **Izveštaji i PDF** — dnevnik glavne knjige, bruto bilans (finansijski i analitike), kartica konta, IOS, kamata, izveštaj o zalihama.

- **Uvoz iz DOS sistema** (`AccountingMigration`) — automatski uvoz kontnog plana, naloga, partnera, materijala, magacina, ulaza, trebovanja, kartica i kamatnih stopa iz legacy dBase III / Clipper fajlova.

- **Lokalizacija** — kompletan korisnički interfejs preveden na srpski jezik.

- **Pomoć ugrađena u aplikaciju** — uputstvo za korišćenje po modulima, dostupno direktno iz sidebar-a (bez potrebe za eksternom dokumentacijom).

### 🏗️ Arhitektura

- Analitika partnera (otvorene stavke) vezana je preko `StavkaNaloga.PartnerId`, umesto paralelne ANAL strukture iz legacy DOS sistema — glavna knjiga i analitika su objedinjene u istim tabelama.
- Materijalne kartice koriste jedinstven servis nezavisan od vrste artikla (roba/materijal), pa je spreman za dalje širenje i na robni promet.

### 📚 Dokumentacija

- `README.md` — pregled funkcionalnosti, tehnologija i strukture projekta.
- `ANALIZA_I_PLAN.md` — analiza legacy Clipper sistema (moduli FIN/ANAL/ROB/MAT) i detaljan istorijat faznog razvoja sa obrazloženjima odluka.
- `run-accounting-app` skill — UI-automation vodič za pokretanje i testiranje aplikacije.
