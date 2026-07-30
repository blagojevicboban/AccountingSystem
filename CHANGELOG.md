# 📋 Istorija izmena (Changelog) — AccountingSystem

Sve značajne promene i novine u aplikaciji **AccountingSystem** dokumentovane su u ovom fajlu.

Format je zasnovan na [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) standardu i prati Semantic Versioning.

## [1.0.29] - 2026-07-30

### 🚀 Nove Funkcionalnosti & Obračun Zatezne Kamate
- **Modul Obračun Zatezne Kamate (`obrac_kamate()`)**:
  - Ugrađena kompletna poslovna logika obračuna zakonske zatezne kamate po konformnom metodu NBS (podržane stope 2021–2026 sa pod-periodima).
  - Automatsko sejanje zakonskih kamatnih stopa i mogućnost unosa novih.
  - Zvanični PDF obrazac **Obračun zatezne kamate** po partnerima sa potpisnim linijama.
  - Automatsko knjiženje kamatnog lista u Glavnu knjigu na **Konto 662000 (Prihodi od zateznih kamata)** i duguje kupac.

### 🎨 Vizuelni Identitet & Ikonica Aplikacije
- Zvanična nova ikona `app.ico` (motiv poslovne aktovke + ERPi FINANSIJE) na plavoj zaobljenoj podlozi (`#2563EB`).

---

## [1.0.28] - 2026-07-29

### 🚀 Poboljšanja & Precizno Usklađivanje Zaključnog Lista (`FIN1.PRG` / `gk5()`)
- **Dvostruko grupiranje i prebijanje po nivoima (Analitika -> Sintetika -> Klasa -> Rekapitulacija)**:
  - Usklađeno saldiranje i grupiranje na sintetičkim kontima prema proceduri `gk5()` iz DOS programa `FIN1.PRG`.
  - Prvo se računa saldo svakog analitičkog konta (`uk_dug - uk_pot`), zatim se vrši prebijanje na nivou 3-cifrenog sintetičkog konta (`tot_sald_dug - tot_sald_pot`), dok se za zbirove po klasama i Rekapitulaciju (`K L A S A : 0..7` i `K L A S A : U`) sabiraju pojedinačni saldi sintetičkih konta.
  - Svi iznosi (Početno stanje, Promet bez početnog stanja, Ukupni promet i Saldo) se sada 100% u cent poklapaju sa starim DOS sistemom.
- **Detekcija početnog stanja (`PromenaKod == 1`)**:
  - Unapređena funkcija `IsPocetnoStanje` da prepoznaje sve stavke sa `PromenaKod == 1` (`PROMENA = 1`), čime se iznosi početnog stanja i tekućeg prometa tačno razdvajaju u odgovarajuće kolone.

---

## [1.0.27] - 2026-07-29


### 🚀 Nove Funkcionalnosti & Rekapitulacija
- **Rekapitulacija po klasama na Zaključnom listu (`GetZakljucniListAsync`)**:
  - Implementirana sekcija **R E K A P I T U L A C I J A** na dnu Zaključnog lista sa potpunim pregledom po svim klasama (`K L A S A : 0` do `K L A S A : 7`) i ukupnim zbirom `K L A S A : U` (identično proceduri `gk5()` iz `FIN1.PRG` i legacy ispisu).
  - Dodate linije za potpis u PDF-u: **`OBRAČUNAO`**, **`ŠEF RAČUNOVODSTVA`** i **`RUKOVODILAC`**.

---

## [1.0.26] - 2026-07-29

### 🚀 Nove Funkcionalnosti & Prikazi
- **Ekranski pregled Zaključnog lista sa Excel dugmetom (`ZakljucniListPreviewWindow`)**:
  - Dodato dugme `👁 Prikaži na ekranu` na kartici **Zaključni list** (`IzvestajiView`).
  - Omogućen ekranski pregled svih sintetičkih (3-cifrenih) konta sa zbirnim redovima po klasama (`KLASA: 0`, `KLASA: 1`, ...).
  - Ugrađeno `📊 Excel` dugme za direktan izvoz Zaključnog lista u `.xlsx` fajl sa C# izračunatim zbirnim redom i senčenim redovima klasa.

### 📄 PDF Izveštaji i Optimizacija za A4 Portrait
- **Prilagođeni fontovi i margine za A4 uspravno (`PdfReportService`)**: Margine smanjene na 1.0cm, velika numerička polja formatirana na 8pt sa optimalnim paddingom tako da svi iznosi staju na A4 uspravno bez prelamanja cifara.

---

## [1.0.25] - 2026-07-29

