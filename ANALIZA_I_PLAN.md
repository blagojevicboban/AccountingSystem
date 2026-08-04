# ERPiFinansije — Analiza legacy Clipper sistema i plan razvoja

> Nastalo iz analize `.PRG` modula u `C:\KNJIGE\Radni` (FIN.CLP, ANAL.CLP, ROB.CLP, MAT.CLP)
> i struktura `.DBF` baza u `C:\KNJIGE\Radni\KOR01`.
> Verzija dokumenta: 2026-07-25, poslednje ažurirano 2026-08-04 (§3 status tabela usklađena sa §5, dodato §9).
> §4 (Gap analiza) je zamrznut istorijski snimak stanja pre Faze 0/1 — tekuće stanje prati §5 (checkbox po fazi).

---

## 1. Šta je legacy sistem (DOS / Clipper)

Originalni sistem je DOS računovodstveni paket pisan u **Clipper/dBase III**, organizovan u
**4 nezavisna modula** koji dele isti direktorijum po firmi (`KORxx`, trenutno KOR01–KOR26).
Svaka firma = zaseban folder sa svojim `.DBF` fajlovima.

| CLP | .PRG moduli | Modul | Srpski naziv |
| --- | --- | --- | --- |
| `FIN.CLP` | FIN1, FIN2, FIN3 | **FIN** | Finansijsko knjigovodstvo / Glavna knjiga |
| `ANAL.CLP` | ANAL1, ANAL2, ANAL3 | **ANAL** | Analitika (kupci / dobavljači) |
| `ROB.CLP` | MAT1–MAT7 | **ROB** | Robno knjigovodstvo (veleprodaja / maloprodaja) |
| `MAT.CLP` | M1–M4 | **MAT** | Materijalno knjigovodstvo (magacin) |

`GLAVNI.PRG` je launcher koji učitava firmu i konfiguraciju i grana na modul.
Konfiguracija firme (`KORISNIC.DBF` / `KOR.DBF`) ima **50+ bool flegova** po firmi
(maloprodaja da/ne, broj decimala, opcije štampe, prosečna cena vs nabavna, itd.).

---

## 2. Funkcionalna mapa po modulima

### 2.1 FIN — Glavna knjiga (FIN1/FIN2/FIN3)
- **Kontni plan** (`KONTPLAN.DBF`) — sintetika/analitika, unos/izmena/brisanje/štampa
- **Nalozi knjiženja** (`NALOG.DBF`) — unos naloga sa stavkama, provera ravnoteže, knjiženje/rasknjiženje
- **Šifarnik opisa promena** — `PROMENA` je šifra opisa dokumenta (dug-pot tip)
- **Kartice konta** (`KARTICA.DBF`) — hronološka kartica sa kumulativnim saldom (UKUP_DUG/UKUP_POT/SALDO)
- **Bruto bilans** (`BRUTO_B.DBF`) — `gk6`, `brut_bil`, ekranski + štampa
- **Simetrični bruto bilans**, **sintetičko stanje** (`sim_brut_bil`, `sint_stanje`)
- **Otvorene stavke** (`gk91`, `gk9`, `otv_st_zag`) — IOS logika
- **Kamate** — stope (`KAM_STOP.DBF`), obračun (`obrac_kamate`, `KAMATA.DBF`)
- **Korisnici + lozinka** (`novikorisnik`, `lozinka`)
- **Nova godina** — prenos salda (`ngod_prenos`, `ngod_bez_prenosa`)
- **Preknjižavanje** (`preknjizi`), sigurnosne kopije (`sig_start`, `bas_snimi`, `restauriraj`)

### 2.2 ANAL — Analitika (ANAL1/ANAL2/ANAL3)
Paralelna struktura FIN-u, ali analitička (prefiks `A`):
- Šifarnik analitike (`ANKONT.DBF`), analitički nalozi (`ANNAL.DBF`), kartice (`ANKART.DBF`)
- Bruto bilans analitike (`A_brut_bil`)
- Računi / otpremnice analitike (`ARAC.DBF`)
- **Prenos u finansijsko** (`Apreb_fin_nalog`, `Apreb_f_karticu`) — analitika knjiži zbir u GK

