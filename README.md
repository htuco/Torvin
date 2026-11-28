# Torvin

Torvin je platforma za upravljanje digitalnim novčanikom: virtualne kartice, interni transferi
između korisnika, zahtjevi za novac, trajni nalozi, štednja, budžet po kategorijama i AI asistent
za korisničku podršku. Platforma ne radi stvarno plaćanje. Balans postoji samo unutar sistema.

Seminarski rad iz predmeta Razvoj softvera II, FIT.
Autor: Hamza Tuco, IB230181.

- **Mobilna aplikacija** (Flutter) je za korisnike.
- **Desktop aplikacija** (Flutter, Windows) je za administratore.
- **Backend** se sastoji od REST API-ja i zasebnog Worker servisa povezanih preko RabbitMQ-a,
  nad SQL Server bazom.

## Pokretanje

### Backend (Docker)

Treba Docker Desktop (na Macu s Apple Siliconom uključena Rosetta, jer SQL Server image postoji
samo za amd64). Iz roota repoa:

```bash
cp .env.example .env
docker compose up -d --build
docker compose ps
```

Podiže se pet servisa: `sqlserver`, `rabbitmq`, `mailpit`, `api` i `worker`. Prvi start traje
nekoliko minuta jer se povlače imagei. API čeka da baza i RabbitMQ budu spremni.

| Šta | Adresa |
|---|---|
| API (Swagger) | `http://localhost:5080/swagger` |
| RabbitMQ UI | `http://localhost:15672` (korisnik i lozinka iz `.env`) |
| Mailpit (pregled poslanih mailova) | `http://localhost:8025` |
| SQL Server | `localhost,1401`, korisnik `sa`, lozinka iz `.env` |

Zaustavljanje: `docker compose down`. Baza ostaje sačuvana u volumeu `sqlserver-data`, a briše je
tek `docker compose down -v`.

API se na bazu spaja kao `sa`, jer SQL Server kontejner ima samo tog korisnika, pa je lozinka zapisana samo
jednom (`SQL_SA_PASSWORD`). Kontejner ima samopotpisan certifikat, pa veza koristi `TrustServerCertificate`.

### Backend (lokalno, bez Dockera)

Treba .NET SDK 10 i fajl `.env` u rootu repoa (`cp .env.example .env`). API i Worker ga u Development okruženju
učitaju sami. Baza, RabbitMQ i Mailpit mogu ostati u Dockeru (`docker compose up -d sqlserver rabbitmq mailpit`),
jer su vrijednosti u `.env` napisane za `localhost`.

```bash
cd torvin.backend
dotnet tool restore                    # dotnet-ef iz dotnet-tools.json, za migracije
dotnet run --project Torvin.API
dotnet run --project Torvin.Worker
dotnet test Torvin.slnx                # traži upaljen Docker (SQL Server testovi)
dotnet test Torvin.slnx --filter "Category!=SqlServer"   # brzo, bez Dockera
```

Većina testova radi nad bazom u memoriji. Testovi prenosa novca (`Category=SqlServer`) sami podignu pravi SQL Server
u Dockeru (Testcontainers, isti image kao u `docker-compose.yml`), jer baza u memoriji nema transakcije ni provjeru
istovremenih izmjena. Bez upaljenog Dockera ti testovi padaju.

API pri startu sam primijeni migracije i kreira bazu `230181` ako ne postoji, pa baza mora biti
podignuta prije API-ja. Ako varijabla fali, aplikacija ne starta i ispiše koje varijable nedostaju. Swagger je na
`http://localhost:5080/swagger`. Port se zadaje varijablom `API_PORT` u `.env` (preporučeno 5080, jer port 5000 na macOS-u
često zauzima AirPlay prijemnik).

### Mobilna aplikacija

```bash
cd torvin.frontend/torvin_mobile
flutter pub get
flutter run --dart-define=API_BASE_URL=http://localhost:5080
```