### 🐛 Ispravke i Validacije (Bug Fixes)
- **Excel "Repair" greška u Bruto bilansu**: Otklonjena korupcija `calcChain.xml` fajla uzrokovana ClosedXML `FormulaA1` — TOTAL red sada koristi vrednosti izračunate u C# umesto Excel formula, eliminišući Excelovu "Removed Records" poruku pri otvaranju fajla.
- **Pogrešan TOTAL u Excel izvozu**: TOTAL red je pogrešno sabirao i subtotale klasa (`KLASA: 5`) i subtotale sintetičkih konta (`TOTAL sintetičkog konta 563`). Sada se sabiraju isključivo analitički (Detalj) redovi, identično prikazu u aplikaciji.

### 🎨 UI / UX i Odzivnost
- **Senčenje redova u Excel izvozu Bruto bilansa**: Excel fajl sada vizualno odražava prikaz u aplikaciji — `TOTAL sintetičkog konta` redovi su senčeni svetlo sivom (`#F8FAFC`, bold), a `KLASA:` redovi tamnijom sivom (`#E2E8F0`, bold), identično XAML `DataTrigger` stilizaciji u prozoru za pregled.

---

## [1.0.24] - 2026-07-29

### 🚀 ErpHub Integracija & Pokretanje sa Konkretnom Bazom (CLI Integration)
- **Ugrađena podrška za `--db-path` CLI argument**: Omogućeno direktno pokretanje `AccountingApp.exe` iz ErpHub centralnog kontrolnog panela sa automatskim prosleđivanjem putanje do izabrane SQLite baze/firme (`--db-path "<path>"`).

---

## [1.0.23] - 2026-07-29

### 🚀 Izvoz Finansijskih i Robnih Podataka u Excel (XLSX)
- **Implementiran `ExcelExportService` (ClosedXML)**: Omogućeno jednim klikom izvoženje bilo kog tabelarnog prikaza u `.xlsx` fajl sa automatski udesno poravnatim numeričkim vrednostima, prilagođenim širinama kolona, stilizovanim zaglavljima i zbirnim redom sa Excel `=SUM(...)` formulama.
- **Dodata `📊 Excel` dugmad na svim tabovima (15 tabova)**:
  * **Materijalno knihovodstvo (`MagacinView`)**: Šifrarnik materijala, Ulazi, Trebovanja, Primopredaje, Kartice materijala, Bruto bilans materijala.
  * **Robno knihovodstvo (`TrgovinaView`)**: Računopolagači, Šifrarnik artikala, Poreske tarife, Primopredaje, Kalkulacije, Računi/Otpremnice, Nivelacije cena, Robna kartica, Robni Bruto bilans.

### 🎨 UI / UX, Odzivnost i Otklanjanje Preklapanja (Responsive Toolbars)
- **Kompletno refaktorisana zaglavlja svih tabova na `DockPanel` + `WrapPanel`**: Rigidni jednocelijski `Grid` elementi na svim tabovima (`MagacinView`, `TrgovinaView`, `PartneriView`) zamenjeni su fleksibilnim layout-om koji automatski prelama dugmiće i komande u više redova na manjim rezolucijama ekrana ili uzim prozorima. Sva preklapanja pretrage i akcionih dugmića su u potpunosti eliminisana.
- **Standarizacija naziva u zaglavljima**: Preimenovano zaglavlje modula u **`BRUTO BILANS MATERIJALNOG KNJIGOVODSTVA`** na svim nivoima (WPF tab, WPF prozor, PDF izveštaj i Excel export) radi potpunog usklađivanja sa Clipper izveštajima (`M1.PRG` / `st_mat_bruto()`).
- **Orijentacija PDF štampa u Portret (A4 Portrait)**: Svi štampani izveštaji i kartice (`RobnaKartica`, `MaterijalnaKartica`, `RobniBrutoBilans`, `StanjePoArtiklima`, `RasporedArtikala`) prebačeni su u A4 vertikalni (portrait) format.

### 🐛 Ispravke, Preciznost Bilansa i Baza Podataka
- **Matematička preciznost u paru u Bruto bilansu materijalnog knjigovodstva**: Rešen problem neslaganja salda u odnosu na Clipper izveštaj `brutobilansmaterijalno.txt`. Prilagođeno filtriranje kartica tako da se ne izbacuju stavke iz `MaterijalneKartice` koje nemaju eksplicitnu oznaku vrste materijala (npr. ekseri `09010`), čime se ukupni zbir za magacine poklapa u paru sa DOS izvornikom (npr. Centralni magacin `1.368.356,67 RSD` Duguje / `372.599,16 RSD` Potražuje / `995.757,51 RSD` Saldo).
- **Korekcija uvoza `RACUNOPOL` polja (`DbfImportService`)**: Mapiranje uvoza magacina prilagođeno tako da se podaci iz polja `RACUNOPOL` upisuju u naziv magacina/računopolagača.
- **Konverzija brojeva dokumenata u Integer tipove**: Reinisane i migrirane kolone za brojeve dokumenata (`BrojNaloga`, `BrojKalkulacije`, `BrojOtpremnice`, `BrojRacuna`, `BrojNivelacije`) u celobrojni tip (int) radi efikasnijeg sortiranja i pretrage.