### 2.3 ROB — Robno (MAT1–MAT7)
- **Računopolagači / magacini** (`MAGACIN.DBF`: SIFRA, RACUNOPOL)
- **Artikli** (`ARTIKLI.DBF`) — sa tarifnim brojem
- **Nalozi za knjiženje robe** (`MAT_NAL.DBF`)
- **Kalkulacije veleprodaja** (`KALKULAC.DBF`) i **maloprodaja** (`MALKULAC.DBF`)
  — nabavna vrednost + zavisni troškovi → razlika u ceni + porez → prodajna vrednost
- **Nivelacija cena** (`NIV_NAL.DBF`) — promena prodajne cene, obračun razlike
- **Računi-otpremnice** (`RAC_OTP.DBF`) — izlaz robe, rabat, porez, poseban porez
- **Tarife / porezi** (`TARIFE.DBF`)
- **Cenovnik** (`CENOVNIK.DBF`)
- **Robne kartice** (`MAT_KART.DBF`), **bruto bilans robni**
- Stanje računopolagača, raspored artikala, sintetika po artiklima

### 2.4 MAT — Materijalno (M1–M4)
- **Šifarnik materijala** (`M_SIFR.DBF`) — 2.466 stavki u KOR01
- **Ulazi** (`ULAZ.DBF`) — prijem materijala
- **Trebovanja** (`TREBOV.DBF`) — izdavanje (izlaz), po kontu troška
- **Primopredaje** (`M_PRIMO.DBF`) — interni prenos magacin→magacin
- **Materijalne kartice** (`M_KART.DBF`) — **prosečna cena** (weighted average)
- **Bruto bilans materijalni** (`M_BRUT_B.DBF`), planske cene, cenovnik

---

## 3. Mapiranje DBF → .NET modeli

Trenutni skeleton (`ERPiFinansijeData/Models`) već pokriva jezgro FIN modula. Puno mapiranje:

| DBF | .NET model | Status |
| --- | --- | --- |
| KONTPLAN | `Konto` | ✅ postoji, sve kolone uvezene (adresa/žiro/telefon/stari konto) |
| KORISNIC/KOR | `Firma` + `FirmaSettings` | ⚠️ `Firma` postoji, fale config flegovi |
| — | `Korisnik` | ✅ postoji (dodati hash) |
| NALOG | `Nalog` + `StavkaNaloga` | ✅ postoji |
| KARTICA | računato preko `KarticaService.GetKarticaKontaAsync` | ✅ (nije poseban model — hronološka kartica se izvodi iz `StavkaNaloga` u letu) |
| BRUTO_B | računato preko `BrutoBilansService` | ✅ (agregacija po kontu iz proknjiženih naloga, nije poseban model) |
| KAM_STOP / KAMATA | `KamatnaStopa` + `KamataService.ObracunajKamatuAsync` | ✅ postoji |
| ANKONT/ANNAL/ANKART | — | ❌ **namerno nije portovano** — §3 Faza 3 arhitektonski nalaz: legacy ANAL modul nikad nije stvarno korišćen (prazni placeholder slogovi), pokriveno preko `Partner` + `StavkaNaloga.PartnerId` |
| ARAC | — | ❌ **namerno nije portovano**, isti razlog kao ANKONT/ANNAL/ANKART (0 stvarnih podataka za KOR01) |
| MAGACIN | `Magacin` (SIFRA, RACUNOPOL) | ✅ postoji |
| ARTIKLI / M_SIFR | `Artikal` | ✅ postoji (razdvojiti robni vs materijalni šifarnik) |
| MAT_NAL / MAT_KART | `Kalkulacija`/`MaloprodajnaKalkulacija`/`NivelacijaCena`/`RacunOtpremnica` + `MaterijalnaKartica` | ✅ postoji — realizovano kao odvojeni modeli po tipu dokumenta, ne kao jedan generički `RobniNalog`/`RobnaKartica` |
| KALKULAC / KAL_NAL | `Kalkulacija` + `KalkulacijaStavka` | ✅ sve kolone uvezene, knjiži se u GK (1320/1329/dobavljač) |
| MALKULAC / MAL_NAL | `MaloprodajnaKalkulacija` + stavke | ✅ sve kolone uvezene, knjiži se u GK (1340/1344/1348/dobavljač) |
| NIV_NAL | `NivelacijaCena` + stavke | ✅ postoji, knjiži se u GK (1320/1340 ↔ 1329/1348 prema vrsti magacina) |
| RAC_OTP | `RacunOtpremnica` + stavke | ✅ postoji, knjiži se u GK (konto kupca/6120/4700/5010/1320-1340), razdužuje materijalnu karticu |
| TARIFE | `PoreskaTarifa` | ✅ postoji, samostalan šifarnik (stopa, poseban porez) |
| CENOVNIK | — | ❌ nema posebnog modela; cena se drži samo na `Artikal.NabavnaCena`/`ProdajnaCena`, bez istorije cenovnika kroz vreme |
| ULAZ / TREBOV / M_PRIMO | `UlazNalog`, `TrebovanjeNalog`, `PrimopredajaNalog` | ✅ postoje, sve tri knjiže materijalnu karticu (modul MAT) |
| M_KART | `MaterijalnaKartica` | ✅ postoji, prosečna (ponderisana) cena — `MaterijalnaKarticaService` |

