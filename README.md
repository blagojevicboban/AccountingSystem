# 💼 AccountingSystem — Finansijsko knjigovodstvo i glavna knjiga

> Desktop ERP aplikacija za finansijsko knjigovodstvo (glavna knjiga, nalozi za knjiženje, kontni plan, kartice konta), analitiku partnera (otvorene stavke, IOS, kamate), magacinsko poslovanje po prosečnoj ceni i trgovinu — razvijena u **C# / .NET 8 / WPF**, po uzoru na [SredstvaSystem](https://github.com/blagojevicboban/AssetManager) i modelovana kao zamena za legacy DOS/Clipper sistem.

---

## ✨ Funkcionalnosti

### Glavna knjiga (FIN)
- 📊 **Radna tabla** — pregled ključnih brojki (nalozi, konta, artikli, partneri) i poslednjih naloga.
- 📖 **Nalozi za knjiženje** — unos/izmena naloga sa **brzom pretragom konta (`F2`)**, **brzim unosom sa tastature** (`Insert`/`Enter`/`Tab`), **smart auto-balansom** i živom proverom ravnoteže (Duguje == Potražuje), knjiženje, **masovno knjiženje** (`⚡ Proknjiži sve`), **masovno preknjižavanje konta** (`🔄 Preknjižavanje`) i **rasknjižavanje** (samo Administrator, uz potvrdu — vraća nalog u nacrt radi ispravke, odbija naloge iz godine za koju je već napravljen prenos početnog stanja, i upisuje trag ko/kada u `NalogAudit`).
- 📋 **Kontni plan i kartice konta** — namensko upravljanje kontnim planom (`📋 Kontni plan` - unos novog konta, izmena, brisanje, PDF štampa), filter konta sa prometom (`[x] Samo konta sa knjiženjima`), hronološka kartica konta sa kumulativnim saldom.
- 📅 **Nova godina** — prenos zaključnog salda svih konta u nalog za početno stanje naredne godine; **odbija prenos ako knjige nisu u ravnoteži** (bezbednosna provera pre nego što se nešto pogrešno prenese).

### Partneri i analitika (ANAL)
- 👥 **Partneri — otvorene stavke i IOS** — praćenje dugovnih stavki po partneru (kupci/dobavljači), PDF **IOS obrazac**.
- 💰 **Kamate** — obračun zatezne kamate po danu kašnjenja, sa podrškom za više kamatnih stopa kroz vreme; unos novih stopa iz aplikacije.
- 📊 **Bruto bilans analitike** — promet i saldo po partneru (paralelno finansijskom bruto bilansu po kontu).

### Zvanični Finansijski Izveštaji za APR (Bilansi)
- 🏛️ **Bilans Stanja** — Obračun AOP pozicija za Aktivu (Klasa 0–2) i Pasivu (Klasa 3–4) na izabrani datum, uz automatsku proveru ravnoteže (`Aktiva == Pasiva`).
- 📈 **Bilans Uspeha** — Obračun AOP pozicija za Poslovne prihode (Klasa 6), Poslovne rashode (Klasa 5), Finansijske prihode/rashode i obračun Neto dobitka ili gubitka perioda.
- 📥 **PDF Štampa Bilansa** — Izvoz zvaničnih PDF izveštaja Bilansa Stanja i Bilansa Uspeha.

### SEF e-Fakture (Sistem Elektronskih Faktura RS)
- ⚡ **Direktna SEF API konekcija** — Slanje izlaznih e-faktura direktno na SEF portal (`POST /sales-invoice/ubl`), provera statusa u realnom vremenu i preuzimanje ulaznih e-faktura dobavljača.
- 📄 **UBL 2.1 XML Generator** — Zvaničan XML format po specifikaciji e-Faktura RS (`urn:cen.eu:en16931:2017#compliant#urn:mfin.gov.rs:srbdt:2021`).
- 🔑 **Podešavanja po firmi** — Unos API ključa, izbor Demo vs Produkcionog okruženja, JBKJS broja i dugme za testiranje konekcije.

### PDV Evidencija (KIR, KPR, POPDV i ePorezi PP-PDV)
- 🧾 **KIR (Knjiga Izdatih Računa)** i **KPR (Knjiga Primljenih Računa)** — Automatsko prikupljanje izlaznih računa i ulaznih kalkulacija sa raščlanjavanjem osnovice i PDV-a (20%, 10%, 0%).
- ⚖️ **POPDV Rekapitulacija Obaveze** — Obračun PDV obaveze za uplatu ili prava na povraćaj.
- 📄 **ePorezi PP-PDV XML Izvoz** — Generisanje zvanične XML prijave Obrasca PP-PDV koja se direktno učitava na portal Poreske uprave RS (`eporezi.purs.gov.rs`).

