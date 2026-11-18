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
dotnet test Torvin.slnx
```

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

### Desktop aplikacija

```bash
cd torvin.frontend/torvin_desktop
flutter pub get
flutter run -d macos --dart-define=API_BASE_URL=http://localhost:5080
```

Za predaju se pravi Windows build, a na Macu se razvija na macOS-u. Desktop prijava radi samo za
administratore. Token se čuva u sigurnom skladištu sistema (Windows Credential Manager, macOS Keychain), a kad istekne ili
bude opozvan, aplikacija se vrati na prijavu.

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
  samo zadnje 4 cifre, a lozinke i kodovi samo kao hash. Sva vremena su UTC.

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