**Napomena o poljima** — legacy koristi `N19.2` (decimal 18,2 je ok), datumi `D8` (yyyymmdd),
`KNJIZEN N1` (bool 0/1). Migracioni alat (`ERPiFinansijeMigration/Program.cs`) već čita DBF binarno;
proširiće se novim tabelama.

---

## 4. Gap analiza — trenutni skeleton vs cilj

**Postoji i radi:** Dashboard (broji), Nalozi (pregled+filter), `NaloziService`,
DBF migracija (KONTPLAN, ANKONT, M_SIFR, MAGACIN, NALOG), Velopack pakovanje.

**Kritični nedostaci temelja:**
1. Koristi `EnsureCreated()` umesto **EF Core Migrations** → blokira evoluciju šeme
2. Nema **login / sesije / izbora firme** (multi-firma iz README ne stoji bez ovoga)
3. Lozinka je **plaintext** (`admin123`)
4. Nema **unosa/izmene naloga** — sve read-only
5. Nema pravih izveštaja (kartica, bruto bilans, IOS) ni `PdfReportService` sadržaja
6. Meni-dugmad (Kartice, Partneri, Magacin, Kalkulacije, Firme) su prazni stubovi

---

## 5. Fazni plan implementacije

### Faza 0 — Temelji (blokira sve ostalo)
- [x] Zameniti `EnsureCreated()` **EF Core migracijama** (`AccountingDbContext.Create(dbPath)` → `Database.Migrate()`, migracija `InitialCreate`)
- [ ] `Firma` + `FirmaSettings` (config flegovi iz KOR.DBF: `MALO`, `DECIM`, `PROS_PR`, `NABAVNA`...) — odloženo, nije blokiralo login
- [x] **Login + `AppSession` + prikaz firme** (`LoginWindow`, `AppSession.TrenutniKorisnik/TrenutnaFirma`) — testirano UI automatizacijom, radi
- [x] **Hash lozinke** (PBKDF2, 100k iteracija, osoljeno) — `HashPassword`/`VerifyPassword`, seed admin/admin123 preko `HasData`
- [x] `AccountingDbContextFactory` za design-time migracije (kao u ERPiSredstvaData)