### ⚠️ Migracije i Baza Podataka
- **Nova EF Core migracija `BrojPoljaKaoInt`**: Automatski se primenjuje pri pokretanju aplikacije za ažuriranje tipova kolona u SQLite bazi.

---

## [1.0.22] - 2026-07-29

### 🚀 Reorganizacija i Unapređenje Glavnog Menija
- **Kompletan pristup modulu Rezervnih kopija (`BackupView`)**: Uvrštena nova stavka `💾 Rezervne kopije (Backup)` pod sekcijom Podešavanja i Sistem sa podrškom za ručne/automatske kopije i restauraciju baze.
- **Eksplicitna opcija za promenu firme (`BtnFirme`)**: Dodata vidljiva stavka `🏢 Upravljanje firmama` u meniju kao i značka `🔄 Promeni` na kartici aktivne firme.
- **Logička reorganizacija sekcija**: Meni je jasno struktuiran u 4 glavne knjigovodstvene celini: *FINANSIJSKO KNJIGOVODSTVO*, *ROBNO KNJIGOVODSTVO I MAGACINI*, *POREZI I ZVANIČNI BILANSI*, *PODEŠAVANJA I SISTEM*, *DOKUMENTACIJA*.

### 🎨 UI / UX, Sklopivi Meni & Pretraga
- **☰ Sklopivi bočni meni (Compact Sidebar Toggle)**: Dodato dugme `☰` u zaglavlju aplikacije za sklapanje menija sa `240px` na `64px` čime se oslobođava radni prostor za finansijske i robne tabele.
- **🔍 Brza pretraga komandi u meniju**: Ugrađena traka za brzu pretragu sa automatskim filtriranjem stavki u realnom vremenu pri kucanju.
- **⌨️ Tastaturne prečice (Global Shortcuts)**: Implementirani shortcut-ovi `Ctrl + F` / `Ctrl + K` (brza pretraga menija), `Ctrl + M` (sklapanje sidebara) i `F1` (pomoć).
- **Redizajn kartice aktivne firme**: Prilagođen layout kartice firme u 3 odvojena reda sa `TextTrimming="CharacterEllipsis"` kako dugme *Promeni* više ne prekriva naziv preduzeća.
- **ToolTip vodiči**: Dodati detaljni opisi za sve navigacione kontrole menija.

### 🐛 Ispravke i Validacije
- **Ispravka prikaza detalja firme (`FirmeView`)**: Rešen problem neprikazivanja podataka u desnom panelu pri selekciji firme iz tabele levo implementacijom `DgFirme_SelectionChanged` i automatskom selekcijom trenutno otvorene firme.

---

## [1.0.21] - 2026-07-28

### 🚀 Modul Materijalne kartice & Provera salda (Magacin / MAT)
- **Kompletiran modul Materijalnih kartica (`M1.PRG`)**: Omogućen rad sa materijalnim karticama zaliha po ponderisanoj prosečnoj ceni — unos i izmena materijala u šifarniku magacina (`MaterijalEditWindow`).
- **Provera i rekalkulacija materijalnih kartica (`ProveraKarticaWindow`)**: Novi modul za dijagnostiku slaganja kartica sa dokumentima ulaza i trebovanja sa mogućnošću automatske rekalkulacije i korekcije salda zaliha.
- **Upravljanje karticama i štampa (`MagacinView`)**: Omogućen prikaz pojedinačne kartice, prikaz svih kartica, pojedinačna/masovna PDF štampa materijalnih kartica i pokretanje provere zaliha.

### 🎨 UI / UX i Odzivnost
- **Novi dijaloški prozori**: Ugrađeni namenski dijalozi za brzi unos/izmenu materijala i dijagnostički prozor za proveru kartica.
- **PDF Izveštaji materijalnih kartica**: Novi format i izgled PDF dokumenata za materijalne kartice i izveštaj provere kartica.

### ⚡ Optimizacija i Performanse
- **MaterijalnaKarticaService & Uvoz**: Proširen servis za automatsko ažuriranje salda i prosečne cene zaliha, uz unapređen uvoz materijala, ulaza i trebovanja iz Clipper DOS baza (`KOR03`, `KOR04`, `KOR05`).