Za Android emulator je adresa `http://10.0.2.2:5080`. Release APK smije zvati API preko `http`
(`usesCleartextTraffic` u `AndroidManifest.xml`), jer lokalni API nema TLS. Bez `API_BASE_URL` aplikacija pri startu
javlja grešku.

Mobilna ima prijavu, registraciju s potvrdom emaila (kod stiže u Mailpit, `http://localhost:8025`), zaboravljenu
lozinku i reset. Novi kod za potvrdu stiže pri pokušaju prijave nepotvrđenim računom. Prijava radi samo za korisnike,
a administrator dobija poruku da koristi desktop aplikaciju.

Kartice se otvaraju s Početne ("Moje kartice"): lista s filterom po statusu, detalji s historijom kartice (filter
poslano/primljeno), dodavanje (tip, valuta, broj, mjesec i godina isteka, pa kod iz emaila), blokiranje uz potvrdu,
deblokiranje, prijava gubitka ili krađe i QR kod za primanje. Akcija koja za status kartice nije moguća je
onemogućena, a ispod piše zašto. Za probu dodavanja radi testni broj `4111111111111111`. Polje forme javlja grešku
tek kad ga korisnik promijeni, a sva polja se provjere pri slanju.

Novac se šalje s Početne ("Pošalji novac"): kartica (samo aktivne, uz dostupan balans), iznos (`1.245,00`: tačka za
hiljade, zarez za decimale, uz brze sume 20/50/100/200), primalac i napomena. Primalac se bira iz reda omiljenih,
pretragom (od 3 znaka) ili skeniranjem QR koda, koji popuni primaoca i iznos iz koda. Prije slanja dijalog pokazuje
sažetak, a poslije slanja ekran potvrde pokazuje iznos, primaoca i broj reference. Bez aktivne kartice forma se ne
otvara. QR za primanje (detalji kartice) može nositi iznos. Skeniranje traži dozvolu kamere, a bez nje ekran
objašnjava kako je dati.

Historija ("Transakcije" na Početnoj) ima pretragu (ime, napomena, referenca), smjer, period, karticu i iznos od–do,
grupisana je po danu i učitava sljedeću stranicu pri skrolanju. Dodir na transakciju (i u historiji kartice) otvara
detalje: datum, tip, smjer, kartice (zadnje 4 cifre), napomenu, kategoriju i referencu, uz "Dodaj u omiljene".
"Omiljeni primaoci" imaju pretragu, izmjenu nadimka, uklanjanje uz potvrdu i slanje novca jednim dodirom. Materialovi
tekstovi (izbor datuma, Nazad) su na bosanskom. Poznato ograničenje: filter perioda server računa po UTC danima, pa
transakcija iz prvog sata poslije ponoći po našem vremenu pada u prethodni dan.

### Desktop aplikacija

```bash
cd torvin.frontend/torvin_desktop
flutter pub get
flutter run -d macos --dart-define=API_BASE_URL=http://localhost:5080
```

Za predaju se pravi Windows build, a na Macu se razvija na macOS-u. Desktop prijava radi samo za
administratore. Token se čuva u sigurnom skladištu sistema (Windows Credential Manager, macOS Keychain), a kad istekne ili
bude opozvan, aplikacija se vrati na prijavu.

### Windows i Android build

Release build se pravi u GitHub Actions, jer Windows build traži Windows mašinu. Workflow `build-release` se pokreće
ručno (Actions → build-release → Run workflow) i daje dva artefakta: `windows-release` (folder `Release/` s
`torvin_desktop.exe`, API na `http://localhost:5080`) i `android-apk` (`app-release.apk`, API na `http://10.0.2.2:5080`).
Svaki pull request prolazi workflow `ci`: build backenda bez upozorenja, svi testovi backenda (i oni nad SQL Serverom,
jer runner ima Docker), te `flutter analyze` i `flutter test` za sva tri Flutter projekta.