### Faza 1 — FIN jezgro (najveća vrednost)
- [x] **Unos/izmena naloga**: `NalogEditWindow` — dodavanje/brisanje stavki, live provera Duguje==Potražuje (zeleno/žuto/crveno), snimanje preko `NaloziService.SaveNalogAsync`; izmena dozvoljena samo za neproknjižene naloge. „Proknjiži" dugme već je postojalo i i dalje radi.
- [ ] Šifarnik **opisa promena** (`PROMENA` kod → tabela) — odloženo, nije blokiralo ostatak Faze 1
- [x] **Kartica konta** — `KarticeView` (izbor konta + pretraga) + `KarticaService.GetKarticaKontaAsync`, hronološka sa kumulativnim saldom, testirano na kontu 470 (93 stavke, saldo se poklapa sa bruto bilansom)
- [x] **Bruto bilans** — `BrutoBilansService.GetBrutoBilansAsync` (agregacija po kontu iz proknjiženih naloga), pravi PDF izvoz u Izveštajima
- [x] `PdfReportService`: `GenerisiKarticuPdf`, `GenerisiBrutoBilansPdf` (uz postojeći `GenerisiDnevnikPdf`)

**Napomena iz testiranja (ažurirano 2026-07-25):** Ranije zabeleženo da bruto bilans obuhvata 418 različitih konta iz proknjiženih stavki dok je `Konta` tabela imala samo 42 sintetička konta, protumačeno kao nedostatak legacy master podataka. **Ispravka:** to je zapravo bio bag u uvozu (`ERPiFinansijeMigration/Program.cs` je čitao pogrešnu kolonu — `ST_KON` umesto `KONTO` — za broj konta; `ST_KON` je prazno za skoro sve redove). Nakon ispravke (objedinjen `DbfImportService`, vidi CHANGELOG 1.0.9) uvozi se svih ~3200 konta iz KONTPLAN.DBF.

### Faza 2 — FIN dopune
- [x] **Otvorene stavke / IOS** (`gk91`, `otv_st_zag`) + PDF obrazac IOS — `OtvoreneStavkeService`, `PartneriView`, `PdfReportService.GenerisiIOSPdf`. Vezano preko `StavkaNaloga.PartnerId` (ne preko konta — legacy ANAL modul za KOR01 nije korišćen, pa nema podataka za uparivanje po kontu partnera). Dodat i izbor partnera (ComboBox) po stavci u `NalogEditWindow` da nove stavke mogu da se vezuju za partnera.
- [x] **Kamate** — stope + obračun (`obrac_kamate` iz FIN2.PRG) — `KamatnaStopa` model + uvoz `KAM_STOP.DBF`, `KamataService.ObracunajKamatuAsync` (zatezna kamata po danu, sa podrškom za više perioda različitih stopa kroz vreme), `KamataWindow` dijalog iz Partneri taba (pregled/dodavanje stopa + obračun po partneru + PDF). **Napomena:** uvezene legacy stope su istorijske (poslednja iz 2006. godine, 13%) — ispravan obračun za tekuće naloge zahteva unos aktuelne zvanične stope kroz UI ("Dodaj stopu"); nisam izmišljao "trenutnu" stopu jer ne raspolažem pouzdanim tekućim zvaničnim podatkom. Testirano servisno i vizuelno (10.000 RSD dug, 204 dana, 13% → 726,58 RSD, poklapa se sa ručnim izračunom).
- [x] **Nova godina** — prenos salda (`ngod_prenos` iz FIN2.PRG) — `NovaGodinaService.PrenesiUNovuGoduAsync`, dugme "📅 Nova godina" u `NaloziView` (samo za Administratora). Računa zaključni saldo po kontu i kreira/knjiži nalog `PS-{godina}` datiran 1.1. Ima ugrađen bezbednosni test: **odbija prenos ako ukupan saldo svih konta nije 0** (znak neispravnog naloga u knjigama) umesto da tiho prenese neuravnoteženo stanje — testirano na pravom KOR01 (ispravno odbija zbog poznatog "Nalog 0" artefakta, razlika 338,00) i na kopiji baze posle uklanjanja tog zapisa (uspešno, Uravnotezen=True). Takođe sprečava dupli prenos za istu godinu.
- [x] **Rasknjižavanje** (`rasknjizi` iz FIN3.PRG) — `NaloziService.RasknjiziNalogAsync`, dugme "🔓 Rasknjiži" u `NaloziView` (samo za Administratora, uz potvrdu), vraća nalog u nacrt da bi mogao ponovo da se izmeni preko postojećeg "Izmeni" dugmeta. Sigurnosne kopije još nisu urađene (postoji `BackupService` u Sredstva kao uzor).