---

## [1.0.20] - 2026-07-28

### 🚀 Sintetički izveštaj "Stanje po artiklima" (Trgovina)
- **Sintetički izveštaj Stanje po artiklima (`BtnStampajStanjePoArtiklima`)**: Novi sintetički PDF izveštaj (ekvivalent mat92 / MAT1.PRG) koji prikazuje objedinjeni ulaz, izlaz, stanje, duguje, potražuje i saldo po artiklima preko SVIH magacina preduzeća do zadatog datuma.

### 🎨 UI / UX i Odzivnost
- **Dugme za PDF štampu**: Dodata komanda `📊 Stanje po artiklima (PDF)` u zaglavlju sekcije Robni bruto bilans u Trgovini.
- **Prikaz pakovanja u izveštajima**: Podatak o pakovanju artikla uključen u tabelarni prikaz i PDF izveštaje robnog bruto bilansa i stanja po artiklima.

### ⚡ Optimizacija i Performanse
- **RobniBrutoBilansService nadogradnja**: Proširen model `RobniBrutoBilansRed` sa poljem `Pakovanje` radi celovitog prikaza tehničkih i komercijalnih svojstava robe.

---

## [1.0.19] - 2026-07-28

### 🚀 Višestruka štampa robnih kartica i analitika artikala (Trgovina)
- **Višestruka štampa robnih kartica (`BtnStampajRobnuKarticu` & `BtnStampajSveKartice`)**: Omogućena selekcija više artikala (ili svih artikala u magacinu / svim magacinima) i generisanje objedinjenog PDF dokumenta sa robnim karticama.
- **Raspored artikala — analitika (`MAT91`)**: Novi izveštaj i štampa „Raspored artikala (analitika)" sa prikazom stanja, nabavne cene i ukupne vrednosti zaliha po magacinima na izabrani datum.
- **Obuhvat „Svi magacini”**: Dodata opcija „Svi magacini” u robnom knjigovodstvu radi sagledavanja zbirnog prometa i stanja robe u celom preduzeću.

### 🎨 UI / UX i Odzivnost
- **Proširena selekcija u listi artikala**: `LstArtikliRobno` sada podržava višestruki izbor artikala (`SelectionMode="Extended"`).
- **Redizajn dugmadi za štampu**: Intuitivna organizacija komandi za štampu pojedinačne/grupne kartice, svih kartica i analitičkog rasporeda artikala.

### ⚡ Optimizacija i Performanse
- **Grupna agregacija kartica**: Efikasno skupljanje i spajanje svih robnih kartica u jedinstven PDF dokument uz minimalno opterećenje memorije.

---

## [1.0.18] - 2026-07-28

### 🚀 PDF Štampa kalkulacija i primopredaja (Trgovina)
- **Štampa kalkulacija (`BtnStampajKalkulaciju`)**: Omogućena direktna generacija i otvaranje PDF dokumenata za veleprodajne (`MAT6`) i maloprodajne (`MAT3`) kalkulacije iz pregleda kalkulacija.
- **Prikaz jedinica mere i naziva artikala**: Automatsko dopunjavanje naziva artikla i jedinice mere u svim tabelarnim pregledima i PDF dokumentima kalkulacija i primopredajnih naloga.

### 🎨 UI / UX i Odzivnost
- **PDF Nivelacija cena — zbirna traka (TOTAL)**: Dodata zbirna traka sa ukupnim vrednostima starog iznosa, novog iznosa i ukupne razlike u PDF zapisniku o nivelaciji cena.
- **Prikaz u listi primopredajnih naloga**: Ubačene kolone za naziv artikla i jedinicu mere u detaljnom prikazu stavki primopredajnog naloga (`DgPrimopredajaStavke`).

### 🐛 Ispravke i Validacije
- **KalkulacijaStavka & PrimopredajaStavka models**: Dodata unmapped svojstva `NazivArtikla` i `JedinicaMere` radi bezbednog prenosa i prikaza podataka u UI i PDF servisu.

---

## [1.0.17] - 2026-07-28

### 🧾 Poreske tarife (novi šifarnik)
- **Šifarnik poreskih tarifa (`PoreskaTarifaEditWindow`)**: Novi CRUD ekran (tarifni broj, porez %, poseban porez %, porez u ceni) sa PDF štampom, analogan legacy `TARIFE.DBF`. Uvoz podržan i kroz `⚙️ Podešavanja` i kroz samostalni `AccountingMigration` alat.