### Magacin i Trgovina (MAT & ROB)
- 📊 **Radna tabla (Robno i Materijalno)** — po jedna namenska radna tabla za svaki modul (vrednost zaliha VP/MP odnosno materijala, upozorenje na negativna stanja, poslednji dokumenti i brze akcije za nov unos).
- 📦 **Magacin i zalihe** — Materijalne kartice po ponderisanoj prosečnoj ceni (`M1`), unos/izmena materijala (`MaterijalEditWindow`), provera i rekalkulacija salda zaliha (`ProveraKarticaWindow`), Ulazi (`M2`), Trebovanja (`M3`) i **Primopredaje / Interni prenosi (`M4`)** (razlikuje Primopredaja/Zaduženje/Razduženje).
- 🛒 **Trgovina i Fakture** — Veleprodajne (`MAT6`) i Maloprodajne (`MAT3`) kalkulacije (sa direktnom PDF štampom), **Računi-Otpremnice / Fakture (`MAT5`)** sa rokom dospelosti, rabatom i PDV-om (unos stavki po šifri artikla), **Predračun** (isti ekran, čekboks "Predračun" — dokument sa rokom važenja koji se ne knjiži i jednim klikom ("🔁 Pretvori u račun") pretvara u pravu fakturu), i **Nivelacije cena (`MAT7`)** sa automatskom generacijom svođenjem na prosečnu nabavnu cenu, zbirnom PDF štampom i masovnim knjiženjem.
- 🔎 **Filter Svi / Proknjiženi / Neproknjiženi** — dostupan na svim tabovima gde se knjiži, i u Robnom (Zaduženja, Razduženja, Primopredaje, Kalkulacije, Računi-Otpremnice, Nivelacije) i u Materijalnom (Ulazi, Trebovanja, Primopredaje) knjigovodstvu.
- 🔓 **Rasknjižavanje dokumenata (Robno i Materijalno)** — kao i kod naloga glavne knjige: klik na 'Izmeni' nad proknjiženim dokumentom nudi rasknjižavanje (samo Administrator), sa bezbednosnom proverom da za taj artikal/magacin nije u međuvremenu knjiženo nešto kasnije (štiti tačnost prosečne cene zaliha).
- 🧾 **Poreske tarife** — samostalan šifarnik poreskih stopa (tarifni broj, porez %, poseban porez %) sa CRUD ekranom i PDF štampom.
- 📊 **Robni i Materijalni Bruto Bilans (`BRUTO BILANS MATERIJALNOG KNJIGOVODSTVA`)** — početno stanje/ulaz/izlaz/stanje po magacinu i artiklu, količinski i vrednosno sa slažećim zbirom u paru sa Clipper izveštajima (`M1.PRG` / `st_mat_bruto()`), višestrukom štampom robnih kartica (više artikala ili svi magacini), izveštajem „Raspored artikala (analitika MAT91)” i sintetičkim izveštajem „Stanje po artiklima (sintetika MAT92)”.
- 📊 **Excel (XLSX) Izvoz** — jednim klikom izvoz svih 15 tabova robnog i materijalnog poslovanja u Excel sa udesno poravnatim numeričkim kolonama i automatskim `=SUM(...)` zbirnim formulama.
- 🏢 **Šifarnik artikala i Računopolagača** — samostalni CRUD ekrani (`MAT1`, `MAT2`) sa PDF štampom.