### Faza 3 — ANAL modul

**Arhitektonski nalaz (2026-07-24):** provereni su stvarni podaci u ANNAL.DBF i ARAC.DBF za
KOR01 — svaki ima tačno **jedan prazan placeholder slog** (svi iznosi 0, prazna polja), a
ANKART.DBF nema nijedan slog. Legacy ANAL modul **nikad nije stvarno korišćen** za ovu firmu.
Važnije: DOS sistem je držao FIN i ANAL kao **odvojene DBF fajlove** samo zato što Clipper
nije imao relacione veze — analitika i finansije su se ručno sinhronizovale kroz
`Apreb_fin_nalog`/`Apreb_f_karticu` procedure. U ovom sistemu, `StavkaNaloga.PartnerId`
(napravljeno u Fazi 2 za Otvorene stavke/IOS) **već objedinjuje** glavnu knjigu i analitiku u
jednoj tabeli. Praviti paralelnu `AnalitickiNalog`/`AnalitickiKartica` strukturu bi bilo:
(a) arhitektonski suvišno — isti podatak bi postojao dva puta bez razloga, i
(b) netestabilno — nema stvarnih podataka za uvoz.

Prema tome, Faza 3 je svedena na jedinu stvarno novu i korisnu stavku:

- [ ] ~~Analitički konti/nalozi/kartice (paralelno FIN-u)~~ — nepotrebno, već pokriveno preko `Partner` + `StavkaNaloga.PartnerId` (Faza 2)
- [x] **Bruto bilans analitike** — `OtvoreneStavkeService.GetBrutoBilansAnalitikeAsync` (agregacija duguje/potražuje/saldo po partneru, analogno `A_brut_bil` iz ANAL2.PRG ali nad istim, već unificiranim podacima), `PdfReportService.GenerisiBrutoBilansAnalitikePdf`, nova kartica u Izveštajima. Testirano servisno sa 2 partnera i više naloga — agregacija tačna.
- [ ] ~~Prenos analitike u finansijsko (`Apreb_fin_nalog`)~~ — nepotrebno, nema odvojenih tabela između kojih bi se prenosilo
- [ ] ~~Analitički računi/otpremnice (`ARAC`)~~ — nula stvarnih podataka za KOR01; ako zatreba, prirodnije se uklapa u Fazu 5 (Robno/fakturisanje) nego u ANAL

### Faza 4 — MAT modul (materijalno / magacin)
- [x] **Materijalne kartice sa prosečnom cenom** — `MaterijalnaKarticaService` (`DodajUlazRedAsync`/`DodajIzlazRedAsync`). Formula je **validirana replay-em stvarnih istorijskih podataka**: obrisan je legacy M_KART snapshot za par magacin 001/artikal 22560 (22 stavke), sve odgovarajuće UlazStavke su ponovo proknjižene kroz novi algoritam od nule, i finalni rezultat (Stanje=821,47, Saldo≈1.234.824,98) se poklopio sa originalnim legacy snapshotom (1.234.824,97) na paru novčića. Ključni nalaz: **prijem se knjiži po unetoj ceni** (Saldo se akumulira), a **izdavanje/korekcija po trenutnoj prosečnoj ceni** (Saldo/Stanje) — ne po nekoj unetoj vrednosti.
- [x] Ulazi, Trebovanja — `UlazService`/`TrebovanjeService` (CRUD + knjiženje sa proverom duplog knjiženja i nedovoljnog stanja), `MagacinView` (tabovi Kartice/Ulazi/Trebovanja), `UlazEditWindow`/`TrebovanjeEditWindow` za unos. Testirano servisno (pun tok: unos→knjiženje→provera kartice, oba zaštitna mehanizma) i vizuelno kroz UI.
- [ ] Primopredaje — UI nije rađen (0 stvarnih stavki u KOR01, model/uvoz već postoje od ranije)
- [ ] Bruto bilans materijalni, planske cene — odloženo za kasnije ako zatreba