### 📊 Robni bruto bilans (novi izveštaj)
- **Robni bruto bilans (`RobniBrutoBilansService`)**: Agregacija materijalnih kartica po magacinu i artiklu — početno stanje, ulaz, izlaz i stanje, količinski i vrednosno, sa filterima i PDF štampom. Napaja nove KPI pločice na Radnoj tabli (vrednost zaliha, negativna stanja).

### 🔄 Nivelacije cena — proširenje
- **Automatska generacija zapisnika o nivelaciji**: Svođenje na prosečnu nabavnu cenu po magacinu (analogno legacy `svodj_pros_p()`), pokreće se dugmetom „Generiši nivelaciju“.
- **Ispravka knjiženja**: Knjiženje nivelacije sada stvarno ažurira prodajnu cenu artikla (ranije je promena cene ostajala nezabeležena) i knjiži na konto 1320 ili 1340 u zavisnosti od vrste magacina (maloprodaja/veleprodaja), umesto na fiksni konto.
- **Masovno knjiženje nivelacija** — novo dugme za knjiženje svih nezaknjiženih nivelacija odjednom.

### 📄 Računi-Otpremnice — unapređenja
- **Unos stavki po šifri artikla**: Stavke računa se sada unose kucanjem šifre artikla umesto obavezne veze na postojeći šifarnik — omogućava unos i pre nego što je artikal formalno zaveden.
- **Nova polja**: Broj otpremnice, konto kupca, rok plaćanja (broj dana), način plaćanja. Dodata PDF štampa računa-otpremnice.

### 🏢 Šifarnik artikala i Računopolagača — samostalni CRUD
- **Nove tabele u Trgovini**: „🏢 Računopolagači (MAT1)“ i „📦 Šifarnik artikala (MAT2)“ sa unosom, izmenom, brisanjem i PDF štampom (ranije se održavalo samo kroz uvoz).

### 🔁 Uvoz DBF/DOS podataka — fleksibilnije mapiranje
- **Mapiranje kolona sa fallback-om**: Uvoz sada proba više mogućih naziva kolone (npr. `SIFRA`/`KOD`, `NAZIV`/`IME`/`OPIS`) umesto tačno jednog imena — sprečava tihe neuspehe uvoza kad legacy DBF fajlovi variraju u nazivima polja.
- **Novi mapperi**: poreske tarife, materijalne/robne kartice, kalkulacije veleprodaje, primopredaja/zaduženje/razduženje (novo polje `VrstaDokumenta`), računi-otpremnice, nivelacije cena.
- **Ispravka**: Uvoz iz DOS sistema sada automatski aktivira novouvezenu firmu na kraju uvoza (ranije je ostajala neaktivna dok je korisnik ručno ne izabere).

### 📊 Radna tabla — nove KPI pločice
- Broj magacina, broj nezaknjiženih dokumenata (kalkulacije, računi, primopredaje, nivelacije), ukupno fakturisano, broj računa/kalkulacija, vrednost zaliha, negativna stanja.

### ⚠️ Migracije i Baza Podataka
- `20260727152534_DodajPoreskeTarife` — nova tabela `PoreskeTarife`; `MagacinId`/`ArtikalId` na stavkama Nivelacije/Računa-Otpremnice postaju nullable (dozvoljava uvoz legacy redova koji nemaju razrešivu vezu ka šifarniku).

---

## [1.0.16] - 2026-07-26

### 🚀 Zvanični Finansijski Izveštaji za APR (Bilansi)
- **Bilans Stanja (`BilansiView`)**: Obračun AOP pozicija za Aktivu (Klasa 0–2) i Pasivu (Klasa 3–4) na izabrani datum, uz automatsku proveru ravnoteže (`Aktiva == Pasiva`) i upozorenje u realnom vremenu.
- **Bilans Uspeha (`BilansiView`)**: Obračun AOP pozicija za Poslovne prihode (Klasa 6), Poslovne rashode (Klasa 5), Finansijske prihode/rashode i obračun Neto dobitka ili gubitka perioda.
- **PDF Štampa Bilansa**: Izvoz zvaničnih PDF izveštaja Bilansa Stanja i Bilansa Uspeha sa AOP pozicijama i zaglavljem firme (`GenerisiBilansStanjaPdf`, `GenerisiBilansUspehaPdf`).

