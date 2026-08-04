# 📋 Istorija izmena (Changelog) — ERPiFinansije

Sve značajne promene i novine u aplikaciji **ERPiFinansije** dokumentovane su u ovom fajlu.

Format je zasnovan na [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) standardu i prati Semantic Versioning.

## [1.5.0] - 2026-08-05

### 🔍 Pretraga po šifri i nazivu u ćeliji artikla (Primopredaje/Zaduženja/Razduženja) i polju kupca (Ponude)
- **Artikal u `PrimopredajaEditWindow`** (deljen za tabove Primopredaje/Zaduženja/Razduženja u Trgovini) je bio obična padajuća lista bez pretrage, sa samo nazivom artikla — nemoguće pronaći artikal po šifri, a bez ijedne pretrage kroz nekoliko hiljada stavki šifarnika. Kolona je zamenjena template kolonom (isti obrazac kao `ColKonto` u `NalogEditWindow`): kucanje filtrira listu uživo po šifri ili nazivu, strelice gore/dole/PageUp/PageDown biraju kroz filtrirane rezultate, a Enter/Tab/klik mišem potvrđuju izbor. Ćelija van režima izmene sad prikazuje „šifra - naziv" (`PrimopredajaStavkaModel.PrikazArtikla`) umesto samo naziva.
- **Kupac / Partner u `PonudaEditWindow`** je isto bio obična nepretraživa lista (samo naziv). Novi `PartnerPicker` (analogan postojećem `KontoPicker` za konta) čini polje editable sa pretragom po šifri, nazivu i PIB-u dok se kuca; `Partner` dobija `Prikaz` computed property („šifra - naziv"), isti obrazac kao `Artikal`/`Magacin`/`Konto`.
- Verifikovano UI automatizacijom (`run-accounting-app` drajver): otvoren nalog Zaduženja, otkucano „rep" u ćeliji artikla, strelica dole + Enter je izabrala „R-02060 - reparatur malter"; otvorena postojeća ponuda, Kupac prikazuje „202094 - beles trans", polje prima kucani unos.
- Isti nepretraživi obrazac (`DisplayMemberPath="Naziv"`) i dalje postoji na artikal/kupac poljima u drugim prozorima (npr. `RacunOtpremnicaEditWindow`, `KalkulacijaEditWindow`, `PonudaEditWindow.CmbArtikli`) — nije dirano u ovom koraku, ostaje za kasnije ako zatreba ista doslednost.

## [1.4.9] - 2026-08-04

### 🔄 Primopredaja veleprodaja↔maloprodaja pravi nalog u Glavnoj knjizi (`PrimopredajaService`, `PrimopredajaNalog`)
- **Zaduženje/Razduženje prodavnice** (Trgovina, dokumenti nad istom `PrimopredajaNalog` tabelom kao obična Primopredaja — vidi `ANALIZA_I_PLAN.md` §9.1) je do sada dizalo samo materijalnu karticu. Kad magacin koji daje i magacin koji prima nisu iste vrste (veleprodaja vodi robu bez PDV na `1320`, maloprodaja sa PDV na `1340`), prenos je menjao zalihu bez ijednog traga u knjigovodstvu.
- Novo polje **`PrimopredajaNalog.StopaPdv`** (podrazumevano 20%, uneseno na nalogu — analogno jedinstvenoj stopi po dokumentu kod `MaloprodajnaKalkulacija`) — polje i napomena se u `PrimopredajaEditWindow` prikazuju samo kad izabrani magacini prelaze VP↔MP granicu.
- Pri knjiženju: osnovna vrednost ide sa konta magacina koji daje na konto magacina koji prima (`1320`↔`1340`), a razlika ide na ukalkulisani PDV (`1344`/`13441`, po stopi) — bez konta dobavljača, jer je ovo interni prenos robe, ne nabavka. Rasknjiženje uklanja i redove kartice i ovaj nalog, simetrično ostalim robnim dokumentima.
- **Namerno ne dira** ukalkulisanu razliku u ceni (`1329`/`1348`) — ta rekvalifikacija je zaseban, još neurađen korak (periodični obračun razlike, `ANALIZA_I_PLAN.md` §9.1 "Otvoreno").
- Pokriveno sa 3 nova testa u `PrimopredajaServiceTests` (ista vrsta magacina ne pravi nalog; VP→MP dodaje PDV i pravi uravnotežen nalog; rasknjiženje uklanja i karticu i nalog).

## [1.4.8] - 2026-08-04

### 👥 Zatvaranje stavki i istorija zatvaranja rade i za legacy konto bez promocije (`ZatvaranjeStavkiService`)
- Ručno zatvaranje otvorenih stavki i istorija zatvaranja su i dalje bile blokirane za sintetičke (legacy) partnere (`PartnerId=0`), iako je [1.4.7](#147---2026-08-04) uvela mogućnost "promocije" u pravog partnera. Blokada je uklonjena bez uslova promocije — `ZatvaranjeStavkiWindow` sad učitava otvorene stavke preko `GetOtvoreneStavkeZaKontoAsync` kad partner nema `PartnerId`, a nova `GetIstorijaZatvaranjaZaKontoAsync` radi isto za istoriju. Samo zatvaranje (`ZatvoriGrupnoAsync`) je oduvek radilo na nivou stavki naloga, bez zavisnosti od partnera.
- Promocija partnera ([1.4.7](#147---2026-08-04)) ostaje preporučen put za PIB/matični broj i NBS verifikaciju, ali ubuduće više nije preduslov za zatvaranje IOS stavki.

## [1.4.7] - 2026-08-04

### 🌍 Kursna lista NBS ponovo radi — stari izvor je ugašen (`NbsApiClient`)
- **Preuzimanje je tiho vraćalo 0 valuta za bilo koji datum.** Uzrok nije bio nedostatak podataka nego mrtav endpoint: stari javni feed `www.nbs.rs/net/xmlrs/kursnaLista.xml` vraća `404 Page Not Found`, ne prazan XML — NBS ga je ugasio.
- Zvaničan programski pristup postoji samo kroz registrovani "Sistem veb-servisa NBS" (`webservices.nbs.rs`, prijava pravnog lica). Dok se taj pristup ne obezbedi, `NbsApiClient` sada parsira HTML tabelu nove NBS web-app forme (`webappcenter.nbs.rs/ExchangeRateWebApp`) — `ExchangeRateListTypeID=3` za zvanični srednji kurs (jedina vrednost koju knjigovodstvo koristi za preračun), `=1` za kupovni/prodajni prikaz u Kalkulatoru, sa `sr-Latn` kolačićem da nazivi valuta ostanu na latinici.
- Krhko po prirodi (HTML scraping) — ako NBS ponovo redizajnira formu, treba ažurirati parsiranje.

### 👥 Partneri: sintetički (legacy) partneri se mogu "promovisati" u prave (`PartnerPromocijaService`, `PartnerEditWindow`)
- **Većina partnera u šifarniku su "sintetički" zapisi** (`PartnerId=0`) izvedeni direktno iz kontnog plana — legacy DBF uvoz nikad nije kreirao pravi red u tabeli Partneri za njih. Zato su za njih bili blokirani zatvaranje otvorenih stavki, istorija zatvaranja i NBS verifikacija računa (nedostaju PIB/matični broj), bez ikakvog načina da se to popravi iz aplikacije.
- Desni klik na partnera u listi → **"✏️ Izmeni podatke/konto"** otvara `PartnerEditWindow`. Za sintetičkog partnera, čuvanje kreira pravi red u Partneri (idempotentno — ne dupira ako je već promovisan) **i povezuje (backfill) sve dosadašnje stavke naloga tog konta** koje još nemaju `PartnerId`, tako da zatvaranje stavki, istorija i verifikacija odmah rade i za uvezenu istoriju. Menja se samo veza (`PartnerId`), nikad iznosi/datumi već proknjiženih stavki.

### 🖥️ Sitnije popravke ekrana Partneri i Obračuna kamate
- **Obračun kamate**: naslov partnera i "Datum obračuna" su preklapali dugmad kad je naziv partnera dug (`KamataWindow`) — `StackPanel` bez ograničenja širine je puštao sadržaj preko granice kolone. Zamenjen `Grid`-om sa pravim kolonama; elipsa na dugom nazivu sada stvarno radi.
- Dugme **"💰 Obračun kamate"** je disable-ovano kad izabrani konto partnera nije konto kupca (204/120) — kamata na dobavljača nema smisla.
- Dodata radio dugmad **Svi / Kupci / Dobavljači** za filtriranje liste partnera (uz postojeću pretragu).
- Padajuća lista konta na "Analitičkoj kartici" se prikazuje kao običan tekst kad partner ima samo jedan konto — dropdown strelica bez izbora je samo zbunjivala; ostaje kao combo kad partner vodi više konta (npr. i kupac i dobavljač).

## [1.4.6] - 2026-08-04

### 🐛 Ispravka pada pri otvaranju baze bez istorije migracija (`AccountingDbContext`)
- **Aplikacija je padala sa `SQLite Error 1: 'table "Firme" already exists'`** kad je kao aktivna baza bila otvorena datoteka koju je kreirao drugi modul (npr. ERPi Zarade), ili baza čija je `__EFMigrationsHistory` tabela bila prazna. EF Core nije video nijednu primenjenu migraciju i pokušao da napravi `Firme` i ostale tabele koje već postoje.
- **Uzrok:** `AccountingDbContext.Create` je zvao `ctx.Database.Migrate()` direktno, bez provere da li baza već ima šemu ali ne i evidenciju migracija.
- **Popravka:** dodate tri privatne metode (`InitializeDatabase`, `PostojiZatecenaSemaBezMigracija`, `OznaciSveMigracijeKaoPrimenjene`) po uzoru na isti, već dokazan obrazac u `PlataDbContext` (ERPi Zarade). Pre `Migrate()` se proverava: ako baza postoji, sadrži tabele, ali nema ni jednu primenjenu migraciju — sve poznate migracije se upisuju u `__EFMigrationsHistory` bez izvršavanja (`INSERT OR IGNORE`), pa `Migrate()` primenjuje samo stvarno nove izmene šeme.
- Pokrivena su tri scenarija: nova baza (sve od nule), zatečena baza bez istorije (sva migracijska evidencija se žigoše), baza sa potpunom istorijom (standardna nadogradnja).

## [1.4.5] - 2026-08-04

### 🧾 Ukalkulisani PDV maloprodaje ide na konto svoje stope (`RobnaKonta`, `MaloprodajnaKalkulacijaService`)
- Kontni plan drži **dve analitike ukalkulisanog PDV-a** — `1344` za opštu i `13441` za posebnu stopu — ali je knjiženje maloprodajne kalkulacije uvek išlo na `1344`. Kalkulacija po nižoj stopi je time završavala na kontu opšte stope, pa bi obe stope stajale pomešane na istom saldu.
- Konto se sada bira po poreskoj stopi dokumenta (`UkalkulisaniPdvZaStopu`). Prag je isti kao u `PdvService` (≥18% je opšta stopa), da bi i **istorijske stope 18%/8%** iz uvezenih baza pale na ista konta kao današnje 20%/10%.

### 🧹 Uklonjeni zaostali snimci ekrana iz korena repozitorijuma
- `login.png`, `login2.png`, `recover.png`, `robne_kartice3.png`, `robne_kartice_max.png` — radni snimci koji nisu nigde referencirani.

## [1.4.3] - 2026-08-04

### ➕ Eksplicitno dugme za novu stavku u šifarniku opisa promena (`PromeneWindow`)
- **Dodavanje nove šifre/opisa je već radilo, ali samo ako se red u gridu slučajno deselektuje** (klikom u prazan prostor ispod poslednjeg reda) — dugme „Sačuvaj" je inače uvek izgledalo kao izmena selektovane stavke.
- Dodato dugme „➕ Nova stavka" pored pretrage: deselektuje grid, čisti formu (šifra se auto-popuni sledećim slobodnim brojem) i fokusira polje za opis, tako da je dodavanje nove stavke očigledna, nezavisna radnja.

## [1.4.2] - 2026-08-04

> Nastavak na 1.4.0/1.4.1: popunjava rupe nađene pri proveri veze robno → finansijsko
> knjigovodstvo — kupac na fakturi je bio slobodan tekst i konto mu je bio zakucan,
> prodaja nije razduživala zalihu, a uvozna kalkulacija nije mogla da se rasknjiži.

### 🧾 Kupac na računu-otpremnici je padajuća lista iz kontnog plana (`RacunOtpremnicaEditWindow`, `KontoPicker`)
- **Polje „Kupac / Konto" je bilo slobodan tekst.** Sada je pretraživa lista konta grupe kupaca (**204**, odnosno **120** kod firmi prenetih sa starog zakona) preko istog `KontoPicker`-a koji već bira konto dobavljača na kalkulaciji — traži se i po broju i po nazivu, a konto van grupe se i dalje može uneti rukom.
- **Nalog knjiženja je duguio zakucan konto `2040`** bez obzira šta je uneto u polje kupca. Sada duguje izabrani konto sa dokumenta; `2040` ostaje samo kao podrazumevana vrednost za račune snimljene pre ove izmene (prazno `KontoKupca`).

### 📤 Račun-otpremnica razdužuje robnu karticu i knjiži nabavnu vrednost prodate robe (`RacunOtpremnicaService`, `RacunOtpremnicaEditWindow`)
- **Prodaja nije diralа zalihu.** Račun-otpremnica je knjižio samo prihod (`6120`) i PDV (`4700`) — roba je ostajala na stanju iako je prodata, a pomoć u prozoru je i dalje obećavala „automatsko razduženje zaliha".
- **Dodat je konto Magacin (obavezan) na formu računa.** Pri knjiženju se za svaku stavku upisuje izlazni red u materijalnu karticu, po **prosečnoj (nabavnoj) ceni** — isto načelo kao Trebovanje/Primopredaja preko `MaterijalnaKarticaService`, ne po prodajnoj ceni sa fakture.
- **Nalog dobija dve nove stavke**: `5010` (nabavna vrednost prodate robe) duguje, konto robe potražuje — `1320` za veleprodajni magacin, `1340` za maloprodajni (`RobnaKonta.RobaZaVrstuMagacina`).
- **Rasknjižavanje uklanja i redove kartice**, obrnutim redosledom od knjiženja, uz istu proveru kao kod ostalih dokumenata — baca grešku ako je za neki artikal u međuvremenu knjiženo nešto kasnije, da se ne bi pokvarilo stanje/prosečna cena za naloge posle njega.

### 🌍 Uvozna kalkulacija se može rasknjižiti (`UvoznaKalkulacijaService`, `UvoznaKalkulacija`)
- **Nalog knjižen pri uvozu se nije mogao ukloniti.** Za razliku od (maloprodajne) kalkulacije, nivelacije i računa-otpremnice, `UvoznaKalkulacija` nije čuvala `NalogId` — jednom proknjižen nalog na `1300`/`4350`/`4330` ostajao je trajno, bez načina da se ispravi greška u unosu.
- `UvoznaKalkulacija.NalogId` (nova kolona, migracija `DodajNalogIdUvoznaKalkulacija`) i `UvoznaKalkulacijaService.RasknjiziUvozAsync` prate isti obrazac kao ostala robna dokumenta. **Za sada samo na nivou servisa** — `UvoznaKalkulacijaWindow` je i dalje unos-samo-za-knjiženje prozor bez liste postojećih uvoza, pa dugme za rasknjižavanje u interfejsu nije dodato u ovom koraku.

### 📖 Pregled: konta robno → finansijsko i veza dokumenta sa nalogom
Vidi `ANALIZA_I_PLAN.md` §9 za tabelu svih konta po tipu robnog dokumenta i objašnjenje kako se čuva veza sa nalogom (`NalogId`) — uključujući i dalje otvorene stavke (obračun razlike u ceni, dnevni pazar, primopredaja VP→MP, uvozna kalkulacija bez kartice).

## [1.4.1] - 2026-08-04

> Ispravke na knjiženju kalkulacija uvedenom u 1.4.0.

### 🧾 Ukalkulisani PDV prati poresku stopu kalkulacije (`RobnaKonta`, `MaloprodajnaKalkulacijaService`)
- **Maloprodajna kalkulacija po stopi 10% završavala je na kontu opšte stope.** Kontni plan drži dve analitike ukalkulisanog PDV — `1344` (20%) i `13441` (10%) — a knjiženje je uvek išlo na `1344`, bez obzira na `PoreskaStopaProcenat` dokumenta. Saldo oba konta bi bio pogrešan, a nalog i dalje u ravnoteži, pa se greška ne bi sama primetila.
- Konto se sada bira preko `RobnaKonta.UkalkulisaniPdvZaStopu(stopa)`, sa istim pragom kao u `PdvService` (≥18% je opšta stopa) — tako i istorijske stope 18%/8% iz uvezenih baza padaju na ista konta kao današnje 20%/10%.

## [1.4.0] - 2026-08-04

> Kalkulacije. Uvoz iz DOS-a čitao je samo deo kolona iz `KAL_NAL.DBF` — razlika u ceni, porez,
> prodajna vrednost i prodajna cena po jedinici ostajali su na nuli na **svakoj** uvezenoj stavci.
> Maloprodajne kalkulacije se nisu uvozile uopšte, a nijedna kalkulacija nije ulazila u Glavnu knjigu.

### 📒 Kalkulacije se knjiže u glavnu knjigu (`KalkulacijaService`, `MaloprodajnaKalkulacijaService`, `RobnaKonta`)
- **Kalkulacije su bile jedina robna dokumenta koja ne dodiruju glavnu knjigu.** Knjiženje je upisivalo red u robnu karticu i postavljalo `IsKnjizen`, ali nije pravilo nalog — pa nabavljena roba nije ulazila ni u jedan saldo, iako to rade nivelacije, računi-otpremnice, primopredaje, ulazi, trebovanja i uvozne kalkulacije.
- **Konta nisu uzeta iz Kontnog okvira nego očitana iz zatečenih naloga ovih firmi**, jer se analitika razlikuje od firme do firme (`RobnaKonta`):
  - **Veleprodaja** — `1320` duguje (nabavno + razlika, tj. prodajna vrednost **bez** PDV), `1329` potražuje (razlika u ceni), konto dobavljača potražuje (svega nabavno). Obrazac iz naloga 410 „KALK.3 OD 04.12.02".
  - **Maloprodaja** — `1340` duguje (prodajna vrednost **sa** PDV), `1344` potražuje (ukalkulisani PDV), `1348` potražuje (ukalkulisana razlika u ceni), konto dobavljača potražuje. Obrazac iz 123 naloga sa opisom „KALKULACIJA NA MALO".
  - Veleprodaja namerno **nema ukalkulisani PDV** — to je „korak više" koji ima samo maloprodaja, pa se roba u stovarištu vodi po ceni bez poreza.
  - Razlika u maloprodaji ide na **1348**, ne na 1349: u kontnom planu postoje oba, ali svih 143 zatečenih knjiženja idu na 1348, a 1349 nema nijednu stavku.
- **Rasknjižavanje uklanja nalog** — `Kalkulacija` i `MaloprodajnaKalkulacija` sada nose `NalogId`, kao što ga `NivelacijaCena` već ima.
- **Kalkulacija bez konta dobavljača se i dalje knjiži, ali bez naloga** — bez protivstavke nalog ne bi bio u ravnoteži, a kalkulacije iz starijeg DBF uvoza umeju da nemaju dobavljača.
- **Napomena:** nalog pokriva robnu stranu i obavezu prema dobavljaču u neto iznosu. **Pretporez (`270`) i bruto obaveza po ulaznom računu nisu deo ovog naloga** — u dosadašnjoj praksi firme to je zaseban blok stavki („racuni") u istom nalogu. `Kalkulacija` ne nosi iznos PDV-a sa ulaznog računa, pa se to ne može izvesti iz nje.

### 🏪 Maloprodajne kalkulacije se prepoznaju i mogu se prebaciti iz veleprodajnih
- **Legacy drži obe vrste u istom fajlu.** `KALKULAC.DBF` se koristi i za nabavku u stovarište i za nabavku pravo u prodavnicu — vrsta se vidi tek po magacinu, pa je uvoz sve svrstavao u veleprodajne. U ARHIBEL-u su tako sve 128 kalkulacija (magacin „Magacin maloprodaje") završile kao veleprodajne, a knjiže se na maloprodajna konta.
- **Uvoz sada čita vrstu magacina iz naziva** (`DbfImportService.VrstaIzNaziva`). `MAGACIN.DBF` ima samo `SIFRA` i `RACUNOPOL` — nema polje za vrstu, pa je naziv jedini trag: „Magacin maloprodaje" / „Prodavnica" → maloprodaja. Ranije je **svaki** uvezeni magacin bio veleprodajni, zbog čega su i nivelacije i kalkulacije išle na veleprodajna konta.
- **`KalkulacijaService.PrebaciUMaloprodajuAsync` / `PrebaciSveUMaloprodajuAsync`** prebacuju dokument sa svim stavkama iz veleprodajnih u maloprodajne kalkulacije. Bez dugmeta u programu — služi za jednokratnu ispravku zatečenih baza. Redovi robne kartice se ne diraju (roba je ušla u isti magacin), a kalkulacija proknjižena u glavnu knjigu se preskače dok se ne rasknjiži, da nalog na veleprodajnim kontima ne bi ostao iza nje. Kad zaglavlje nema magacin (baze uvezene pre nego što je `MAG_PRIMA` mapiran), magacin se očitava iz redova robne kartice.
- **Nabavka pravo u prodavnicu se konačno može proknjižiti.** Maloprodajno knjiženje je pretpostavljalo prenos iz veleprodaje i tražilo „magacin (daje)", pa je kalkulacija bez njega bila odbijena. Sada, kad magacina koji daje nema a magacin koji prima postoji, roba **ulazi** u prodavnicu po maloprodajnoj ceni; rasknjižavanje uklanja isti red.

### 🏷️ Nivelacija u maloprodaji više ne knjiži razliku na veleprodajni konto (`NivelacijaService`)
- Konto robe se pravilno granao na maloprodaju (`1340`) i veleprodaju (`1320`), ali je **konto razlike bio zakucan na `1329` („RAZLIKA U CENI ROBE U STOVARISTU") i za maloprodajne nivelacije**. Razlika iz prodavnice je time završavala na veleprodajnom kontu — oba salda pogrešna, a nalog i dalje u ravnoteži, pa se greška nije sama otkrivala. Sada prati vrstu magacina (`1348` za maloprodaju).

### 🧮 Uvoz kalkulacija iz DOS-a čita sve kolone (`DbfImportService`, `DosImportService`)
- **Stavke veleprodajnih kalkulacija su se uvozile poluprazne.** Uvoznik je za `KAL_NAL.DBF` tražio kolone `RAZLIKA`, `POREZ`, `PROD_VRED` i `PROD_CENA`, kojih u tom fajlu nema — stvarna imena su `RAZLIKA_IZ`, `POREZ_IZ`, `PROD_SA_P` i `PROD_PO_JM`. Zbog toga su **razlika u ceni, porez, prodajna vrednost i prodajna cena po jedinici mere ostajali na nuli na svakoj uvezenoj stavci**, pa se prodajna cena nije mogla ni proknjižiti u robnu karticu.
- **Dodate ranije nemapirane kolone** `RAZLIKA_PR` (procenat marže), `PROD_BEZ_P` (prodajna bez poreza), `POREZ_PR` (poreska stopa), `POS_P_PR`/`POS_P_IZ` (poseban porez), `PREN_POR`/`PREN_P_POR` (preneti porez), `POR_ZA_UPL` (porez za uplatu), `STARA_CENA` (cena pre kalkulacije) i `KNJIZEN` po stavci.
- **Količina se više ne zaokružuje.** `KAL_NAL.KOLICINA` je `N(12,4)`, a kolona u bazi je bila `decimal(18,2)` — sada je `decimal(18,4)`.
- **Izvedene vrednosti se dopunjuju po formulama iz `MAT6.PRG`** kad ih starija baza nema: `nabavna = iznos + troškovi`, `prod_bez_p = nabavna + razlika_iz`, `prod_sa_p = prod_bez_p + porez_iz`, `por_za_upl = porez_iz − pren_por`, `prod_po_jm = prod_sa_p / količina`.
- **Zaglavlje dobija procente marže i PDV-a.** `KALKULAC.DBF` ih ne čuva (legacy ih drži po stavci), pa se izvode iz iznosa umesto da stoje na nuli.
- **Prazni datumi otpremnice/računa ostaju prazni** umesto da se upišu kao današnji datum.

### 🏪 Maloprodajne kalkulacije se uvoze iz DOS-a (`MALKULAC.DBF`, `MAL_NAL.DBF`)
- Uvoz iz UI-ja ih **do sada uopšte nije obuhvatao** — u bazu su ulazile samo veleprodajne. Sada se uvoze zaglavlja i stavke, sa maloprodajnim dodacima: `RABAT_PR`/`RABAT_IZ`, `T_KNJIZEN` (trgovinsko knjiženje odvojeno od finansijskog), `TARIFNI`, `TAKSA`, `BR_RAZDUZ` i `NAZ_ROBE`/`JED_MERE`.
- Stavke se vezuju po **(prodavnica, broj kalkulacije)** — broj maloprodajne kalkulacije je jedinstven samo unutar prodavnice.
- **Zaostalo duplo zaglavlje više ne udvostručuje stavke**: stavke se vezuju samo za prvo zaglavlje sa datim brojem.
- **Zaglavlje sa zbirovima na nuli se dopunjuje iz stavki** (u `ARHSTO\kor03` takvih je 22 od 409) da dokument u pregledu ne bi izgledao prazan; već popunjena legacy zaglavlja se ne diraju.
- Provereno na stvarnim bazama: za `ARHSTO\kor01` zbir prodajne vrednosti stavki poklapa se sa zaglavljima u dinar (25.184.565,51), a svaki red `KAL_NAL`/`MAL_NAL` je ili uvezen ili objašnjen (legacy brojač, siroče bez zaglavlja).

### 📦 Unos kalkulacije prati DOS ekran (`KalkulacijaEditWindow`, `MaloprodajnaKalkulacijaEditWindow`)
- **Artikal se bira iz šifarnika.** Kolona „Šifra artikla" u stavkama bila je slobodan tekst — šifra se pamtila napamet i greška se videla tek posle snimanja. Sada je **padajuća lista artikala** (`šifra - naziv (JM)`) sa kucanjem šifre za pretragu, kao `osvezi_art()` u `MAT2.PRG`/`MAT3.PRG`.
- **Konto dobavljača je pretraživa lista iz kontnog plana**, a ne slobodan tekst — ponuđena su konta grupe dobavljača (**435**, odnosno **220** kod firmi prenetih sa starog zakona; `FIN1.PRG:643-649`), a traži se **i po broju i po nazivu**. Odgovara `daj_konto(2)` iz `FIN2.PRG:1226`. Konto van grupe se i dalje može uneti rukom.
- **Veleprodajna kalkulacija dobila je datum otpremnice i datum računa.** DOS ekran (`MAT6.PRG:65-68`) ima tri datuma — kalkulacije, otpremnice i računa — a prozor je nudio samo datum kalkulacije, pa su preostala dva pri ručnom unosu ostajala prazna iako `Kalkulacija` ima polja za njih.
- **Šifra magacina se vidi u padajućim listama** (`001 - CENTRALNI MAGACIN` umesto samo naziva) — u dokumentima se magacin vodi po šifri (`MAG_PRIMA`/`MAG_DAJE`), pa je ona ta koja se poredi sa papirom. Promena važi za sve liste magacina u Robnom i Materijalnom.

## [1.3.0] - 2026-08-04

> Prati **ERPiZarade 1.15.0**, koja od ove verzije knjiži refundaciju bolovanja na konta
> 225, 454, 455 i 456. ERPiFinansije nema podrazumevani kontni plan — konta stižu iz DBF
> migracije ili se unose ručno — pa bi prvi uvoz iz nove verzije stao na poruci „konto ne
> postoji", a korisnik bi četiri konta otvarao rukom, tražeći im nazive u propisu.

### 📗 Konta koja nedostaju se nude na zavođenje (`ZaradeImportService`, `ZaradeKontniOkvir`)
- Kad uvoz zarada stane zbog konta kojih nema u kontnom planu, program **prikaže spisak sa predloženim nazivima iz Pravilnika o Kontnom okviru** i zavede ih po potvrdi, pa se fajl čita ponovo i uvoz nastavlja.
- **Provera time nije zaobiđena nego rešena.** Pravilo iz 1.2.0 ostaje: proknjižen iznos na nepostojećem kontu ne bi bio ni na jednoj kartici. Posle zavođenja konto postoji, pa iznos ima svoju karticu i vidi se u bilansu.
- Nudi se **samo kad su konta jedina greška**. Nalog van ravnoteže ili tuđ fajl se zavođenjem konta ne rešavaju, pa bi ponuda tu značila da korisnik zavede konta i opet ostane bez uvoza.
- **Naziv je predlog, ne pravilo** — vidi se pre potvrde i posle se menja u „Kontnom planu" kao i kod svakog konta. Za analitiku koju firma vodi po svom (npr. `520-1`) predlaže se naziv **sintetike**; za konto koji Kontni okvir uopšte ne poznaje uzima se opis stavke iz naloga, uz napomenu da naziv nije iz propisa.
- Klasa i oznaka sintetike se izvode iz samog broja konta, istim pravilom kao pri DBF migraciji. Ponovljeno zavođenje ne pravi duplikat.

### 🩺 Šta stiže iz ERPiZarade 1.15.0
- Naknada zarade **na teret RFZO nije trošak poslodavca**: umesto na 520/450 dolazi kao **potraživanje na 225**, uz obaveze na **454** (neto), **455** (porez i doprinosi zaposlenog) i **456** (doprinosi poslodavca). Potraživanje na 225 se zatvara **izvodom posebnog računa** kad refundacija stigne od Fonda — taj korak je ovde, u ERPiFinansije.
- Format fajla je nepromenjen (verzija 1), pa stariji nalozi i dalje ulaze bez izmene.

### 🎯 Nalog otvoren iz kartice pozicionira se na kliknutu stavku (`KarticeView`, `NalogEditWindow`)
- Kad se iz kartice konta otvori nalog (dupli klik, „Pregledaj nalog" ili „Izmeni / Rasknjiži nalog"), grid stavki se sada odmah **selektuje i skroluje baš na stavku iz koje je nalog otvoren**. Kod naloga sa desetinama stavki više se ne traži ručno red sa kog se došlo.
- Kartica sada nosi i `StavkaNalogaId`/`RedniBroj` po redu (`KarticaService`, `OtvoreneStavkeService`), pa je red kartice jednoznačno vezan za stavku naloga, a ne samo za nalog.

## [1.2.0] - 2026-08-04

> Prva veza sa **ERPiZarade**: obračun zarada se knjiži u glavnu knjigu bez ručnog prepisivanja.

### 📒 Uvoz naloga za knjiženje iz ERPiZarade (`NaloziView`, `ZaradeImportService`)
- Dugme **„📒 Uvoz zarada"** na ekranu naloga učitava fajl koji ERPiZarade izveze menijem „Nalog za knjiženje". Fajl je **već nalog** — stavke, konta i iznosi su izvedeni iz obračuna, gde jedino i postoje podaci o radnicima.
- **Pri uvozu se ništa ne računa**, samo proverava i prepisuje. Svako računanje pri prenosu bilo bi drugo mesto koje ume da se raziđe sa obračunom, poreskom prijavom i nalozima za prenos.
- Nalog se prvo **pročita i pokaže** (firma, period, broj stavki, duguje/potražuje, broj koji će dobiti), pa tek po potvrdi snimi — i to kao **neproknjižen**. Knjiženje ostaje odluka korisnika, kao i kod svakog drugog naloga.
- **Uvoz se zaustavlja u tri slučaja:** fajl nije iz ERPiZarade (prepoznaje se po oznaci formata i verziji), nalog nije u ravnoteži, ili neki konto ne postoji u kontnom planu. Poslednje je namerno strogo — proknjižen iznos na nepostojećem kontu ne bi bio ni na jednoj kartici, a u bilansu bi nedostajao bez traga.
- **Mesta troška se uparuju po šifri.** Nepoznata šifra ne zaustavlja uvoz: te stavke ulaze bez podele, a podela se dobija kad se mesto troška zavede. Obaveze prema radnicima se po mestima troška ne dele.
- **Ponovljen uvoz se prijavljuje, ali ne zabranjuje** — legitiman je kad se obračun ispravi. Ako nalog istog opisa i datuma već postoji, program to javi pre potvrde, da isti nalog ne uđe u knjige dvaput nezapaženo.
- Broj naloga se dodeljuje sam, po najvećem zatečenom; datum se čita po ISO zapisu, nezavisno od regionalnih podešavanja.

## [1.1.6] - 2026-08-03

### 🐛 Izbor konta u nalogu za knjiženje (`NalogEditWindow`)
- **Filter konta više ne usporava unos.** Padajuća lista je na svaki pritisak tastera dobijala ceo kontni plan (u većim firmama i preko 3.000 konta), uz pretvaranje svakog naziva u mala slova. Sada se mala slova računaju jednom pri učitavanju, poređenje je `Ordinal`, a u listu ide najviše 100 pogodaka — konta koja **počinju** unetim brojem prikazuju se prva.
- **Konto se sada prihvata klikom i tasterom Enter**, ne samo tasterom Tab. Kolona konta je prevedena iz `DataGridComboBoxColumn` u šablonsku kolonu, jer je `SelectedValueBinding` brisao izabrani konto svaki put kad se lista filtrira (izabrana stavka tada ispadne iz liste). Enter potvrđuje konto i prelazi na kolonu Dokument, isto kao Tab.
- **Unos se potvrđuje i pri izlasku iz ćelije** — klikom na drugu ćeliju ili klikom na dugme „Snimi nalog". Time nestaje poruka „Svaka stavka mora imati unet konto" za stavku u kojoj je konto bio uredno unet.
- **Esc vraća prethodni konto**, a pretraga na `F2` preuzima ono što je do tada otkucano u ćeliji kao početni upit.

## [1.1.5] - 2026-08-02

### 🐛 Preuzimanje podataka i kada je nova verzija već pokrenuta (`AppConfig`)
- **Prazna podrazumevana baza više ne pobeđuje nad zatečenim podacima.** Ako je nova verzija već jednom pokrenuta, ona je napravila praznu `accounting.db` i upisala je kao aktivnu — pa se posle preuzimanja podataka i dalje otvarala prazna. Sada se takva baza prepoznaje (nema nijedne firme) i aktivna se vraća na firmu koja je bila otvorena pre preimenovanja.
- **Zatečena istoimena baza sa podacima se ne gubi** — preuzima se pod sufiksom `_stara`, a ako je i ona prazna podrazumevana, preskače se da se ne bi pojavila kao lažna firma u spisku.

### 🎨 Ikonica aplikacije u boji modula
- `app.ico` je regenerisan iz originalnog 1024px izvora u **slate boji** kartice modula, sa providnom pozadinom i svim veličinama do 256px (ranije samo do 64px, u istoj plavoj kao ostali moduli).

## [1.1.4] - 2026-08-02

### 🐛 Firme i baze nestale posle preimenovanja (`AppConfig`)
- **Podaci se preuzimaju iz starog foldera.** Preimenovanje u ERPi liniju promenilo je i ime foldera sa podacima (`%LOCALAPPDATA%\AccountingApp` → `%LOCALAPPDATA%\ERPiFinansijeApp`), pa je nova verzija startovala sa praznim spiskom firmi iako sve baze i dalje stoje na disku. Pri prvom pokretanju se sada **kopira ceo stari folder** — baze, rezervne kopije, podešavanja i logovi.
- **Aktivna baza se premapira** na kopiju u novom folderu, pa se aplikacija otvara na istoj firmi kao pre.
- Podaci se **kopiraju, ne premeštaju** — stara instalacija ostaje netaknuta dok se ne uverite da je sve preneto, a stari folder možete obrisati ručno. Preuzimanje se izvršava jednom i beleži se fajlom `preuzeto_iz_starog_foldera.txt`.

## [1.1.3] - 2026-08-02

### 🏷️ Preimenovanje projekta u ERPi liniju
- **Rešenje i svi projekti preimenovani**: `AccountingSystem.slnx` → `ERPiFinansije.slnx`, a projekti `AccountingApp`/`AccountingData`/`AccountingData.Tests`/`AccountingMigration` → `ERPiFinansijeApp`/`ERPiFinansijeData`/`ERPiFinansijeData.Tests`/`ERPiFinansijeMigration` (folderi, `.csproj` fajlovi, `namespace`-ovi i reference).
- **Repozitorijum i radni folder**: kod je premešten u `C:\ERPi\ERPiFinansije`, a `origin` pokazuje na `https://github.com/blagojevicboban/ERPiFinansije.git`.
- **Velopack `packId` je sada `ERPiFinansije`** (ranije `AccountingSystem`), izvršni fajl je `ERPiFinansijeApp.exe`. `ERPiHub` prepoznaje i staru i novu instalaciju, pa se na računarima sa ranijom verzijom modul i dalje vidi kao instaliran.
- Ažurirani `.github/workflows/release.yml`, `.vscode` zadaci, skills dokumentacija i README/ANALIZA_I_PLAN.

## [1.1.2] - 2026-08-02

### 🚀 Usklađivanje verzije i automatsko ažuriranje (ERPiHub)
- **Konačna verzija 1.1.2**: Čista verzija `v1.1.2` koja garantovano prevazilazi sve ranije 1.1.0/1.1.1 tagove i osigurava da `ERPiHub` i `Velopack` odmah ponude automatsko ažuriranje na svim klijentskim računarima.
- Sadrži sve najnovije bezbednosne i funkcionalne nadogradnje: bezbedna fiskalizacija, provereni NBS kursevi, zaštita ugrađenog REST API-ja sa Bearer tokenom, Serilog logovanje u fajl i PDF generisanje ponuda i predračuna.

## [1.1.1] - 2026-08-02

### 🚀 Usklađivanje verzije i automatsko ažuriranje (ERPiHub)
- **Konačna verzija 1.1.1**: Usklađena verzija modula sa `ERPiHub` detekcijom kako bi sve instalacije automatski prepoznale dostupno ažuriranje.
- Uključuje sve najnovije bezbednosne i funkcionalne nadogradnje: bezbedna fiskalizacija, provereni NBS kursevi, zaštita ugrađenog REST API-ja sa Bearer tokenom, Serilog logovanje u fajl i PDF generisanje ponuda i predračuna.

## [1.0.54] - 2026-08-01

### 🔒 Bezbednost i integritet podataka (kritično)
- **Fiskalizacija više ne prijavljuje lažni uspeh (`PfrApiClient`, `EsirFiskalizacijaService`)**:
  - Ranije je svaki neuspeh komunikacije sa PFR-om (nema mreže, odbijen zahtev, pogrešan PAC) tiho vraćao „uspešno fiskalizovan" sa izmišljenim brojem računa i lažnim `suf.purs.gov.rs` verifikacionim linkom, a račun je upisivan kao `Fiskalizovan`.
  - Sada se neuspeh prijavljuje kao greška i račun ostaje nefiskalizovan.
  - Simulator za testiranje i obuku zadržan, ali iza izričite opcije **Podešavanja → Fiskalizacija → „Dozvoli simulirane račune"** (podrazumevano isključeno). Simulirani računi dobijaju status `Simulacija`, broj sa prefiksom `SIMULACIJA-` i **nemaju** verifikacioni URL.
- **Kursna lista se više ne izmišlja (`NbsApiClient`, `KursnaListaService`)**:
  - Uklonjeni hardkodovani „rezervni" kursevi (EUR 117,1850, USD 108,2410…) koji su se pri nedostupnom NBS-u upisivali u bazu kao zvanični kurs za traženi datum i ulazili u devizno knjigovodstvo i uvozne kalkulacije.
  - Osvežavanje sa NBS-a više ne briše postojeću kursnu listu ako preuzimanje ne uspe.
  - `PretvoriDevizeURsdAsync` više ne vraća neponvertovan iznos kada kurs ne postoji (100 EUR se knjižilo kao 100 RSD) — prijavljuje jasnu grešku.
- **Verifikacija partnera u NBS registru (`NbsApiClient`)**: uklonjen fallback koji je za bilo koji PIB vraćao izmišljen račun `205-0000000012345-67` i status „AKTIVAN (Nije u blokadi)".
- **Ugrađeni REST API zaštićen (`AccountingWebServer`)**:
  - Uklonjen `Access-Control-Allow-Origin: *` — bilo koji sajt otvoren u pretraživaču mogao je da pročita promet firme i žiro-račune partnera dok aplikacija radi.
  - Uveden pristupni token koji se generiše pri svakom pokretanju servera; svi `/api/` pozivi zahtevaju `Authorization: Bearer`. Dashboard se otvara dugmetom koje token prosleđuje automatski.
  - Poruke o greškama više ne vraćaju detalje izuzetka (putanju baze) klijentu.
- **Uklonjeno automatsko resetovanje admin lozinke (`LoginWindow`)**: pri svakom otvaranju login prozora lozinka naloga `admin` se prepisivala hardkodovanim hešom iz izvornog koda. Sada se pri prijavi podrazumevanom lozinkom traži obavezna promena.

### 📋 Logovanje (`AppLog`, Serilog)
- **Uvedeno pravo logovanje u fajl.** Aplikacija do sada nije imala logger: dijagnostika je bila
  `Debug.WriteLine` (nevidljiv u Release verziji) i poruke u dijalozima koje korisnik zatvori i zaboravi.
  Kada bi se kod korisnika nešto pokvarilo, nije postojao nikakav trag.
- Zapisi idu u `%LOCALAPPDATA%\ERPiFinansijeApp\logs\log-GGGGMMDD.txt`, novi fajl svakog dana, čuva se
  poslednjih 14 dana. Zamenjuje raniji `crash.log` koji je rastao bez ograničenja.
- **Globalni hvatači proširen**: uz greške na korisničkom interfejsu i fatalne greške pozadinskih niti
  sada se hvataju i neposmatrane greške u pozadinskim zadacima, koje su ranije mogle tiho da obore proces.
- Sve `Debug.WriteLine` poruke u `catch` blokovima prevedene u `Serilog.Log.Error`, sa strukturiranim
  parametrima gde ima smisla (naziv fajla, putanja, datum).

### 🧪 Testovi
- `EsirFiskalizacijaTests` prepisan — proverava da fiskalizacija bez PFR-a pada i da simulirani račun nikad ne dobija status `Fiskalizovan`.
- Novi `AccountingWebServerTests` — 401 bez/sa pogrešnim tokenom, 200 sa ispravnim, zabrana wildcard CORS-a.

### 🛠️ Interno (bez uticaja na rad aplikacije)
- **CI kapija kvaliteta (`.github/workflows/release.yml`)**: workflow razdvojen na `test` i `build` job. Release se objavljuje tek kada build i testovi prođu; dodat `pull_request` triger tako da se PR testira ali ne objavljuje. Ranije se nijedan test nije pokretao pre objavljivanja.
- **`Directory.Build.props`**: upozorenja se u Release konfiguraciji tretiraju kao greške (`TreatWarningsAsErrors`); u Debug-u ostaju upozorenja. `NU1701` izuzet jer dolazi iz LiveCharts/SkiaSharp paketa.
- Očišćena sva preostala upozorenja prevodioca: `PdfReportService` (zastareli QuestPDF `Text(object)` preopterećeni poziv), `NalogEditWindow` (nullable anotacija).

---

## [1.0.53] - 2026-08-01

### 🚀 Nove funkcionalnosti i Poboljšanja
- **PDF Štampa Ponuda i Predračuna (`PdfReportService.GenerisiPonudaPredracunPdf`)**:
  - Brendirano generisanje PDF dokumenta za ponude i proforme sa detaljima partnera, stavkama, obračunom PDV-a, uslovima i ugrađenim potpisnim linijama.
  - Automatsko otvaranje generisanog PDF-a u sistemskom pregledaču u 1-klik (`TrgovinaView.xaml.cs`).
- **Klikabilni Link Web Servera u Podešavanjima (`PodesavanjaView.xaml.cs`)**:
  - Prikaz statusa ugrađenog Web Servera kao klikabilnog hiperlinka (`http://localhost:5050`) koji direktno otvara pretpregled u browseru.

---

## [1.1.1] - 2026-08-01

### 🎨 UI / UX
- Ikonica 📘 na login ekranu sada bela (`Foreground="White"`) — ranije se renderovala crno i gubila na tamnom header-u.

---

## [1.0.52] - 2026-08-01

### 🚀 Nove funkcionalnosti
- **Mesta troška i Projekti — Cost Centers (`MestaTroskaView` & `MestaTroskaService`)** — kompletan novi modul za analitičko praćenje po poslovnim jedinicama i projektima.
  - **Šifarnik Mesta Troška & Projekata**: Definisavanje mesta troška, gradilišta, projekata i objekata (Šifra, Naziv, Tip, Status).
  - **Povezivanje sa Stavkama Naloga (`StavkaNaloga.MestoTroskaId`)**: Dodeljivanje analitičkog mesta troška svakoj pojedinačnoj stavci pri knjiženju u Glavnoj Knjizi.
  - **Proračun Profitabilnosti po Projektu (`GetAnalitikaPoMestuTroskaAsync`)**: Automatski proračun ukupnih prihoda (Konto 6xx), rashoda (Konto 5xx) i neto finansijskog rezultata (Dobit/Gubitak po objektu/projektu).
  - **Nova Kartica u Navigaciji**: Dodato dugme "🎯 Mesta troška i Projekti" u bočnu navigaciju aplikacije.

### 📚 Dokumentacija & Pomoć
- Dodata ugrađena tema "🎯 Mesta troška i Projekti" u Pomoć (`PomocView`).
- Ažurirani `README.md` i `version.txt` (`1.0.52`).

---

## [1.0.51] - 2026-08-01

### 🚀 Nove funkcionalnosti
- **Blagajničko poslovanje — Dinarska i Devizna Blagajna (`BlagajnaView` & `BlagajnaService`)** — kompletan novi modul za blagajničko poslovanje.
  - **Nalozi za Uplatu i Isplatu**: Unos uplatnica i isplatnica za Dinarsku (**Konto 2430**) i Deviznu (**Konto 2440**) blagajnu.
  - **Hronološki Dnevnik Blagajne sa Saldom (`GetBlagajnickiDnevnikAsync`)**: Proračun prenetog stanja, ukupnih uplata/isplata i tekućeg salda blagajne za odabrani period.
  - **Automatsko Knjiženje u GK (`KnjiziBlagajnickiNalogAsync`)**: Kreiranje uravnoteženog naloga `BL` u Glavnoj Knjizi sa zaduženjem ili razduženjem konta 2430/2440 i odgovarajućeg protivkonta.
  - **Nova Kartica u Navigaciji**: Dodato dugme "💰 Dinarska i Devizna Blagajna" u bočnu navigaciju aplikacije.

### 📚 Dokumentacija & Pomoć
- Dodata ugrađena tema "💰 Blagajničko poslovanje" u Pomoć (`PomocView`).
- Ažurirani `README.md` i `version.txt` (`1.0.51`).

---

## [1.0.50] - 2026-08-01

### 🚀 Nove funkcionalnosti
- **Putni nalozi i Dnevnice (`PutniNaloziView` & `PutniNalogService`)** — kompletan novi modul za evidenciju službenih putovanja u zemlji i inostranstvu.
  - **Automatski Proračun Dnevnica (`IzracunajDnevnice`)**: Izračunavanje broja dnevnica na osnovu satnice puta (>12h = 1.0, 8h-12h = 0.5, <8h = 0.0) sa obračunom celih dana i preostalih sati.
  - **Prateći Troškovi Puta**: Unos računa za gorivo, smeštaj (hoteli), prevoz/taksi i putarine uz evidenciju isplaćenih akontacija.
  - **Automatsko Knjiženje u GK (`KnjiziPutniNalogAsync`)**: Automatsko knjiženje na **Konto 5330** (službena putovanja u zemlji) ili **Konto 5340** (službena putovanja u inostranstvu) i odobrenje Konta 4650.
  - **Nova Kartica u Navigaciji**: Dodato dugme "🚗 Putni nalozi i Dnevnice" u bočnu navigaciju aplikacije.

### 📚 Dokumentacija & Pomoć
- Dodata ugrađena tema "🚗 Putni nalozi i Dnevnice" u Pomoć (`PomocView`).
- Ažurirani `README.md` i `version.txt` (`1.0.50`).

---

## [1.0.49] - 2026-08-01

### 🚀 Nove funkcionalnosti
- **Kompenzacije, Asignacije i Cesije (`KompenzacijeView` & `KompenzacijaService`)** — kompletan novi modul za prebijanje obostranih dugovanja i potraživanja.
  - **Pametni Matching Engine (`GetObostranaDugovanjaAsync`)**: Automatska detekcija partnera koji su ujedno i kupci (Konto 2040) i dobavljači (Konto 4350) sa proračunom maksimalnog iznosa kompenzacije.
  - **Dvojne i Trojne Kompenzacije**: Kreiranje predloga poravnanja za 2 ili 3 ugovorne strane (Asignacija / Cesija).
  - **Automatsko Knjiženje i IOS Zatvaranje (`KnjiziIZatvoriKompenzacijuAsync`)**: Generisanje proknjiženog naloga `KOM` (Konto 4350 / Konto 2040) i automatsko zatvaranje faktura u sistemu otvorenih stavki.
  - **Nova Kartica u Navigaciji**: Dodato dugme "🤝 Kompenzacije i Cesije" u bočnu navigaciju aplikacije.

### 📚 Dokumentacija & Pomoć
- Dodata ugrađena tema "🤝 Kompenzacije, Asignacije i Cesije" u Pomoć (`PomocView`).
- Ažurirani `README.md` i `version.txt` (`1.0.49`).

---

## [1.0.48] - 2026-08-01

### 🚀 Nove funkcionalnosti
- **Komercijala — Ponude, Predračuni i Narudžbenice (`KomercijalaService`)** — kompletna podrška za komercijalno poslovanje u sklopu sekcije Trgovina & Komercijala.
  - **Ponude & Predračuni (Proforme)**: Unos i izdavanje ponuda kupcima u PDF-u sa obračunom rabata i PDV-a.
  - **1-Klik Konverzija u Fakturu & SEF (`PretvoriPonuduURacunAsync`)**: Dugme za automatsko pretvaranje prihvaćene ponude u konačni izlazni račun (`RacunOtpremnica`) spremnog za slanje na e-Fakture (SEF).
  - **Narudžbenice Dobavljačima (Purchase Orders)**: Praćenje ugovorene robe sa dobavljačima i rokova isporuke.
  - **1-Klik Konverzija u Ulaznu Kalkulaciju (`PretvoriNarudzbenicuUKalkulacijuAsync`)**: Dugme za automatski prenos naručenih artikala u novu ulaznu kalkulaciju sa detekcijom odstupanja pristiglih količina.
  - **Dva Nova Taba u `TrgovinaView`**: Dodati tabovi "📜 Ponude & Predračuni" i "🛒 Narudžbenice Dobavljačima".

### 📚 Dokumentacija & Pomoć
- Dodata ugrađena tema "📜 Komercijala — Ponude, Predračuni i Narudžbenice" u Pomoć (`PomocView`).
- Ažurirani `README.md` i `version.txt` (`1.0.48`).

---

## [1.0.47] - 2026-08-01

### 🚀 Nove funkcionalnosti
- **Napredno Višekriterijumsko Filtriranje Tabela (`NaprednaPretragaWindow`)** — ugrađen univerzalni sistem za kombinovanje više filtera simultano (raspon datuma Od-Do, min/max iznosi, brojevi dokumenata/naloga, partneri, konta i status knjiženja).
- **Masovni Izvoz u Excel sa Sačuvanim Rasporedom Kolona (`ExcelExportService`)** — omogućen izvoz u Excel uz striktno poštovanje redosleda kolona koje je korisnik izmenio na ekranu prevlačenjem (`DisplayIndex`).
- **Integracija u Glavnu Knjigu i Robno Knjigovodstvo** — dodata dugmad **"⚙️ Napredni filter"** u `NaloziView` i `TrgovinaView`.

### 📚 Dokumentacija & Pomoć
- Dodato uputstvo za napredno filtriranje i izvoz u Pomoć (`PomocView`).
- Ažurirani `README.md` i `version.txt` (`1.0.47`).

---

## [1.0.46] - 2026-08-01

### 🚀 Nove funkcionalnosti
- **AI / OCR Čitač Skeniranih Računa u DMS-u (`DmsOcrPreviewWindow`)** — ugrađena podrška za automatsko skeniranje, analizu i ekstrakciju podataka sa ulaznih računa (PDF i Slikovni prilozi u DMS-u).
  - **DmsOcrInvoiceParser**: Pametno izdvajanje PIB-a dobavljača, broja ulaznog računa, datuma izdavanja, valute dospelosti, osnovice (neto), PDV iznosa (20%/10%) i ukupnog iznosa za uplatu (bruto).
  - **DmsOcrMatchingService**: Automatsko uparivanje prepoznatog PIB-a sa šifarnikom `Partneri` u bazi i generisanje uravnoteženih stavki naloga knjiženja (Konta `5010`/`5390` nabavka/usluge, Konto `2700` ulazni PDV 20%, Konto `4350` obaveza prema dobavljaču sa `PartnerId`).
  - **DmsWindow Integracija**: Novo dugme **"🔍 OCR Nalog"** uz svaki dokument u listi DMS priloga sa pretpregledom i pokretanjem `NalogEditWindow` u 1 klik.

### 📚 Dokumentacija & Pomoć
- Dodata ugrađena tema "🔍 AI / OCR Čitač skeniranih računa u DMS-u" u Pomoć (`PomocView`).
- Ažurirani `README.md` i `version.txt` (`1.0.46`).

---

## [1.0.45] - 2026-08-01

### 🚀 Nove funkcionalnosti
- **Uvoz Elektronskih Bankarskih Izvoda & Automatski Matching Engine (`UvozIzvodaWindow`)** — ugrađena kompletna podrška za uvoz i automatsku obradu bankarskih izvoda.
  - **4 Podržana Formata**: Halcom E-Bank XML, Asseco / Office Banking XML, ISO 20022 `camt.053.001.02` XML i SWIFT MT940 TXT izvodi.
  - **BankIzvodMatchingEngine**: Automatska detekcija formata i 3 nivoa pametnog uparivanja (PIB/žiro račun partnera, poziv na broj/broj fakture, bankarske provizije Konto 5530).
  - **Automatsko Knjiženje u GK & IOS Zatvaranje**: Automatsko kreiranje proknjiženog naloga vrste `IZV` sa uravnoteženim stavkama za tekući račun (Konto 2410) i automatsko zatvaranje otvorenih potraživanja i dugovanja kupaca/dobavljača u sistemu otvorenih stavki.

### 📚 Dokumentacija & Pomoć
- Dodata ugrađena tema "🏦 Uvoz elektronskih bankarskih izvoda" u Pomoć (`PomocView`).
- Ažurirani `README.md` i `version.txt` (`1.0.45`).

---

## [1.0.44] - 2026-08-01

### 🚀 Nove funkcionalnosti
- **Devizno Knjigovodstvo & Viševalutno knjiženje** — ugrađena kompletna podrška za devizno poslovanje (EUR, USD, CHF, GBP).
  - Proširena `StavkaNaloga` sa deviznim poljima (`Valuta`, `KursValute`, `DevizniDuguje`, `DevizniPotrazuje`) i prikaz u `NalogEditWindow`.
  - Automatski proračun dinarske protivvrednosti po važećem NBS kursu.
- **Automatsko Valviranje Deviznih Konta i Kursne Razlike (`DeviznoValviranjeWindow`)**:
  - `DeviznoKnjigovodstvoService` vrši proračun i automatsko generisanje naloga knjiženja za valviranje deviznih konta na dan bilansa.
  - Knjiženje pozitivnih kursnih razlika na **Konto 6630** (Prihodi od kursnih razlika) i negativnih na **Konto 5630** (Rashodi od kursnih razlika).
- **Ino-Fakture i Uvozne Kalkulacije (`UvoznaKalkulacijaWindow`)**:
  - Model `UvoznaKalkulacija` i `UvoznaStavka` sa unosom ino-fakture u devizama, proračunom carine, špedicije i prevoza.
  - `UvoznaKalkulacijaService` vrši proporcionalnu raspodelu zavisnih uvoznih troškova na nabavne cene robe/materijala i knjiži ulaz u magacin (Konto 1300/1010) i obaveze (Konto 4350/4330).

### 📚 Dokumentacija & Pomoć
- Dodate teme "💱 Devizno knjiženje i Obračun kursnih razlika" i "🛃 Ino-fakture i Uvozne kalkulacije" u Pomoć (`PomocView`).
- Ažurirani `README.md` i `version.txt` (`1.0.44`).

---

## [1.0.43] - 2026-08-01

### 🚀 Nove funkcionalnosti
- **DMS (Document Management System)** — ugrađen sistem za skladištenje i upravljanje priloženim skeniranim dokumentima (ulazni računi, ugovori, zapisnici).
  - Model `DokumentPrilog` i `DmsService` za bezbedno kopiranje i povezivanje PDF/slikovnih fajlova sa nalozima knjiženja, fakturama i kalkulacijama.
  - Dijaloški prozor `DmsWindow` sa ugrađenim pretpregledom, otvaranjem u sistemskom pregledaču i mogućnošću brisanja.
  - Dodato dugme **"📎 Prilozi (DMS)"** u zaglavlju naloga knjiženja (`NalogEditWindow`).
- **Cloud REST API & Mobile Web Dashboard** — ugrađen lagani HTTP web server za mobilni i web uvid u poslovanje.
  - `AccountingWebServer` servira REST API endpoints (`/api/status`, `/api/dashboard`, `/api/partneri`) i ugrađenu responzivnu Tailwind/HTML5 Web Dashboard aplikaciju na portu `5050` (`http://localhost:5050`).
  - Kontrolni tab u podešavanjima (`PodesavanjaView`) za pokretanje/zaustavljanje servera i brzi pristup iz pregledača sa pametnog telefona ili računara.

### 📚 Dokumentacija & Pomoć
- Dodate teme "📎 DMS — Prilozi uz naloge i skenirani dokumenti" i "🌐 REST API & Web Dashboard (Mobilni uvid)" u Pomoć (`PomocView`).
- Ažurirani `README.md` i `version.txt` (`1.0.43`).

---

## [1.0.42] - 2026-08-01

### 🚀 Nove funkcionalnosti
- **Prateći APR Finansijski Izveštaji & Poreski Bilans (PB-1, PDP, OA)** — ugrađen kompletni set finansijskih i poreskih izveštaja za predaju APR-u i Poreskoj upravi RS.
  - **Statistički izveštaj (SI)**: Automatska priprema statističkih AOP pozicija 9001-9010 (zarade, troškovi robe, usluga, nabavke osnovnih sredstava i porezi).
  - **Izveštaj o tokovima gotovine (Cash Flow Statement)**: Obračun priliva i odliva gotovine po AOP pozicijama 3001-3040 iz poslovnih, investicionih i finansijskih aktivnosti.
  - **Izveštaj o promenama na kapitalu**: Matrica promena osnovnog kapitala, rezervi, neraspoređene dobiti i gubitka u toku godine (AOP 4001-4010).
  - **Poreski Bilans Obrazac PB-1 (`PoreskiBilansWindow`)**: Obračun oporezive dobiti sa usklađivanjem nepriznatih rashoda (reprezentacija iznad 0.5%, zatezne kamate, kazne i penali, donacije iznad 5%) i stope poreza na dobit od 15%.
  - **Poreska Amortizacija Obrazac OA (`GenerisiPoreskuAmortizacijuOaAsync`)**: Obračun amortizacije po I-V grupama osnovnih sredstava (2.5% do 30%) i usklađivanje sa računovodstvenom amortizacijom u PB-1.
  - **Obrazac PDP (Poreska Prijava)**: Priprema poreske prijave poreza na dobit sa obračunatim porezom i mesečnim akontacijama za naredni period.

### 📚 Dokumentacija & Pomoć
- Dodata ugrađena tema "🏛️ Bilansi (APR) i Poreski Bilans (PB-1 / PDP / OA)" u Pomoć (`PomocView`).
- Ažurirani `README.md` i `ANALIZA_I_PLAN.md`.

---

## [1.0.41] - 2026-08-01

### 🚀 Nove funkcionalnosti
- **e-Fiskalizacija (ESIR / PFR Integracija RS)** — ugrađena kompletna podrška za komunikaciju sa PFR / LPFR servisom (Procesor Fiskalnih Računa) Poreske uprave RS (`http://localhost:8443`).
  - **PFR Klijent & Servis (`PfrApiClient`, `EsirFiskalizacijaService`)**: Slanje zahteva za fiskalizaciju maloprodajnih računa u PFR po propisanoj JSON strukturi sa poreskim oznakama (Đ 20%, E 10%, А 0%) i metodama plaćanja (Gotovina, Platna kartica, Prenos na račun).
  - **Fiskalni Račun i Isečak (`FiskalniRacunWindow`)**: Izdavanje fiskalnog računa kupcu, prikaz PFR broja računa (`InvoiceNumber`), PFR brojača, fiskalnog žurnala i zvaničnog verifikacionog URL QR koda Poreske uprave RS (`suf.purs.gov.rs`).
  - **PFR Podešavanja (`PodesavanjaView`)**: Novi tab "🧾 e-Fiskalizacija (PFR / ESIR)" sa unosom PFR URL-a, PAC koda Bezbednosnog Elementa (BE), naziva kasira i dugmetom za testiranje PFR konekcije.
  - **Evidencija i status u Trgovini (`TrgovinaView`)**: Dodato novo dugme "🧾 Fiskalizuj (PFR)", nova kolona `Fiskalni Broj` u tabeli računa i evidencijska tabela `FiskalniRacuniLog`.
  - **EF Core Migracija**: Dodana migracija `DodajEsirFiskalizaciju` sa PFR poljima u tabelama `Firme`, `RacuniOtpremnice` i `FiskalniRacuniLog`.

### 📚 Dokumentacija & Pomoć
- Dodata nova tema "🧾 e-Fiskalizacija (ESIR / PFR) i Izdavanje Računa" u ugrađenu Pomoć (`PomocView`).
- Ažurirani `README.md` i `ANALIZA_I_PLAN.md`.

---

## [1.0.40] - 2026-08-01

### 🚀 Nove funkcionalnosti
- **NBS Konekcija (Kursna lista i Registar tekućih računa)** — ugrađena integracija sa web servisima Narodne banke Srbije (`nbs.rs`).
  - **Dnevna Kursna Lista NBS (`KursnaListaWindow`)**: Preuzimanje zvanične dnevne srednje, kupovne i prodajne kursne liste NBS za sve valute (EUR, USD, CHF, GBP, BAM, RUB, JPY, itd.) uz automatsko skladištenje u lokalnoj SQLite bazi.
  - **Kalkulator Konverzije Valuta**: Brzi preračun proizvoljnih deviznih iznosa u dinarsku protivvrednost (RSD) po zvaničnom srednjem kursu NBS za izabrani datum.
  - **Verifikacija partnera u Registru NBS (`ProveriTekuciRacunPartneraAsync`)**: Provera žiro-računa i statusa blokada poslovnih partnera u Jedinstvenom registru računa NBS na dugme "🔍 Verifikuj račun (NBS)".
  - **EF Core Migracija**: Dodana migracija `DodajNbsKursnuListu` sa novom tabelom `KursneListeStavke`.

### 📚 Dokumentacija & Pomoć
- Dodata nova tema "💱 Kursna lista NBS i Registar partnera" u ugrađenu Pomoć (`PomocView`).
- Ažurirani `README.md` i `ANALIZA_I_PLAN.md`.

---

## [1.0.39] - 2026-08-01

### 🚀 Nove funkcionalnosti
- **ePorezi / Obrazac PP-PDV (XML Izvoz)** — ugrađen automatski izvoz zvanične XML poreske prijave PP-PDV za portal ePorezi Poreske uprave RS (`eporezi.purs.gov.rs`).
  - **PP-PDV XML Generator (`PpPdvXmlGenerator`)**: Generisanje XML strukture po zvaničnoj šemi Poreske uprave sa sekcijama `<PodaciOPrijavi>`, `<ObracunatiPdv>` (Polja 001-008, 101-108), `<PrethodniPdv>` (Polja 009-010, 109-110) i `<KonacniObracun>` (Polja 111-113).
  - **Pametno opredeljenje za povraćaj (Polje 113)**: Ako u izabranom periodu postoji preplata PDV-a, korisnik pri izvozu bira da li iznos traži za povraćaj na tekući račun (Polje 113 = 1) ili ga vodi kao poreski kredit za naredni period (Polje 113 = 0).
  - **Izvoz u PDV Evidenciji (`PdvEvidencijaView`)**: Dodato novo dugme "📄 Izvezi XML za ePorezi (PP-PDV)" za brzi izvoz sa izabranim opsegom datuma.

### 📚 Dokumentacija & Pomoć
- Dodata nova uputstva u ugrađenu Pomoć (`PomocView`) pod temom "🧾 PDV Evidencija (KPR, KIR i PP-PDV XML)".
- Ažurirani `README.md` i `ANALIZA_I_PLAN.md`.

---

## [1.0.38] - 2026-08-01

### 🚀 Nove funkcionalnosti
- **SEF API Integracija (e-Fakture RS)** — ugrađena direktna konekcija sa državnim Sistemom Elektronskih Faktura (SEF) Ministarstva finansija Republike Srbije.
  - **UBL 2.1 XML Generator (`SefUblGenerator`)**: Generisanje zvaničnih XML e-Faktura po srpskom profilu e-Fakture (`urn:cen.eu:en16931:2017#compliant#urn:mfin.gov.rs:srbdt:2021`).
  - **REST API Klijent & Biznis Sloj (`SefApiClient`, `SefService`)**: Slanje e-faktura (`POST /sales-invoice/ubl`), provera statusa u realnom vremenu i preuzimanje ulaznih e-faktura dobavljača.
  - **SEF Podešavanja u Podešavanjima (`PodesavanjaView`)**: Nov tab "⚡ SEF e-Fakture" za unos API ključa, izbor Demo/Produkcionog okruženja, JBKJS broja i dugme "⚡ Testiraj SEF Konekciju". SEF podešavanja su izolovana po firmi.
  - **Upravljanje fakturama i statusi (`TrgovinaView`)**: Tabela računa obogaćena kolonom "SEF Status" i dugmadima `📤 Pošalji na SEF`, `🔄 SEF Status`, `📄 UBL XML` i `📥 Ulazne SEF`.
  - **Pregled ulaznih e-Faktura (`SefUlazneFaktureWindow`)**: Namenski prozor za preuzimanje i uvid u ulazne e-fakture dobavljača sa SEF-a.
  - **EF Core Migracija**: Dodana migracija `DodajSefPolja` i nov enum `SefStatusFakture`.

### 📚 Dokumentacija & Pomoć
- Dodata nova tema "⚡ SEF e-Fakture" u ugrađenu Pomoć (`PomocView`).
- Ažurirani `README.md` i `ANALIZA_I_PLAN.md` sa dokumentovanim SEF API modulom.

---

## [1.0.36] - 2026-08-01

### 🚀 Nove funkcionalnosti
- **Predračun (Trgovina — Računi-Otpremnice)** — isti ekran za unos računa-otpremnice sada ima čekboks "📝 Predračun" koji otkriva polje "Rok važenja predračuna". Predračun se ne može proknjižiti dok se prvo ne pretvori u pravi račun (novo dugme "🔁 Pretvori u račun" — zadržava sve stavke, samo postavlja tekući datum i menja tip dokumenta). Lista računa ima novu kolonu "Tip" i filter "Predračuni". PDF štampa predračuna ispisuje "PREDRAČUN" zaglavlje, rok važenja umesto roka plaćanja, i napomenu da ne predstavlja obavezu plaćanja. Uvoz starih DOS podataka (RAC_OTP.DBF) ostaje nepromenjen — svi uvezeni računi ostaju tipa "Račun", pošto stari DOS program predračun nikad nije ni implementirao (pozivane `izmena_predrac()`/`stampa_predrac()` procedure nisu postojale nigde u legacy kodu).
- **Trag rasknjižavanja naloga (`NalogAudit`) i zaštita od razilaženja sa prenetim početnim stanjem** — rasknjižavanje naloga u Glavnoj knjizi sada upisuje ko i kada je rasknjižio (korisnik, vreme, broj naloga) radi revizije, i odbija rasknjižavanje naloga iz godine za koju je već napravljen prenos početnog stanja u narednu godinu (jer bi to tiho razišlo preneto stanje od stvarnog prometa).

### 🐛 Ispravke i Validacije
- **Gubljenje podataka na Računu-Otpremnici posle ponovnog učitavanja** — polja `BrojOtpremnice`, `KontoKupca`, `RokPlacanjaDana` i `NacinPlacanja` su bila označena `[NotMapped]` (bez veze sa kolonom u bazi), pa su se posle svakog snimanja i ponovnog otvaranja tiho vraćala na podrazumevane vrednosti (npr. rok plaćanja uvek nazad na 15 dana, broj otpremnice i način plaćanja se brišu, kupac ostaje prazan ako nije prepoznat kao postojeći partner). Sada su stvarno mapirana u bazu (migracija `MapirajPoljaRacunOtpremnice`); dodat test koji snima račun i učitava ga preko potpuno novog konteksta baze da dokaže da vrednosti opstaju.

### 📚 Dokumentacija
- Ažuriran README.md sa opisom Predračuna i revizionog traga rasknjižavanja naloga. Ažuriran `run-accounting-app` skill (UI-test alat) sa novim saznanjima o tajmingu prijave, `ComboBox` fokusiranju i pretpodešenim dozvolama za bržu vožnju aplikacije.

---

## [1.0.35] - 2026-07-31

### 🚀 Nove funkcionalnosti
- **Radna tabla za Materijalno knjigovodstvo (`MaterijalnoDashboardView`)** — nova stavka u meniju (sekcija MATERIJALNO KNJIGOVODSTVO), po uzoru na postojeću Robnu radnu tablu: vrednost zaliha materijala, broj materijala na zalihi, upozorenje na negativna stanja, poslednji ulazi/trebovanja i brze akcije (Novi ulaz, Novo trebovanje, Nova primopredaja).
- **Filter Svi / Proknjiženi / Neproknjiženi u Robnom knjigovodstvu** — dodat na sve tabove gde se knjiži: Zaduženja, Razduženja, Primopredaje, Kalkulacije (veleprodaja i maloprodaja), Računi-Otpremnice i Nivelacije cena. Isti filter je već postojao u Materijalnom (Ulazi/Trebovanja/Primopredaje).

### 📚 Dokumentacija
- Ažurirani README.md i ugrađena Pomoć (`PomocView`) sa opisom rasknjižavanja i filtera u Robnom/Materijalnom knjigovodstvu i novih radnih tabli.

---

## [1.0.34] - 2026-07-31

### 🚀 Nove funkcionalnosti
- **Rasknjižavanje dokumenata u Robnom i Materijalnom knjigovodstvu** — po uzoru na Glavnu knjigu: klik na 'Izmeni' nad proknjiženim dokumentom sada nudi pitanje "Da li želite da rasknjižite radi izmene?" umesto samo blokirajuće poruke, na svim tabovima gde se knjiži:
  - Robno: Zaduženja, Razduženja, Primopredaje, Kalkulacije, Računi-Otpremnice, Nivelacije cena.
  - Materijalno: Ulazi, Trebovanja, Primopredaje.
  - Dostupno isključivo administratorima. Rasknjižavanje bezbedno uklanja samo redove materijalne kartice koje je dati dokument upisao (LIFO provera protiv kasnijih knjiženja za isti artikal/magacin — u suprotnom se odbija radi zaštite tačnosti prosečne cene zaliha) i vraća/briše povezane naloge Glavne knjige i cene artikala gde je primenljivo (Računi-Otpremnice, Nivelacije).
- **Ekranski pregled izveštaja** — novi preview prozori za Dnevnik glavne knjige, Bruto bilans analitike i Vrednovanje zaliha (`IzvestajiView`).

### 🐛 Ispravke i Validacije
- **Uvoz KALKULAC.DBF (veleprodajne kalkulacije)** — ispravljeno mapiranje kolone magacina: legacy fajl koristi `MAG_PRIMA`, a ne `MAGACIN`/`MAG` kako je uvoznik ranije tražio, zbog čega je magacin ostajao prazan na **svim** uvezenim kalkulacijama (onemogućavalo rasknjižavanje uz grešku "nema magacin"). Dodatno mapirane i ranije nemapirane kolone `OTPREM_BR`/`OTPREM_DAT`/`RACUN_BR`/`RACUN_DAT`/`TRANS_TROS` i raspodela troškova (`TROS_USKL`/`UTOV_ISTOV`/`TR_OSIGUR`/`OSTALI`).

---

## [1.0.33] - 2026-07-31

### 🎨 UI / UX
- Dodate ikonice za štampu/PDF na dugmadima u više modula (Bilansi, Izveštaji, Konta, Magacin, Nalozi, Partneri, PDV Evidencija).
- Dodata kolona Naziv artikla i J.M. u tabelama Trgovine.

---

## [1.0.32] - 2026-07-31

### 🚀 Nove funkcionalnosti
- **Modul Trgovina (Robno Knjigovodstvo)**:
  - Omogućeno **masovno knjiženje** za dokumente po specifičnim tipovima: Zaduženja, Razduženja i Primopredaja.
  - Implementirana **štampa** po navedenim tipovima naloga.
  - Dodat **Export u Excel** za tabele Zaduženja, Razduženja i Primopredaje.
- **Kalkulacije**:
  - Implementirana **izmena (editovanje)** postojećih (neproknjiženih) kalkulacija preko prozora `KalkulacijaEditWindow` uz očuvanje svih referenci na stavke u bazi.

### 🎨 UI / UX i Odzivnost
- Primenjen širi paket vizuelnih poboljšanja: povećani fontovi i poboljšana poravnanja u `DataGrid` tabelama radi bolje čitljivosti preko glavnih modula (Bilansi, Firme, Izveštaji, Kartice, Konta, Magacin, Nalozi, Partneri, PDV Evidencija, Trgovina).
- Podešavanja na početnom `Dashboard` prikazu.

### 🐛 Ispravke i Validacije
- **Kartice**: Mapirano polje `NalogId` u `KarticaRed` radi ispravnog prikaza porekla promene kod robnih/materijalnih kartica.
- Manji refaktoring koda u `PrimopredajaEditWindow` i proveri proknjiženosti kod izmene naloga.

---
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

### 🚀 ERPiHub Integracija & Pokretanje sa Konkretnom Bazom (CLI Integration)
- **Ugrađena podrška za `--db-path` CLI argument**: Omogućeno direktno pokretanje `ERPiFinansijeApp.exe` iz ERPiHub centralnog kontrolnog panela sa automatskim prosleđivanjem putanje do izabrane SQLite baze/firme (`--db-path "<path>"`).

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
- **Šifarnik poreskih tarifa (`PoreskaTarifaEditWindow`)**: Novi CRUD ekran (tarifni broj, porez %, poseban porez %, porez u ceni) sa PDF štampom, analogan legacy `TARIFE.DBF`. Uvoz podržan i kroz `⚙️ Podešavanja` i kroz samostalni `ERPiFinansijeMigration` alat.

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

### 🏢 Firme prerađene po uzoru na ERPiSredstvaApp (1 baza = 1 firma)
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

### 🗂️ Baze podataka premeštene u zaseban Baze folder (po uzoru na ERPiSredstvaApp)
- **Uvoz firmi više ne piše bazu u DOS folder firme** (`C:\KNJIGE\Radni\KORxx\`) — taj folder je izvor za reimport koji samostalni `ERPiFinansijeMigration` alat po potrebi briše i pravi iznova, pa je živa baza tamo bila izložena riziku od tihog gubitka podataka. Baze sada žive u `%LocalAppData%\ERPiFinansijeApp\Baze\`, imenovane `firma_{Šifra}_{Naziv}.db`.
- **Jednokratna migracija postojeće baze**: pri prvom pokretanju posle nadogradnje, ako živa baza i dalje sedi na staroj DOS lokaciji, automatski se premešta u Baze folder (analogno `ERPiSredstvaApp.AppConfig.PrilagodiNazivZajednickeBaze`) — bez gubitka podataka, testirano na KOR01 (3.207 konta, 338 naloga pre i posle migracije identično).

---

## [1.0.9] - 2026-07-25

### 🐛 Ispravka mapiranja DBF kolona pri uvozu (KONTPLAN, ANKONT, MAGACIN, ARTIKLI, NALOG)
- **Objedinjen uvoz u `DbfImportService`**: I uvoz iz aplikacije (`⚙️ Podešavanja`) i samostalni `ERPiFinansijeMigration` alat sada koriste isti deljeni mapping kod, tako da se imena DBF kolona ne mogu razminuti između dva mesta.
- **Ispravljeno pogrešno mapiranje imena kolona**: Uvoz naloga (`NALOG.DBF`) i partnera (`ANKONT.DBF`) je tražio kolone koje ne postoje u ovim DBF fajlovima i tiho uvozio 0 redova; uvoz kontnog plana je čitao pogrešnu kolonu za broj konta (uvezeno je bilo samo ~42 sintetička konta umesto svih ~3200).
- **Dodate kolone koje su ranije bile odbačene**: Kontni plan sada čuva staru šifru konta, ulicu, mesto, žiro račun i telefon (iz KONTPLAN.DBF); artikli čuvaju klasifikacionu šifru i selektovan flag; stavke naloga čuvaju staru šifru konta i šifru promene.
- **Ispravljena UNIQUE constraint greška** u `ERPiFinansijeMigration` alatu pri uvozu kontnog plana (KONTPLAN.DBF sadrži par dupliranih šifara konta) — dodata ista in-memory `HashSet` provera koja se već koristila u uvozu iz aplikacije.

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
- **Izolacija baza po firmama (kao u ERPiSredstvaApp)**: Pri uvozu DOS podataka za svaku izabranu firmu kreira se ili osvežava **njena zasebna SQLite baza** u njenom folderu (npr. `C:\KNJIGE\Radni\KOR01\accounting_kor01.db`, `C:\KNJIGE\Radni\KOR06\accounting_kor06.db`).
- **Nezavisnost naloga i šifarnika**: Svi nalozi za knjiženje, konta, partneri, magacini i artikli se uvoze i skladište isključivo u posebnu bazu konkretne firme, bez mogućnosti ukrštanja ili preslikavanja podataka među firmama.

---

## [1.0.6] - 2026-07-25

### 🐛 Ispravke uvoza DOS/DBF podataka
- **Sprečavanje dupliranja šifara i UNIQUE constraint grešaka**: Ugrađena brza in-memory `HashSet` provera za sprečavanje dupliranja konta, partnera, magacina, artikala i naloga tokom uvoza više firmi.
- **Detaljniji prikaz izuzetaka**: Omogućen detaljan prikaz `InnerException` poruka u dijaloškom prozoru i dnevniku uvoza.

---

## [1.0.5] - 2026-07-25

### 🐛 Ispravke i vraćanje ikone
- **Vraćanje plave aktovke (`app.ico`)**: Vraćena originalna plava ikona sa slikom aktovke specifična za **ERPiFinansije**.
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
- **Automatsko ažuriranje (Velopack)**: Integrisana pozadinska provera i dijaloški prozor `UpdateDialog` za preuzimanje i instalaciju novih verzija pri pokretanju `ERPiFinansijeApp` aplikacije.
- **Upravljanje firmama (`FirmeView`)**: Implementiran kompletan modul za pregled, filtriranje, unos i izmenu matičnih podataka firmi, te trenutni izbor aktivne firme.

---

## [1.0.1] - 2026-07-24

### 🔧 Poboljšanja i ispravke
- **Konfiguracija ikone aplikacije**: Povezana ikona `app.ico` u svim slojevima aplikacije (`.csproj`, WPF prozori, Velopack instalacioni paket).
- **Podešavanje okruženja za prevođenje i publikovanje**: Usaglašeni VS Code zadaci i `launch.json` za brzo pokretanje i debagovanje preko `F5`.
- **Integracija AI veština**: Iskopirane i prilagođene sve AI veštine iz `ERPiSredstva` radnog okruženja.

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

- **Uvoz iz DOS sistema** (`ERPiFinansijeMigration`) — automatski uvoz kontnog plana, naloga, partnera, materijala, magacina, ulaza, trebovanja, kartica i kamatnih stopa iz legacy dBase III / Clipper fajlova.

- **Lokalizacija** — kompletan korisnički interfejs preveden na srpski jezik.

- **Pomoć ugrađena u aplikaciju** — uputstvo za korišćenje po modulima, dostupno direktno iz sidebar-a (bez potrebe za eksternom dokumentacijom).

### 🏗️ Arhitektura

- Analitika partnera (otvorene stavke) vezana je preko `StavkaNaloga.PartnerId`, umesto paralelne ANAL strukture iz legacy DOS sistema — glavna knjiga i analitika su objedinjene u istim tabelama.
- Materijalne kartice koriste jedinstven servis nezavisan od vrste artikla (roba/materijal), pa je spreman za dalje širenje i na robni promet.

### 📚 Dokumentacija

- `README.md` — pregled funkcionalnosti, tehnologija i strukture projekta.
- `ANALIZA_I_PLAN.md` — analiza legacy Clipper sistema (moduli FIN/ANAL/ROB/MAT) i detaljan istorijat faznog razvoja sa obrazloženjima odluka.
- `run-accounting-app` skill — UI-automation vodič za pokretanje i testiranje aplikacije.