### Faza 5 — ROB modul (robno / trgovina)

**Napomena:** KOR01 nema stvarnih podataka za ovaj modul (KALKULAC/MALKULAC/NIV_NAL/RAC_OTP su
prazni ili gotovo prazni u legacy bazi), pa je fokus stavljen na deo koji se može rigorozno
proveriti nezavisno od podataka — čistu formulu obračuna — umesto na delove koji bi ostali
netestirani na realnim brojevima.

- [x] **Kalkulacija veleprodaje** (troškovi → razlika → PDV → prodajna, analogno `kalkknjizenje`
  iz MAT2.PRG) — `KalkulacijaService.Izracunaj` (čista formula, bez zavisnosti od baze) +
 - [x] **Kalkulacija maloprodaje** (MAT3 / `MALKULAC.DBF` sa ukalkulisanom maržom i maloprodajnim PDV-om) — omogućeno filtriranje i prebacivanje Tip-a (Veleprodaja vs Maloprodaja) u `TrgovinaView`.
- [x] **Nivelacija cena** (MAT7 / `NIV_NAL.DBF`) — promena prodajnih cena artikala u magacinu sa proračunom razlike u ceni, knjiženjem na kontima `1320`/`1340` i PDF zapisnikom (`NivelacijaEditWindow`).
- [x] **Računi-otpremnice (Fakture)** (MAT5 / `RAC_OTP.DBF`) — prodaja robe kupcima sa rokom dospelosti, rabatom %, PDV-om (20%/10%/0%), automatskim razduženjem magacina i knjiženjem finansijskog naloga u Glavnoj knjizi (`Kupci 2040` / `Prihod 6120` / `PDV 4700`) + Zvanični PDF račun-faktura.
- [x] **Primopredaje / Interni prenosi** (M4 / `M_PRIMO.DBF`) — interni prenos materijala iz dajućeg u ulazni magacin sa automatskim proračunom ponderisane prosečne cene (`PrimopredajaEditWindow`).
- [x] **Korisnički Help i Prečice** — obogaćen `PomocView` i podržan taster `Esc` za brzi izlaz iz svih modalnih prozora.

---

## 6. ERPiSredstva & Budući plan razvoja (Roadmap)

### 🏢 6.1 Integracija sa ERPiSredstva (`C:\SREDSTVA\ERPiSredstva`)
- **Automatsko knjiženje amortizacije u Glavnu knjigu**:
  - Generisanje naloga knjiženja u `ERPiFinansije` (Konto `5400` Troškovi amortizacije / Konto `0290` Ispravka vrednosti) direktno iz obračuna u `ERPiSredstva`.
- **Poreska amortizacija i bilansi**:
  - Povezivanje obračuna poreske amortizacije (Obrazac OA) i privremenih razlika sa Poreskim bilansom (**Obrazac PB-1**).

### 📊 6.2 Zvanični Finansijski Izveštaji za APR
- **Bilans Stanja**: Pregled aktivnih i pasivnih konta po zvaničnoj AOP šemi.
- **Bilans Uspeha**: Pregled rashoda i prihoda po grupacijama konta.