### 🧾 PDV Evidencija (KIR, KPR i POPDV)
- **Knjiga Izdatih Računa (KIR)**: Automatsko prikupljanje proknjiženih računa-otpremnica (`RacunOtpremnica`), kupaca, PIB-a i raščlanjivanje osnovica i PDV-a (20%, 10%, 0%).
- **Knjiga Primljenih Računa (KPR)**: Automatsko prikupljanje proknjiženih ulaznih kalkulacija (`Kalkulacija`) i prijemnica dobavljača sa raščlanjavanjem prethodnog PDV-a koji se može odbiti.
- **POPDV Rekapitulacija Obaveze**: Obračun konačne PDV obaveze (`PdvRazlika = KirUkupanPdv - KprUkupanPdv`) sa vizuelnom karticom (crvena pozadina za obavezu za uplatu / zelena za povraćaj/preplatu).
- **PDF Štampa KIR / KPR**: Zvanični pejzažni (Landscape A4) PDF izvoz Knjige izdatih i Knjige primljenih računa.

### 🛒 Robno i Materijalno Knjigovodstvo (ROB & MAT)
- **Fakture / Računi-Otpremnice (MAT5)**: Izdavanje faktura kupcima sa rokom dospelosti, rabatima %, PDV-om, automatskim razduženjem zaliha i finansijskim nalogom u Glavnoj knjizi (`RacunOtpremnicaEditWindow`).
- **Nivelacije cena (MAT7)**: Promena prodajnih cena artikala u magacinu sa proračunom razlike i PDF zapisnikom (`NivelacijaEditWindow`).
- **Primopredaje / Interni prenosi (M4)**: Interni prenos materijala iz dajućeg u primajući magacin sa automatskim proračunom ponderisane prosečne cene (`PrimopredajaEditWindow`).
- **Maloprodajne vs Veleprodajne Kalkulacije**: Dodat `CmbTipKalkulacije` selector u `TrgovinaView` za razdvajanje Veleprodaje (`MAT6`) i Maloprodaje (`MAT3`).

### 📊 Bruto Bilans 6 Kolona (FIN2)
- **Rigorozna 6-kolonska struktura**: Proširenje Bruto Bilansa na tačno 6 kolona (Promet Duguje, Promet Potražuje, Saldo Duguje, Saldo Potražuje) sa međuzbirovima po 3-cifrenim sintetičkim kontima i klasama, sa 100% poklapanjem sa Clipper txt ispisima.

### 🎨 UI / UX i Korisnički Help
- **Osvežena Pomoć (`PomocView.xaml`)**: Kompletno ažurirane teme za Robno, Materijalno, Bilanse, PDV i prečice.
- **Globalna `Esc` prečica**: Podržan taster `Esc` za brzi izlaz iz svih novih modalnih prozora (Fakture, Nivelacije, Primopredaje).

### ⚠️ Migracije i Baza Podataka
- `20260726193016_DodajKalkulacijaStavke` — Dodate stavke kalkulacije za robne kartice.
- `20260726201947_AddRacunOtpremnicaAndNivelacija` — Dodate tabele `RacuniOtpremnice`, `RacunOtpremnicaStavke`, `NivelacijeCena`, `NivelacijaStavke`.

---

## [1.0.15] - 2026-07-26

### 🚀 Šifarnik opisa promena u unosu naloga i Esc navigacija
- **Brzi prozor Šifarnika opisa (`PromeneWindow`)**: Pored dugmeta za pretragu konta (`F2`) dodato je dugme **`📝 Šifarnik opisa`** u prozoru unosa naloga. Omogućava instant pretragu, dodavanje, izmenu i brisanje standardnih opisa promena bez napuštanja naloga sa automatskim osvežavanjem padajuće liste.
- **Unapređena `Esc` navigacija**: Taster `Esc` i dugme `Otkaži (Esc)` (sa `IsCancel="True"`) sada zatvaraju sve prozorčiće i dijaloge u aplikaciji (`NalogEditWindow`, `PromeneWindow`, `KontoPickerWindow`, `NalogHelpWindow`).

### 🎨 UI / UX i uočljivost fokusirane ćelije
- **Čist narandžasti okvir aktivne ćelije**: Refaktorisana uočljivost fokusirane ćelije u tabelama u `App.xaml` — uklonjena žuta podloga (`#FEF08A`) i definisan jasan 2px narandžasti okvir (`#D97706`) bez promene pozadine celog reda.
- **Povezana padajuća lista u tabeli stavki**: Povezan `DataContext` u `NalogEditWindow.xaml` tako da `ComboBox` u koloni Opis nudi punu listu opisa iz šifarnika `Promene` ili podrazumevanog šifarnika.

