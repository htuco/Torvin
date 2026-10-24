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

### Mobilna aplikacija

```bash
cd torvin.frontend/torvin_mobile
flutter pub get
flutter run --dart-define=API_BASE_URL=http://localhost:5000
```

Za Android emulator je adresa `http://10.0.2.2:5000`. Bez `API_BASE_URL` aplikacija pri startu
javlja grešku.

### Desktop aplikacija

```bash
cd torvin.frontend/torvin_desktop
flutter pub get
flutter run -d macos --dart-define=API_BASE_URL=http://localhost:5000
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
| `torvin.backend` | Backend rješenje (_u izradi_) |
| `docs` | Dnevnik razvoja (`JOURNAL.md`) i ERD |
