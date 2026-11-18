# Torvin Development Journal

Mjesto za biljeske o avanturi razvoja aplikacije.

## Kako koristiti
- Dodaj novi unos pri svakom vecem koraku.
- Drzi fokus na: sta je uradjeno, sta je zapelo, sta je sljedece.
- Ako treba, zalijepi i kratke code snippetove ili komande.

---

## Template unosa

### [YYYY-MM-DD] Naslov unosa
- Kontekst:
- Danas uradjeno:
- Problemi/blokade:
- Rjesenje/odluke:
- Sljedece:
---

## Unosi

### [2026-03-23] Pocetak dnevnika
- Kontekst: Pokrenut dnevnik razvoja za Torvin aplikaciju.
- Danas uradjeno: Kreiran JOURNAL.md fajl.
- Problemi/blokade: Nema.
- Rjesenje/odluke: Voditi kontinuitet unosa nakon svake bitne promjene.
- Sljedece: Dodati prvi pravi razvojni unos nakon narednog taska.

### [2026-03-23] Razvojni pristup: frontend first
- Kontekst: Po uputama mentora na drugom projektu, pokazalo se korisnim krenuti frontend first pristupom.
- Danas uradjeno: Dogovoreno da isti pristup primijenimo i na Torvin projektu.
- Problemi/blokade: Nema trenutnih blokada.
- Rjesenje/odluke: Prvo gradimo UI tokove i korisnicko iskustvo, zatim backend prilagodjavamo potrebama ekrana.
- Sljedece: Definisati prioritetne ekrane i napraviti prvi UI prototip.

### [2026-03-23] Uklanjanje nepotrebnih distribucija
- Kontekst: Fokus projekta je na ciljanim platformama, bez potrebe za dodatnim build targetima.
- Danas uradjeno: Uklonjene nepotrebne distribucije aplikacije za Windows, Linux i macOS, dok Web konfiguracija ostaje u projektu.
- Problemi/blokade: Nema.
- Rjesenje/odluke: Smanjen scope projekta uklanjanjem desktop build targeta, uz zadrzavanje postojecih Web fajlova.
- Sljedece: Nastaviti razvoj na prioritetnim platformama prema planu, uz kasniju odluku o potrebi punog Web targeta.

### [2026-03-23] Ciscenje default Flutter postavki
- Kontekst: Prije uvodjenja feature-based arhitekture potrebno je ukloniti starter boilerplate.
- Danas uradjeno: Obrisani default counter demo i test, azuriran bootstrap ekran i projektna dokumentacija.
- Problemi/blokade: Nema.
- Rjesenje/odluke: Projekat je ostavljen u cistom stanju kao baza za naredni arhitekturalni korak.
- Sljedece: Implementirati feature-based folder strukturu i osnovne core module.

### [2026-03-24] Uklonjen debug banner
- Kontekst: Potreban cist prikaz aplikacije tokom razvoja i demo sesija.
- Danas uradjeno: Iskljucen Flutter debug banner u glavnoj aplikaciji.
- Problemi/blokade: Nema.
- Rjesenje/odluke: Zadrzavamo debug build, ali bez vizuelnog debug bannera.
- Sljedece: Nastaviti postavljanje feature-based arhitekture.

### [2026-10-23] Sredjivanje repoa prije sprinta 0
- Kontekst: Prelazak na novi Mac i pocetak ozbiljnog rada kroz PR-ove. Repo je imao .gitignore prepisan iz Flutter SDK repoa, koji je ignorisao i `pubspec.lock`.
- Danas uradjeno: Novi .gitignore samo za ovaj repo (.NET, Flutter, .env, OS i IDE), `pubspec.lock` se sada prati, dodani `.editorconfig` i PR sablon, README prepisan na bosanski kao skelet.
- Problemi/blokade: Na Macu je Flutter 3.47.5, a README je tvrdio 3.41.4. `flutter pub get` je sam dopisao izuzetke u `analysis_options.yaml`.
- Rjesenje/odluke: Jedan root .gitignore umjesto posebnog u Flutter aplikaciji. U README ide samo ono sto stvarno radi, ostalo je oznaceno "u izradi".
- Sljedece: Uskladiti `main` s `development`, ukljuciti zastitu grana na GitHubu, pa krenuti na strukturu frontenda i backend skelet.

