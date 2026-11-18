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

Za Android emulator je adresa `http://10.0.2.2:5080`. Bez `API_BASE_URL` aplikacija pri startu
javlja grešku.

### Desktop aplikacija

```bash
cd torvin.frontend/torvin_desktop
flutter pub get
flutter run -d macos --dart-define=API_BASE_URL=http://localhost:5080
```

Za predaju se pravi Windows build, a na Macu se razvija na macOS-u.

## Pristupni podaci

_U izradi._ Testni korisnici nastaju seedom baze:

| Aplikacija | Korisničko ime | Lozinka |
|---|---|---|
| Desktop | `desktop` | `test` |
| Mobilna | `mobile` | `test` |

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
- Backend: ASP.NET Core Web API, Entity Framework Core, SQL Server, RabbitMQ (_u izradi_)

## Struktura

| Folder | Sadržaj |
|---|---|
| `torvin.frontend/torvin_mobile` | Flutter mobilna aplikacija (korisnik) |
| `torvin.frontend/torvin_desktop` | Flutter desktop aplikacija (administrator) |
| `torvin.frontend/torvin_core` | Zajednički paket: tema i konfiguracija |
| `torvin.backend` | Backend rješenje `Torvin.slnx` (_u izradi_) |
| `docs` | Dnevnik razvoja (`JOURNAL.md`) i ERD |