### 🧾 6.3 PDV Evidencija (KPR i KIR)
- Knjiga primljenih računa (KPR) i Knjiga izdatih računa (KIR) sa automatskim punjenjem iz faktura i kalkulacija.

---

## 7. Ključni algoritmi za portovanje (ne izmišljati — preslikati iz PRG)

1. **Kartica sa saldom** (`prik_kar`/`stampav_kartica`, FIN1) — hronološko ređanje,
   `UKUP_DUG += DUGUJE`, `UKUP_POT += POTRAZUJE`, `SALDO = UKUP_DUG − UKUP_POT`.
2. **Bruto bilans** (`brut_bil`, FIN2) — grupisanje po kontu, promet duguje/potražuje + saldo,
   rekapitulacija po klasama, provera ukupne ravnoteže.
3. **Prosečna cena materijala** (`M_KART`, m-moduli) — nova cena =
   `(staro_stanje*stara_cena + ulaz_kol*ulaz_cena) / (staro_stanje + ulaz_kol)`.
4. **Kalkulacija** (`KALKULAC`/`MALKULAC`, MAT2/MAT3) — svega nabavno + zavisni troškovi
   → razlika u ceni → porez (tarifa) → prodajna vrednost.
5. **Obračun kamate** (`obrac_kamate`, FIN2) — po stopama iz `KAM_STOP` na broj dana dospeća.
6. **Nova godina** (`ngod_prenos`, FIN2) — nulira promet, prenosi saldo konta kao početno stanje.

**Pravilo:** za svaki od ovih izvući tačnu formulu iz `.PRG` i pokriti **xUnit testovima**
(kao što ERPiSredstvaData.Tests pokriva amortizaciju) pre nego što se zameni računanje.

---

## 8. Preporučeni sledeći korak

Krenuti od **Faze 0 (Temelji)** — migracije + login/sesija/firma + hash — jer sve ostale
faze zavise od šeme i sesije. Odmah potom **Faza 1 (unos naloga + kartica + bruto bilans)**
kao prva upotrebljiva verzija koja zamenjuje FIN modul DOS-a.

---

## 9. Robno → finansijsko: konta po dokumentu i veza sa nalogom

> Nastalo iz provere (2026-08-04) da li se u robnim nalozima čuva veza sa finansijskim
> nalogom — kod nas i uporedno kod drugih programa za robno/finansijsko knjigovodstvo.

### 9.1 Konta po tipu robnog dokumenta

Sva konta su čitana iz stvarnih naloga zatečenih baza (`RobnaKonta`), ne izvedena iz
kontnog okvira — analitika (naročito razlika u ceni i ukalkulisani PDV) razlikuje se od
firme do firme. Videti §7, stavka 4 (Kalkulacija) gore za formulu obračuna.

| Robni dokument | Servis | Nalog u glavnoj knjizi |
| --- | --- | --- |
| Veleprodajna kalkulacija | `KalkulacijaService.KnjiziUGlavnuKnjiguAsync` | `1320` D (nabavno+razlika) / `1329` P (razlika) / konto dobavljača P (svega nabavno) |
| Maloprodajna kalkulacija | `MaloprodajnaKalkulacijaService.KnjiziUGlavnuKnjiguAsync` | `1340` D (prodajna sa PDV) / `1344`\|`13441` P (ukalkulisani PDV, po stopi) / `1348` P (razlika) / konto dobavljača P |
| Nivelacija cena | `NivelacijaService.KnjiziNivelacijuAsync` | `1320`/`1340` ↔ `1329`/`1348`, prema vrsti magacina (`RobnaKonta.RobaZaVrstuMagacina`/`RazlikaZaVrstuMagacina`) |
| Račun-otpremnica (prodaja) | `RacunOtpremnicaService.KnjiziRacunAsync` | konto kupca (iz kontnog plana, grupa 204/120) D (bruto) / `6120` P (prihod, osnovica) / `4700` P (PDV) / `5010` D (nabavna vrednost prodate robe) / `1320`\|`1340` P (razduženje, prema vrsti magacina) |
| Uvozna kalkulacija | `UvoznaKalkulacijaService.SacuvajIKnjiziUvozAsync` | `1300` D (nabavna vrednost) / `4350` P (ino dobavljač) / `4330` P (carina i zavisni troškovi) |
| Ulaz, Trebovanje, Primopredaja | `UlazService`, `TrebovanjeService`, `PrimopredajaService` | — samo materijalna kartica, bez naloga u GK |