## Pristupni podaci

Testni korisnici nastaju seedom baze pri prvom startu API-ja:

| Aplikacija | Korisničko ime | Lozinka |
|---|---|---|
| Desktop | `desktop` | `test` |
| Mobilna | `mobile` | `test` |

Seed upisuje i još šest korisnika (lozinka `test`, jedan blokiran), kartice u svim statusima, oko 95
transakcija kroz zadnjih 100 dana (i nekoliko današnjih), zahtjeve za novac i poništenja u svim stanjima, trajne naloge, štednju,
budžete, notifikacije i obavijesti sa slikama. Datumi su računati od trenutka seedanja, pa "ovaj mjesec"
uvijek ima podatke. Seed se pokreće samo kad u bazi nema korisnika, a ispočetka ga vraća
`docker compose down -v && docker compose up -d --build`.

## Prijava i sigurnost

Prijava ide preko `POST /auth/login` s korisničkim imenom ili emailom, lozinkom i aplikacijom (`Desktop` ili
`Mobile`). Desktop prima samo administratore, a mobilna samo korisnike. Odgovor sadrži JWT koji važi
`JWT_EXPIRES_MINUTES` minuta. U Swaggeru se token zalijepi jednom preko dugmeta **Authorize**.

- Svaki controller je zaštićen (`[Authorize]`). Anonimne su samo akcije auth toka, jer bez njih korisnik ne
  može doći do prijave:

  | Endpoint | Zašto je anonimno |
  |---|---|
  | `POST /auth/login` | korisnik još nema token |
  | `POST /auth/register` | korisnik još nema račun |
  | `POST /auth/verify-email` | dio registracije: bez potvrđenog emaila prijava nije moguća |
  | `POST /auth/forgot-password` | korisnik bez lozinke ne može dobiti token |
  | `POST /auth/reset-password` | isto, a promjenu odobrava kod iz maila |

- `POST /auth/logout` opoziva token na serveru, pa isti token odmah dobija 401.
- Registracija ne prima rolu: svaki novi račun je `User`, s nepotvrđenim emailom. Na email stiže šestocifreni
  kod koji važi 15 minuta. Prijava prije potvrde vraća 403 i šalje novi kod (najviše jednom u minuti).
- Zaboravljena lozinka uvijek vraća isti odgovor, i za email koji ne postoji. Kod za reset važi 30 minuta, a
  nova lozinka ne smije biti jedna od zadnje tri. Uspješan reset otključava račun.
- Kodovi su jednokratni, imaju najviše 5 pokušaja i u bazi su samo hashirani (PBKDF2, kao lozinke).
- Lozinka ima 8–100 znakova, veliko i malo slovo, broj i specijalni znak, bez korisničkog imena i emaila.
- Mailove šalje Worker: API objavi poruku u red `email`, a Worker je pošalje preko SMTP-a. Neuspjelo slanje
  se ponavlja poslije 1, 2, 4 i 8 sekundi, a onda poruka ide u red `email.dlq` (vidi se u RabbitMQ UI-ju).
  U razvoju svi mailovi stižu u Mailpit (`http://localhost:8025`). Cijeli tok je u `torvin.backend/Torvin.API/Torvin.API.http`.
- Poslije 5 pogrešnih lozinki zaredom račun je zaključan 15 minuta. Pogrešno ime i pogrešna lozinka imaju istu
  poruku, da odgovor ne otkriva koji računi postoje.
- Greške 401 i 403 imaju isto tijelo kao ostale greške API-ja (`{ "message": ... }`).

## Referentni podaci

Svaka lista u API-ju je paginirana: `page` (od 1), `pageSize` (1–100, podrazumijevano 20) i `fts` za
pretragu po nazivu i kodu. Odgovor je `{ items, totalCount, page, pageSize }`, a `pageSize` iznad 100 vraća 400.