### Zajedničko
- 🔐 **Prijava i uloge** — lozinke osoljene (PBKDF2), uloga Administrator za osetljive operacije (rasknjižavanje, nova godina).
- 🏢 **Rad sa više firmi** — svaka firma ima sopstvenu SQLite bazu podataka.
- 🔄 **Uvoz iz DOS sistema (`AccountingMigration`)** — uvozi kontni plan, naloge, partnere, materijale, magacine, ulaze, trebovanja, kartice i kamatne stope iz legacy dBase III / Clipper fajlova (`C:\KNJIGE\Radni\KORxx`).
- 📄 **PDF izveštaji (`QuestPDF`)** — dnevnik glavne knjige, bruto bilans (finansijski i analitike), kartica konta, IOS, kamata, izveštaj o zalihama (u Portrait A4 formatu).
- ❓ **Pomoć** — uputstvo za korišćenje ugrađeno u samu aplikaciju (tab „Pomoć" u sidebar-u).

---

## 🛠️ Tehnologije

| Oblast | Tehnologija |
| --- | --- |
| **Jezik** | C# 12 / .NET 8.0 |
| **UI okvir** | WPF (Windows Presentation Foundation), code-behind (bez MVVM-a) |
| **Baza podataka** | SQLite (po jedna instanca po firmi) |
| **ORM** | Entity Framework Core 8 (isključivo EF Core migracije, ne `EnsureCreated`) |
| **Izveštaji / PDF** | QuestPDF |
| **Izvoz u Excel** | ClosedXML (XLSX izvoz sa formulama) |
| **Legacy DBF parser** | Sopstveni binarni dBase III parser (Latin1 / YUSCII / CP852) |
| **Pakovanje / Update** | Velopack |
| **Testiranje** | xUnit (kalkulatori/servisi bez zavisnosti od baze — in-memory EF provider za servise koji zavise od baze) |

---

## 📁 Struktura projekta

```text
AccountingSystem/
├── AccountingApp/                  # Glavni WPF desktop projekat
│   ├── Views/
│   │   ├── Korisnici/              # Prijava (Login)
│   │   ├── Dashboard/              # Radna tabla
│   │   ├── Nalozi/                 # Glavna knjiga — nalozi, unos/izmena, rasknjižavanje, nova godina
│   │   ├── Kartice/                # Kartice konta
│   │   ├── Partneri/               # Otvorene stavke, IOS, Kamate
│   │   ├── Magacin/                 # Materijalne kartice, Ulazi, Trebovanja
│   │   ├── Trgovina/                # Kalkulacija veleprodaje
│   │   ├── Izvestaji/               # Izveštaji i PDF
│   │   └── Pomoc/                   # Uputstvo ugrađeno u aplikaciju
│   ├── Services/                    # PdfReportService
│   ├── AppSession.cs                # Trenutno ulogovan korisnik i aktivna firma
│   └── AppConfig.cs                 # Putanja do baze i podešavanje okruženja
├── AccountingData/                  # Sloj za pristup podacima (EF Core modeli i DbContext)
│   ├── Models/                      # Firma, Korisnik, Konto, Nalog, StavkaNaloga, Partner, Artikal, Magacin, KamatnaStopa, Kalkulacija...
│   ├── Migrations/                  # EF Core migracije šeme baze
│   └── Services/                    # NaloziService, KarticaService, OtvoreneStavkeService, KamataService, NovaGodinaService, BrutoBilansService, MaterijalnaKarticaService, UlazService, TrebovanjeService, KalkulacijaService
├── AccountingData.Tests/            # xUnit testovi (formule/kalkulatori + servisi sa in-memory EF)
├── AccountingMigration/             # Konzolni alat za uvoz legacy DOS/Clipper DBF podataka
├── ANALIZA_I_PLAN.md                # Analiza legacy Clipper sistema i fazni plan razvoja (istorijat odluka)
└── .vscode/                         # VS Code launch.json i tasks.json za F5 debagovanje
```

---

## 🚀 Brzi početak

```bash
# 1. Prevesti projekat
dotnet build AccountingSystem.slnx

# 2. Pokrenuti aplikaciju
dotnet run --project AccountingApp/AccountingApp.csproj

# 3. Pokrenuti unit testove
dotnet test AccountingData.Tests/AccountingData.Tests.csproj
```

> **Napomena:** Podrazumevana prijava je **admin / admin123** (zasejano preko EF Core migracije). Baza se automatski kreira i migrira pri prvom pokretanju (`AccountingDbContext.Create`). Za uvoz podataka iz legacy DOS sistema pokrenite `AccountingMigration` projekat — **napomena:** taj alat briše i ponovo kreira bazu podataka firme pri svakom pokretanju (namenjen je uvozu/reimportu test podataka, ne za rad sa produkcionim podacima).

### Vožnja i testiranje UI-ja (za agente)

Za automatizovano pokretanje, prijavljivanje i snimanje ekrana aplikacije pogledajte
[`AccountingApp/.claude/skills/run-accounting-app/SKILL.md`](AccountingApp/.claude/skills/run-accounting-app/SKILL.md).

---

## 🔒 Napomene o bazi podataka

- Lokalni `*.db` fajlovi sa podacima firme nisu deo Git repozitorijuma.
- Svaka firma ima sopstvenu SQLite bazu (test firma je **KOR01** — ARHIBEL 2026).
- Šema baze se upravlja isključivo kroz **EF Core migracije** (`dotnet ef migrations add ...`), ne kroz `EnsureCreated`.

## ⚠️ Poznata ograničenja

- **Kamatne stope** uvezene iz legacy sistema su istorijske (poslednja je iz 2006. godine) — pre obračuna kamate na tekućim dugovanjima unesite aktuelnu zvaničnu stopu kroz ekran „Kamate".
- **Partneri (Analitika)** rade preko `StavkaNaloga.PartnerId`, koji se dodeljuje ručno pri unosu naloga — istorijski uvezeni nalozi iz DOS sistema nemaju dodeljene partnere (legacy ANAL modul za test firmu nije korišćen).

---
*Aplikacija služi za zamenu nasleđenog Clipper MS-DOS sistema (moduli FIN, ANAL, ROB, MAT) i razvija se po uzoru na [SredstvaSystem](https://github.com/blagojevicboban/AssetManager). Detaljan istorijat analize i faznog razvoja je u [ANALIZA_I_PLAN.md](ANALIZA_I_PLAN.md).*
