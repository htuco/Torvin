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

_U izradi._ Ovdje će biti komande za podizanje baze, RabbitMQ-a, API-ja i Workera kroz
`docker compose`.

### Backend (lokalno, bez Dockera)

Treba .NET SDK 10 i fajl `.env` u rootu repoa s varijablama konfiguracije (`CONNECTION_STRING`, `JWT_*`, `RABBITMQ_*`,
`SMTP_*`, `CORS_ORIGINS`, `API_PORT`). API i Worker ga u Development okruženju učitaju sami.

```bash
cd torvin.backend
dotnet run --project Torvin.API
dotnet run --project Torvin.Worker
dotnet test Torvin.slnx
```

Ako varijabla fali, aplikacija ne starta i ispiše koje varijable nedostaju. Swagger je na
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