| Ruta | Šta | Dodatni filter |
|---|---|---|
| `/card-types` | tipovi kartica | — |
| `/card-statuses` | statusi kartica | `code` |
| `/currencies` | valute | `code` |
| `/transaction-types` | tipovi transakcija | `code` |
| `/transaction-statuses` | statusi transakcija | `code` |
| `/transaction-categories` | kategorije transakcija | — |
| `/roles` | role, samo čitanje | — |

- `GET` liste i `GET /{id}` su za svakog prijavljenog korisnika, jer pune padajuće liste u obje aplikacije.
  `POST`, `PUT /{id}` i `DELETE /{id}` su samo za administratora (ostali dobijaju 403).
- Statusi, tipovi transakcija i valute imaju kod (`code`). On se zadaje samo pri kreiranju i poslije se ne
  mijenja, jer se na njega veže logika. Sistemski statusi i tipovi (npr. `Active`, `Transfer`) se ne mogu
  obrisati. Administrator može dodati novi status ili tip, ali on nema ponašanje dok ga kod (prelazi statusa)
  ne uvede.
- Zapis koji se koristi ne može se obrisati, a poruka kaže gdje se koristi, npr. "Valuta EUR se koristi i ne
  može se obrisati (kartice: 2, transakcije: 3)." Broje se i mekano obrisani budžeti i ciljevi štednje, jer
  red u bazi i dalje postoji.
- **Role se samo čitaju.** Prava u kodu su vezana za tačno dvije role (`Admin` i `User`), pa nova rola ne bi
  imala nikakvo dejstvo, a brisanje postojeće bi zaključalo sve njene korisnike. Lista služi za izbor role pri
  upravljanju korisnicima.
- Prva stranica bez pretrage i filtera (ono što puni dropdown) je keširana 10 minuta, a svaka izmjena preko
  API-ja odmah poništi keš tog šifarnika. Pretraga uvijek ide u bazu.

## Kartice

Korisnik (mobilna) radi samo sa svojim karticama, a tuđa kartica vraća 404, isto kao nepostojeća. Administrator
(desktop) vidi sve kartice i ručno mijenja status. Cijeli tok je u `torvin.backend/Torvin.API/Torvin.API.http`.

| Ruta | Ko | Šta |
|---|---|---|
| `GET /cards` | korisnik | moje kartice, najnovija prva; filteri `cardStatusId`, `cardTypeId`, `fts` (naziv, zadnje 4 cifre) |
| `GET /cards/{id}` | korisnik | detalji kartice |
| `POST /cards` | korisnik | nova kartica: tip, valuta, naziv, broj i datum isteka (`MM/GG`), status `Pending` |
| `POST /cards/{id}/verify` | korisnik | aktivacija kodom iz emaila |
| `POST /cards/{id}/resend-code` | korisnik | novi kod (najviše jednom u minuti) |
| `POST /cards/{id}/block`, `/unblock` | korisnik | blokiranje i deblokiranje |
| `POST /cards/{id}/report` | korisnik | prijava gubitka ili krađe (`Lost`, `Stolen`) uz napomenu |
| `GET /admin/cards` | admin | sve kartice; filteri statusa, tipa, korisnika (`userId`) i pretraga po vlasniku |
| `PUT /admin/cards/{id}/status` | admin | ručna promjena statusa uz obavezan razlog |

- **Broj kartice se ne čuva.** Server provjerava 16 cifara, kontrolnu cifru (Luhn) i mrežu iz prvih cifara
  (Visa počinje s 4, Mastercard s 51–55 ili 2221–2720), a u bazi ostaju samo zadnje 4 cifre, mreža i datum
  isteka. CVV se ne traži. Platforma ne naplaćuje ništa s kartice; balans postoji samo unutar sistema.
- Datum isteka `08/29` znači da kartica važi do kraja avgusta 2029. Istekla kartica i datum dalje od 10 godina
  se odbijaju.