### [2026-10-24] Struktura frontenda: mobile, desktop, core
- Kontekst: Upute traže dvije aplikacije (mobilna za korisnika, desktop za administratora) i jedan izvor teme i konfiguracije.
- Danas uradjeno: `torvin` preimenovan u `torvin_mobile` (`git mv`), bundle id `ba.fit.torvin.mobile`, uklonjen `web/`. Kreiran `torvin_core` (tema, `AppConfig`) i `torvin_desktop` (Windows i macOS). Router prebačen na `go_router`.
- Problemi/blokade: Tema je koristila `flutter_screenutil` (`.w`, `.h`, `.sp`), što ne ide u zajednički paket za desktop. `google_fonts` 9 nije radio s Flutterovim `TextTheme`.
- Rjesenje/odluke: Konstante teme su obični brojevi, a `flutter_screenutil` je uklonjen jer ga niko ne koristi. `google_fonts` ostaje na 6.3. Riverpod stiže tek kad zatreba prvi provider.
- Sljedece: Backend skelet (task 02), pa Docker i env.

### [2026-10-24] Backend skelet
- Kontekst: Svaki sljedeći feature treba gotov okvir: slojevi, obrada grešaka i konfiguracija iz okruženja.
- Danas uradjeno: Rješenje `Torvin.slnx` s projektima Model, Services, API, Worker i Tests. Custom izuzeci i `ErrorResponse`, `ExceptionMiddleware` i `CorrelationIdMiddleware`, Options klase iz env varijabli s validacijom pri startu, `DotEnvLoader` za lokalni razvoj, CORS i Swagger. 28 testova, među njima i `WebApplicationFactory` testovi pravog `Program.cs`. Imena env varijabli su na jednom mjestu (`EnvVars`), a `API_PORT` ima svoje Options.
- Problemi/blokade: Port 5000 na Macu zauzima AirPlay prijemnik, pa su prvi odgovori dolazili od Applea. Framework poruke za greške vezivanja vrijednosti su na engleskom.
- Rjesenje/odluke: Nema `/health` rute (anonimna ruta bi bila vidljiva izvana), liveness ide TCP healthcheckom u Dockeru. Jedan oblik greške `{ message, errors? }` za sve statuse, bez 422. API prijavljuje sve neispravne varijable odjednom.
- Sljedece: Docker compose i `.env.example` (task 03).

### [2026-11-18] Docker compose i .env
- Kontekst: Upute traže da se cijeli sistem podiže jednom komandom, s Workerom kao zasebnim kontejnerom i konfiguracijom samo u `.env`.
- Danas uradjeno: Dockerfile za API i Worker u dvije faze (SDK za build, manji runtime image za pokretanje, bez root korisnika). `docker-compose.yml` sa SQL Serverom, RabbitMQ-om, Mailpitom, API-jem i Workerom, s tačnim verzijama imagea i healthcheckovima. `.env.example` sa svim varijablama.
- Problemi/blokade: Lozinka za bazu je stajala i u `SQL_SA_PASSWORD` i unutar `CONNECTION_STRING`, a upute kažu da se ista vrijednost ne piše na dva mjesta. SQL Server image ne postoji za ARM, pa na Macu ide kroz Rosettu.
- Rjesenje/odluke: `CONNECTION_STRING` je zamijenjen sa `DB_HOST`, `DB_PORT` i `DB_NAME`, a lozinka je samo u `SQL_SA_PASSWORD`. Jedan `.env` je napisan za `localhost`, a compose kontejnerima sam zamijeni adrese u imena servisa. Baza je na računaru na portu 1401, da ne smeta SQL Serveru koji je možda već instaliran na 1433.
- Sljedece: Model baze, migracija i seed (task 04).