### 📖 Automatsko uvoženje i dekodiranje opisa promena
- **Redosled uvoza DOS baza**: Uvoz šifarnika `PROMENE.DBF` pomeren pre uvoza naloga u `DosImportService.cs` i `DbfImportService.cs`, čime se brojevi promena (`PROMENA`) iz `NALOG.DBF` automatski prevode u tekstualne nazive.
- **Auto-popravka opisa u nalozima**: Pri otvaranju naloga i kartica konta, sistem automatski prepoznaje `PromenaKod` i zamenjuje tekualni broj dokumenta u polju Opis sa odgovarajućim nazivom promene iz baze.

### 📄 Izveštaji i PDF Štampa
- **Diferencijacija kolona Opis i Promena**: Na štampi Dnevnika knjiženja, Kartice konta i Naloga u `PdfReportService.cs` i `KarticaService.cs`, kolona **Dokument / Opis** prikazuje stvarni broj dokumenta (npr. `RN 5/26 OD 08.06.2026`), dok kolona **Promena** prikazuje naziv promene (`PO RACUNU`, `IZVOD`, `UPLATE`, `ULAZI`...), čime je sprečeno dupliranje identičnih podataka na štampi.

---

## [1.0.14] - 2026-07-26

### 🔍 Brza pretraga konta (F2) i unosi sa tastature u nalozima
- **Modalan dijalog pretrage kontnog plana (`F2`)**: Pritisak na taster `F2` ili klik na dugme `Pretraga konta (F2)` otvara brzi pretraživač `KontoPickerWindow` za instant pretragu konta po šifri, nazivu ili starom kontu.
- **Kombinovani prikaz konta u tabeli stavki**: Kolona "Konto (F2)" u tabeli stavki prikazuje puni opis `BrojKonta - NazivKonta`.
- **Ekspresni unos sa tastature (`Insert` / `Enter` / `Tab`)**: `Enter` ili `Tab` na poslednjoj ćeliji reda automatski otvara novu stavku i fokusira polje Konto.
- **Smart auto-balans salda**: Pri kreiranju nove stavke, sistem izračunava neizbalansiranu razliku salda i automatski predlaže iznos razlike na suprotnoj strani.
- **Auto-prepisivanje**: Nova stavka automatski preuzima broj dokumenta, opis i partnera iz prethodne stavke.
- **Pomoćni dijalog (`F1`)**: Poseban prozor `NalogHelpWindow` sa detaljnim uputstvom i opisom svih prečica na tastaturi.

### 🎨 UI / UX i uočljivost u tabelama
- **Visoka uočljivost aktivne ćelije**: Podešen `DataGridCell` stil sa uočljivom žutom podlogom (`#FEF08A`) i 2px amber okvirom za aktivnu/fokusiranu ćeliju.
- **Responsivan toolbar u nalozima**: Zamenjen kruti `StackPanel` sa `WrapPanel` kontejnerom u `NaloziView.xaml` za pravilno prelamanje dugmadi na manjim širinama.
- **Kontekstni meni na desni klik**: Dodat `ContextMenu` i auto-selektovanje reda u listama Naloga i Kontnog plana.
- **Pravila za onemogućavanje dugmadi**: Onemogućeno "Proknjiži sve" kada je označen filter `Proknjiženi` ili kada su svi nalozi proknjiženi.
- **Rasknjižavanje pri izmeni**: Dijalog sa upitom za rasknjižavanje i proverom administratorskih prava pri pokušaju izmeni proknjiženog naloga.

### 📄 PDF Izveštaji
- **Zbijeniji redovi tabela**: Smanjena vertikalna padding margina na `PaddingVertical(2).PaddingHorizontal(4)` radi većeg kapaciteta i veće preglednosti štampanih izveštaja.

---

## [1.0.13] - 2026-07-26

### 📝 Šifarnik opisa promena (PROMENE.DBF) i UX poboljšanja
- **Uvoz i šifarnik Opisa promena**: Podrška za uvoz šifarnika opisa promena iz legacy DOS `PROMENE.DBF` baze sa upravljanjem u podešavanjima.
- **Grupisanje menija i UX doterivanja**: Poboljšana navigacija sa tematskim grupisanjem modulskih stavki.

---

## [1.0.12] - 2026-07-26

### 📊 Period filteri i zaključni list
- **Bruto bilans** dobija filtere "Od"/"Do" datuma (podrazumevano 1.1. tekuće godine - danas, isti default kao legacy `brut_bil`, FIN2.PRG:1601).
- **Novi izveštaj "Zaključni list"** — totali po sintetičkim (3-cifrenim) kontima, analogno legacy "T O T A L sintetičkog konta" sabircima.
- **Kartica konta** (ekran Kartice) dobija iste "Od"/"Do" filtere za prikaz i PDF štampu — saldo se i dalje računa preko cele istorije (kao legacy poc_dug/poc_pot preneto stanje), filter samo sužava koji redovi se prikazuju.