- Korisnik može imati najviše 5 kartica koje nisu prijavljene ni zatvorene, a ista kartica (mreža, zadnje 4
  cifre i istek) se ne može dodati dvaput. Valuta se bira pri dodavanju i ne mijenja se.
- Kod za aktivaciju važi 15 minuta, vezan je za tu karticu i ima najviše 5 pokušaja. Prva aktivirana kartica
  postaje podrazumijevana za primanje novca.
- **Prelazi statusa** su na jednom mjestu (`CardStateMachine`):

  | Iz | U | Ko |
  |---|---|---|
  | `Pending` | `Active` | korisnik, kodom iz emaila |
  | `Active` | `Blocked` | korisnik i admin |
  | `Blocked` | `Active` | korisnik (osim kad je blokirao admin) i admin |
  | `Active`, `Blocked` | `Lost`, `Stolen` | korisnik i admin |
  | `Active`, `Blocked`, `Lost`, `Stolen` | `Closed` | samo admin, i samo uz balans 0 |

  Iz `Lost`, `Stolen` i `Closed` nema povratka, a nedozvoljen prelaz vraća 400 s razlogom ("Kartica prijavljena
  kao ukradena ne može se ponovo aktivirati.").
- Odgovor kartice ima `isBlockedByAdmin`: `true` kad je kartica blokirana, a posljednju blokadu je postavio
  administrator. Vlasnik je tada ne može sam deblokirati, pa mobilna to dugme unaprijed onemogući. Oznaka se čita iz
  audit traga jednim upitom po stranici, i to samo za blokirane kartice.
- Svaka promjena statusa ostaje u audit tragu (ko, kad, stari i novi status, razlog) i šalje poruku u red
  `card-status`, iz koje Worker pravi notifikaciju vlasniku (vidi Notifikacije).

## Transakcije

Korisnik (mobilna) šalje novac sa svoje aktivne kartice drugom korisniku i vidi svoju historiju. Tuđa transakcija
vraća 404. Cijeli tok je u `torvin.backend/Torvin.API/Torvin.API.http` (koraci 21–27).

| Ruta | Ko | Šta |
|---|---|---|
| `GET /recipients/search?fts=` | korisnik | primaoci po imenu, prezimenu, korisničkom imenu ili tačnom emailu; najmanje 3 znaka, najviše 10 rezultata |
| `POST /transactions` | korisnik | prenos: `fromCardId`, `toUserId`, `amount`, `note`, `transactionCategoryId` (opcionalno) |
| `GET /transactions` | korisnik | poslane i primljene, najnovije prve; filteri `dateFrom`, `dateTo`, `transactionTypeId`, `transactionStatusId`, `cardId`, `amountFrom`, `amountTo`, `direction` (`Sent`, `Received`), `fts` (napomena, broj reference, druga strana) |
| `GET /transactions/{id}` | korisnik | detalji, iz pogleda pošiljaoca ili primaoca |

- **Valuta je valuta kartice pošiljaoca.** Nema konverzije: primalac mora imati aktivnu karticu u istoj valuti.
  Novac ide na njegovu podrazumijevanu karticu ako je aktivna i u toj valuti, inače na najstariju takvu karticu.
  Klijent nikad ne šalje ID tuđe kartice.
- **Provjere na serveru**, svaka s porukom ispod polja: kartica je moja i aktivna, primalac je drugi aktivan
  korisnik, iznos je između 0,01 i 100.000,00 s najviše dvije decimale (više decimala se odbija, ne zaokružuje),
  ima dovoljno sredstava, kategorija postoji. Blokiran račun ne može slati (403).
- **Sve ili ništa.** Skidanje s jedne kartice, dodavanje na drugu, zapis transakcije i audit zapis (stari i novi
  balans obje kartice) idu u jednoj transakciji baze. Ako bilo koji korak padne, ne ostaje ništa.
- **Dva istovremena prenosa** nad istom karticom ne mogu potrošiti isti balans: kartica ima verziju reda, pa drugi
  upis dobija poruku "Kartica je u međuvremenu promijenjena. Osvježite podatke i pokušajte ponovo.".
- Korisnik vidi broj reference (`TRV20261121004817`), a od tuđe kartice samo zadnje 4 cifre. Primalac ne vidi
  neuspjele prenose, jer se njemu ništa nije desilo.
- Poslije knjiženja API objavljuje događaj `TransactionCompleted` na fanout exchange `transaction-completed`.
  Svaki zainteresovani dio Workera ima svoj red vezan za njega. Prvi je `transaction-completed.notifications`
  (notifikacije, vidi dolje), a budžet i anomalije dobijaju svoje redove poslije.

## Omiljeni primaoci i QR kod

Korisnik čuva primaoce kojima često šalje novac, a QR kodom drugima daje svoju karticu za primanje. Tok je u
`torvin.backend/Torvin.API/Torvin.API.http` (koraci 28–34).

| Ruta | Ko | Šta |
|---|---|---|
| `GET /favorite-recipients` | korisnik | moji omiljeni primaoci, po prezimenu; `fts` (nadimak, ime, prezime, korisničko ime) |
| `POST /favorite-recipients` | korisnik | novi omiljeni: `recipientUserId` (iz pretrage ili QR koda), `nickname` (do 50 znakova) |
| `PUT /favorite-recipients/{id}` | korisnik | novi nadimak |
| `DELETE /favorite-recipients/{id}` | korisnik | uklanjanje (meko brisanje, primalac se kasnije može ponovo dodati) |
| `GET /cards/{id}/receive-code?amount=` | korisnik | kod za QR na moju aktivnu karticu, opcionalno s predloženim iznosom |
| `POST /recipients/resolve-code` | korisnik | skeniran kod → ime primaoca, maskirana kartica, valuta i iznos |

- **Jedan primalac jednom.** Isti primalac ne može dvaput biti među omiljenim ("Primalac je već među omiljenim.").
  Provjera je u servisu, a dva istovremena dodavanja zaustavlja unique indeks u bazi. Omiljeni može biti samo
  korisnik kojem se može poslati novac (aktivan, potvrđen, s aktivnom karticom), i nikad sam korisnik.
- **Poznato ograničenje (svjesna odluka):** omiljeni se dodaje po `recipientUserId`, pa prijavljen korisnik koji
  redom proba ID-eve može saznati ime, prezime i korisničko ime aktivnih korisnika. Email i balans se nikad ne
  vraćaju. Kao u aplikacijama za prenos novca s pretragom po korisničkom imenu, ime primaoca je vidljivo
  prijavljenim korisnicima.
- **Kod je potpisan, ne tajan.** Oblik je `torvin:v1:<podaci>.<potpis>`. Podaci (kartica, iznos, rok) su potpisani
  HMAC-SHA256 ključem `RECEIVE_CODE_KEY` iz `.env`, pa izmijenjen ili tuđim ključem napravljen kod daje "QR kod nije
  ispravan.". Kod važi 15 minuta ("QR kod je istekao. Zatražite novi od primaoca.").
- Pri razrješavanju server ponovo provjeri karticu i primaoca, pa kod blokirane kartice ili blokiranog računa više
  ne radi. Pošiljalac vidi samo zadnje 4 cifre kartice. Prenos zatim ide kao i svaki drugi, na `toUserId`: novac
  ide na primaočevu karticu u valuti pošiljaoca.

## Notifikacije

Notifikacije pravi Worker iz događaja koje objavi API, i upisuje ih u tabelu `Notifications` (nepročitane). Čitanje
kroz API i osvježavanje na mobilnoj dolaze s taskovima 20 i 21.

| Događaj (red) | Ko dobija | Primjer |
|---|---|---|
| `TransactionCompleted` (`transaction-completed.notifications`) | primalac | "Primili ste 1.245,00 BAM na karticu •••• 8241. Pošiljalac: Emir Kovačević." |
| | pošiljalac (potvrda) | "Poslali ste 1.245,00 BAM s kartice •••• 3020. Primalac: Tarik Begić." |
| `CardStatusChanged` (`card-status`) | vlasnik, kad je kartica blokirana, izgubljena ili ukradena | "Kartica •••• 4821 je blokirana." |
| | vlasnik, za svaku promjenu koju uradi administrator (s razlogom) | "Administrator je promijenio status kartice •••• 4821 u "Zatvorena". Razlog: …" |

- Aktivacija i deblokiranje koje uradi sam vlasnik ne prave notifikaciju.
- **Ista poruka ne pravi dvije notifikacije.** Svaka notifikacija pamti Id poruke iz koje je nastala, a jedinstveni
  indeks `(SourceMessageId, UserId)` to čuva i kad se ista poruka obrađuje dvaput u isto vrijeme (npr. poslije
  restarta Workera RabbitMQ ponovo isporuči nepotvrđenu poruku).
- Svaka poruka se obrađuje u svom scope-u baze. Prolazna greška (npr. baza još nije spremna) se ponavlja poslije 1,
  2, 4 i 8 sekundi, a onda poruka ide u `*.dlq`. Neispravna poruka ili nepostojeća transakcija ili kartica idu u
  `*.dlq` odmah. Poruka se potvrđuje tek kad je notifikacija upisana.
- Svi consumeri Workera (`email`, `card-status`, `transaction-completed.notifications`) dijele jednu konekciju
  prema RabbitMQ-u, a svaki ima svoj kanal i obrađuje jednu poruku odjednom.

## Model baze

Baza se zove `230181` i ima 19 glavnih i 7 referentnih tabela. ERD je u `docs/Torvin - ERD.pdf`, a izvor
dijagrama je `docs/erd.mmd`.

- **Referentni podaci su tabele** koje administrator uređuje: tipovi i statusi kartica, valute, tipovi i
  statusi transakcija i kategorije. Statusi i tipovi koje koristi logika imaju nepromjenjiv `Code`, a
  administrator mijenja naziv i opis.
- **Stanja procesa su enumi**: status zahtjeva za novac, poništenja i trajnog naloga, učestalost naloga, tip
  notifikacije. Prelaze između njih određuje kod, pa nova vrijednost koju bi dodao administrator ne bi imala
  ponašanje. U bazi su broj s ograničenjem na dozvoljene vrijednosti.
- **Ništa iz historije se ne briše.** Korisnik i kartica mijenjaju status (blokiran, zatvorena), a transakcija
  se ispravlja korektivnom transakcijom. Meko brisanje (`IsDeleted`) imaju samo obavijesti, ciljevi štednje,
  budžeti i omiljeni primaoci. Referentni zapis koji se koristi baza ne da obrisati.
- Novac je `decimal(19,4)`, a balans kartice ne može ispod nule ni u samoj bazi. Od broja kartice čuvaju se
  samo zadnje 4 cifre i mreža (Visa ili Mastercard), a lozinke i kodovi samo kao hash. Sva vremena su UTC.

## Tehnologije

- Flutter 3.47.5 (Dart 3.13.4)
- Backend: ASP.NET Core Web API, Entity Framework Core, Mapster, SQL Server, RabbitMQ (API šalje, Worker obrađuje), MailKit

## Struktura

| Folder | Sadržaj |
|---|---|
| `torvin.frontend/torvin_mobile` | Flutter mobilna aplikacija (korisnik) |
| `torvin.frontend/torvin_desktop` | Flutter desktop aplikacija (administrator) |
| `torvin.frontend/torvin_core` | Zajednički paket: tema i konfiguracija |
| `torvin.backend` | Backend rješenje `Torvin.slnx` (_u izradi_) |
| `docs` | Dnevnik razvoja (`JOURNAL.md`) i ERD |