### [2026-11-18] Model baze i prva migracija
- Kontekst: Upute traže bar 10 glavnih tabela, sve veze kroz EF konfiguraciju i bazu `230181` koju API sam kreira. Model pokriva sve iz prijave, a stari ERD je bio prije prijave.
- Danas uradjeno: 19 glavnih i 7 referentnih tabela, svaka sa svojom konfiguracijom. `TorvinDbContext`, bazna klasa s vremenima nastanka i izmjene, interceptori za ta vremena i za meko brisanje. Migracija `InitialCreate` sa referentnim podacima, a API je primijeni pri startu. Novi ERD u `docs/` (izvor `erd.mmd`). 5 novih testova.
- Problemi/blokade: SQL Server je odbio dva `SET NULL` FK-a iz transakcije prema karticama ("multiple cascade paths"). EF 10.0.15 je tražio novije Microsoft pakete, a SQL klijent je povukao paket s poznatom ranjivošću, pa build s `-warnaserror` nije prolazio.
- Rjesenje/odluke: Kartica se ne briše nego ide u status `Closed`, pa FK ostaje bez kaskade, a transakcija i dalje pamti pošiljaoca i primaoca. Meko brisanje je samo na obavijestima, ciljevima štednje, budžetima i omiljenim primaocima, jer bi skriven korisnik sakrio i tuđu historiju transakcija. Statusi koje admin ne uređuje su enumi, a statusi iz prijave tabele s nepromjenjivim kodom. Svi EF paketi su na 10.0.13 kao ostali, a zakrpljen paket se navodi direktno. Balans ne može ispod nule i iznosi moraju biti pozitivni već u samoj bazi.
- Sljedece: Demo podaci sa slikama (drugi dio taska 04), pa autentifikacija.

### [2026-11-18] Demo podaci za testiranje
- Kontekst: Upute kažu da se rad s premalo podataka ne gleda dalje, a svaki ekran treba nešto za prikaz: kartice u svim statusima, zahtjeve u svim stanjima, budžete za tekući mjesec, obavijesti sa slikama.
- Danas uradjeno: `DataSeeder` koji pri prvom startu upiše 8 korisnika, 14 kartica, oko 95 transakcija kroz zadnjih 100 dana (i nekoliko današnjih, za dnevni prikaz na dashboardu), poništenja, zahtjeve za novac, trajne naloge s historijom pokušaja, štednju, budžete, notifikacije koje pokazuju na svoj zapis i 6 obavijesti sa slikama. `PasswordHasher` (PBKDF2), koji će koristiti i prijava. Testovi za seed i hasher.
- Problemi/blokade: Interceptor je svakom novom zapisu upisivao trenutno vrijeme, pa bi sve transakcije iz seeda imale današnji datum. Audit zapis treba ID kartice ili zahtjeva, a ID postoji tek poslije snimanja.
- Rjesenje/odluke: Interceptor zadržava vrijeme nastanka ako je već zadano. Seed snima u dva koraka unutar jedne transakcije, pa ili upiše sve ili ništa. Balansi se ne upisuju ručno nego računaju iz početnog iznosa i historije, a seed puca ako bi ijedan bio negativan. Datumi idu od trenutka seedanja, da budžet "ovog mjeseca" i grafikoni imaju podatke i na dan odbrane. Slike su jednostavne ilustracije ugrađene u dll.
- Sljedece: Autentifikacija (task 05).

### [2026-11-18] Prijava i odjava preko JWT-a
- Kontekst: Upute traže JWT, odjavu koja token stvarno poništi na serveru i zaštitu svakog endpointa. Desktop je samo za administratore, a mobilna za korisnike.
- Danas uradjeno: Prijava korisničkim imenom ili emailom, s aplikacijom u zahtjevu. Odjava upiše token u listu opozvanih, a provjera ide kroz keš na svakom zahtjevu. Zaključavanje računa poslije 5 pogrešnih lozinki. 401 i 403 vraćaju istu poruku greške kao ostatak API-ja. Swagger ima dugme za token. 25 novih testova.
- Problemi/blokade: ASP.NET je sam dodavao engleske poruke ("The request field is required.", greške JSON parsera), a greška za polje je stizala pod ključem `$.app`. Globalno pravilo "sve traži prijavu" bi nepostojeću rutu pretvorilo iz 404 u 401.
- Rjesenje/odluke: Poruke frameworka su prevedene ili isključene, a ključ greške je ime polja. `[Authorize]` stoji na svakom controlleru, a test pada ako neki controller nema `[Authorize]`. Pogrešna lozinka je 400, a ne 401, jer 401 u aplikaciji znači "sesija je istekla". Enumi u JSON-u idu kao tekst.
- Sljedece: Registracija s kodom na email i reset lozinke, s mailovima koje šalje Worker (drugi dio taska 05).