**Otvoreno** (nije implementirano, nađeno pri istoj proveri):
- **Obračun razlike u ceni** — periodični prenos sa `5010` na `1348`/`1349`, standardan korak
  kod svih uporednih programa (mesečno ili pri zatvaranju perioda); kod nas ne postoji.
- **Dnevni pazar** (ESIR/fiskalizacija) — `EsirFiskalizacijaService` evidentira fiskalne
  podatke, ali ne pravi nalog (`1340` P / `1344` D / `1348` D / `6140` P / `4700` P / blagajna D).
- **Primopredaja veleprodaja → maloprodaja** ne pravi finansijski nalog — prenos menja i
  `1320`/`1340` i ukalkulisani PDV, ali `PrimopredajaService` diže samo materijalnu karticu.
- **Uvozna kalkulacija ne dodiruje materijalnu karticu** — knjiži samo u GK; roba iz uvoza
  ne ulazi u robnu karticu magacina, za razliku od svih ostalih ulaznih dokumenata.

### 9.2 Kako se čuva veza dokument ↔ nalog

**Na nivou zaglavlja dokumenta** veza je FK `NalogId → Nalog.NalogId` (nullable, postavlja se
pri knjiženju, briše pri rasknjiženju): `Kalkulacija`, `MaloprodajnaKalkulacija`,
`NivelacijaCena`, `RacunOtpremnica`, `UvoznaKalkulacija` (dodato 1.4.2 — vidi CHANGELOG). Van
robnog toka isti obrazac koriste i `BlagajnickiNalog`, `KompenzacijaModels`, `PutniNalog`.
Rasknjiženje (`Rasknjizi*Async`) je simetrično knjiženju: ukloni redove materijalne kartice
obrnutim redosledom (uz proveru da nije knjiženo nešto kasnije za isti artikal/magacin —
`MaterijalnaKarticaService.UkloniPoslednjiRedAsync`), pa ukloni nalog i njegove stavke, pa
vrati `IsKnjizen = false` i `NalogId = null`.

**Na nivou reda materijalne kartice veze nema.** `MaterijalnaKartica` nema FK ka izvornom
dokumentu ni ka nalogu — izvorni dokument se u UI-ju (`TrgovinaView.DgRobnaKartica_MouseDoubleClick`)
pogađa **parsiranjem teksta `OpisPromene`** regexom (`^Kalkulacija (\d+)$`,
`^Primopredaja br\. (\d+)`). Format opisa je pisan u dva oblika kroz verzije („Kalkulacija 7"
i „Kalkulacija7"), pa je ovo mesto krhko — pravo rešenje bi bilo `IzvorTip`/`IzvorId` kolone
na `MaterijalnaKartica`, ali menjanje formata reda kartice je veći zahvat (dotiče uvoz, sve
servise koji pišu redove i istoriju postojećih baza), pa nije rađeno uz ovu proveru.

**Uporedno kod drugih programa** (BizniSoft, 4D Wand, Minimax) veza je istog oblika — robni/
izlazni dokument nosi pokazivač na nalog, ne obrnuto; nalog kreiran automatski iz dokumenta se
ne menja ručno, samo kroz storno/rasknjiženje izvornog dokumenta (BizniSoft: „Storniraj
knjiženja i vrati u obradu" — stornira nalog i vraća dokument u obradu; Minimax: automatski
nalog je zaključan za ručnu izmenu).