---

## [1.0.11] - 2026-07-25

### 🏢 Firme prerađene po uzoru na SredstvaApp (1 baza = 1 firma)
- **Ekran "Firme" sada skenira Baze folder** umesto da čita/piše `Firma` red u trenutno otvorenoj bazi — svaki `.db` fajl je jedna firma, sa ugrađenim desnim panelom "Detalji firme" za unos/izmenu (posebni dijalog `FirmaEditWindow` uklonjen). "⭐ Aktiviraj" sada stvarno prebacuje aktivnu bazu i restartuje aplikaciju (kao Sredstva `BtnAktivna_Click`); "🗑️ Briši" fizički briše bazu te firme.
- **Uklonjen suvišan status "Firma je aktivna"** — u novom modelu svaka firma je puna, samostalna baza; postojala je zabuna sa "U upotrebi" pločicom koja već pokazuje koja je trenutno otvorena.
- **Lista firmi pojednostavljena** na Šifra / Naziv / U upotrebi / Akcije — detaljna polja (PIB, adresa, žiro račun...) žive samo u desnom panelu, ne duplirano u tabeli.
- **Pristup premešten**: bočni "Aktivna firma" okvir u meniju je sada klikabilan i otvara upravljanje firmama (kao u Sredstvi); posebna stavka menija "🏢 Firme" uklonjena.
- **Ispravljeno mapiranje KORISNIC.DBF** (izvor za listu DOS firmi pri uvozu): polje `UL` nosi celu vrednost "Ulica i broj" a `BR` (uprkos imenu) nosi "Mesto i post. br." — potvrđeno u `FIN2.PRG`. Adresa/Mesto se ranije nisu uvozili zbog pogrešnih imena kolona.

### 📊 Radna tabla — grafikoni
- Dodata 3 grafikona (LiveCharts, paket je već bio uključen ali nekorišćen): status naloga (proknjiženi/neproknjiženi), promet po kontu (Top 10), top 5 partnera po prometu. Postojeći sadržaj (poslednji nalozi, podaci o firmi) ostaje odmah ispod kartica sa brojevima, grafikoni ispod toga.
- Ispravljen broj u kartici "PROKNJIŽENI NALOZI" — ranije je brojao sve naloge, ne samo proknjižene.

### ✏️ Kontni plan
- `KontoEditWindow` (unos/izmena konta) sada ima i polja Stari konto, Ulica, Mesto, Žiro račun, Telefon (ranije vidljiva samo u tabeli, ne i u formi za unos/izmenu).

---

## [1.0.10] - 2026-07-25

### 📋 Kolone kontnog plana i ispravke ekrana za uvoz DOS podataka
- **Prikaz svih uvezenih kolona u Kontnom planu**: `KontaView` sada prikazuje i stari konto, ulicu, mesto, žiro račun i telefon (uvezene iz KONTPLAN.DBF u v1.0.9, ali dosad neprikazane u tabeli). Kolona "Naziv konta" vraćena na čitljivu širinu (bila se stisla na par piksela pošto su nove kolone pojele preostali prostor).
- **Ispravljen "Izaberi sve" u dijalogu uvoza**: checkbox-ovi za izbor firmi se sada vizuelno ažuriraju odmah (`DbfFirmaDto` sada šalje `PropertyChanged` obaveštenje); ranije je osnovni podatak bio tačno postavljen ali se prikaz na ekranu nije osvežavao za već iscrtane redove.
- **Ispravljena šifra firme u dijalogu uvoza**: prikazivala se npr. `KOR1` umesto `KOR01` (nepodudaranje sa stvarnim nazivom foldera).

### 🗂️ Baze podataka premeštene u zaseban Baze folder (po uzoru na SredstvaApp)
- **Uvoz firmi više ne piše bazu u DOS folder firme** (`C:\KNJIGE\Radni\KORxx\`) — taj folder je izvor za reimport koji samostalni `AccountingMigration` alat po potrebi briše i pravi iznova, pa je živa baza tamo bila izložena riziku od tihog gubitka podataka. Baze sada žive u `%LocalAppData%\AccountingApp\Baze\`, imenovane `firma_{Šifra}_{Naziv}.db`.
- **Jednokratna migracija postojeće baze**: pri prvom pokretanju posle nadogradnje, ako živa baza i dalje sedi na staroj DOS lokaciji, automatski se premešta u Baze folder (analogno `SredstvaApp.AppConfig.PrilagodiNazivZajednickeBaze`) — bez gubitka podataka, testirano na KOR01 (3.207 konta, 338 naloga pre i posle migracije identično).

---

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
