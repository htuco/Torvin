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

### [2026-11-18] Registracija, kodovi na email i prvi posao Workera
- Kontekst: Drugi dio autentifikacije. Upute traže registraciju bez role, reset lozinke kodom s istekom i Worker koji radi stvaran posao preko RabbitMQ-a, s ponovnim pokušajima i redom za neuspjele poruke.
- Danas uradjeno: Registracija, potvrda emaila, zaboravljena lozinka i reset. Šestocifreni kodovi s rokom, najviše 5 pokušaja i jednokratnom upotrebom. API objavljuje mail u red `email` tek kad je sve snimljeno u bazi, a Worker ga šalje preko SMTP-a (Mailpit u razvoju). Neuspjelo slanje se ponavlja poslije 1, 2, 4 i 8 sekundi, pa poruka ide u `email.dlq`. Trajna greška (neispravna adresa, server odbio primaoca) ide odmah u `email.dlq`. Prijava nepotvrđenog računa šalje novi kod. `.http` fajl s cijelim tokom. 47 novih testova.
- Problemi/blokade: Worker kontejner nije više startao: zajednički projekat servisa od prijave koristi ASP.NET biblioteke, a Worker image ih nije imao. Kod je prvo bio hashiran SHA-256, a milion mogućih kodova se iz takvog hasha izračuna za tren. Test je pokazao da pogrešan pokušaj drugog zahtjeva u istom trenutku obara i tačan kod. Ako RabbitMQ ne radi, zaboravljena lozinka bi za postojeći račun vratila grešku, a za nepostojeći uspjeh.
- Rjesenje/odluke: Worker image je sada `aspnet`. Kod se hashira kao lozinka (PBKDF2 sa solju). Brojač pokušaja i oznaka iskorištenosti koda se provjeravaju pri svakom upisu, pa paralelni zahtjevi ne dobiju više od 5 pokušaja ni dvaput isti kod, a pogrešan tuđi pokušaj samo ponovi upis. Pokušaj se snima odmah, odvojeno od ostatka operacije, da ostane zapisan i kad operacija padne. Zaboravljena lozinka grešku slanja samo zapiše u log i vrati isti odgovor. Registracija tada vraća grešku, a ne lažno "poslali smo kod", i korisnik novi kod dobija prijavom. Isti mail poslan dvaput je bezopasan, pa evidencija poslanih poruka dolazi s notifikacijama. Reset ne poništava već izdate tokene, a oni ionako ističu za sat.
- Sljedece: CRUD referentnih podataka (task 06), pa prijava iz Flutter aplikacija (07).

### [2026-11-19] Paginacija i CRUD referentnih podataka
- Kontekst: Upute traže CRUD za sve referentne podatke, filtere na listama, paginaciju s gornjom granicom na svakom list endpointu i jasnu poruku kad se zapis u upotrebi ne može obrisati.
- Danas uradjeno: Zajednička osnova za liste i CRUD (`BaseReadService`, `BaseCRUDService` i odgovarajući controlleri), paginacija s najviše 100 zapisa po stranici i pretraga. Preko nje CRUD za tipove i statuse kartica, valute, tipove i statuse transakcija i kategorije, uz čitanje rola. Mapiranje preko Mapstera (10.0.14). Prva stranica bez pretrage je u kešu, koji svaka izmjena poništi. 21 novi test.
- Problemi/blokade: Provjera "da li se zapis koristi" bi propustila mekano obrisane budžete, a baza bi brisanje odbila, pa bi korisnik dobio grešku servera. Dva istovremena zahtjeva mogu proći provjeru duplikata, pa bi drugi pao na unique indeksu. Poruka "na 12 kartica" traži drugačiji padež za svaki broj.
- Rjesenje/odluke: Mekano obrisani zapisi se broje. Ako baza ipak odbije izmjenu, korisnik dobija istu poruku kao kod provjere, a ne grešku servera. Kod statusa i tipova se zadaje samo pri kreiranju, a sistemski se ne brišu, da logika koja se veže za njih ostane ispravna. Poruka nabraja gdje se zapis koristi ("kartice: 2, transakcije: 3"). Role se samo čitaju, jer su prava u kodu vezana za tačno dvije role. Liste su poredane po ID-u, pa se stranice ne preklapaju. Keš ima verziju po šifarniku: izmjena poveća verziju, pa se stari unosi više ne čitaju. Pretrage se ne keširaju, da niko ne može nasumičnim upitima puniti memoriju API-ja.
- Sljedece: Prijava iz Flutter aplikacija (07).

### [2026-11-19] Prijava iz Flutter aplikacija
- Kontekst: Prvi put cijeli tok ide od ekrana do baze. Upute traže adresu API-ja kroz `--dart-define`, validaciju ispod polja s formatom, povratak na prijavu kad sesija istekne i poruke backenda prikazane korisniku.
- Danas uradjeno: U `torvin_core` su jedan API klijent (token u headeru, 401 vraća na prijavu, greške backenda po poljima), sesija u sigurnom skladištu sistema, stanje prijave preko Riverpoda i validatori s istim pravilima i porukama kao backend. Mobilna ima prijavu, registraciju s potvrdom emaila, zaboravljenu lozinku i reset. Desktop ima prijavu za administratore i okvir sa sidebarom. Oba routera dijele ista pravila preusmjeravanja. 41 test u Flutteru.
- Problemi/blokade: Backend `char.IsUpper` broji "Č" i "Š" kao velika slova, a prvi validator lozinke na klijentu nije, pa bi ispravnu lozinku odbio. Serverska greška polja ostaje na polju i blokira ponovno slanje forme. Odjava s već opozvanim tokenom bi pokazala poruku "prijava je istekla". Ručni tok se ne može provjeriti na Macu.
- Rjesenje/odluke: Validator koristi Unicode klase slova. Serverska greška se briše pri prvoj izmjeni polja. Odjava odmah vraća na prijavu, a lokalni token briše i kad server ne odgovori. Poruka o isteku se pokaže samo kad je korisnik bio prijavljen. Pravila za ponovno slanje koda su ostala na backendu, a ekran za kod kaže da novi kod stiže pri prijavi. Ugovor s API-jem je provjeren curl-om na compose stacku, a pokretanje aplikacija ide na Windows PC.
- Sljedece: Tema iz dizajna v2 (task 09) i CI s Windows buildom (08).

### [2026-11-19] CI i Windows build u GitHub Actions
- Kontekst: Predaja traži Windows build testiran pokretanjem `.exe` i Android APK, a razvoj ide na Macu, gdje se Windows build ne može napraviti. Svaki PR je dosad provjeravan samo lokalno.
- Danas uradjeno: Workflow `ci` na svakom PR-u prema `development` i `main`: build backenda bez upozorenja, testovi backenda, te analiza i testovi za sva tri Flutter projekta. Ručni workflow `build-release` pravi Windows `Release/` na Windows runneru i `app-release.apk` na Linuxu i nudi ih kao artefakte.
- Problemi/blokade: Windows build Fluttera nikad nije bio pokrenut, pa se tek u CI-ju vidi grade li se pluginovi (sigurno skladište) za Windows.
- Rjesenje/odluke: Verzije Fluttera i .NET-a u workflowima su iste kao lokalno. Backend testovima ne trebaju baza ni RabbitMQ, jer koriste bazu u memoriji, pa CI ne podiže servise. Sve provjere su u jednom jobu `ci`, koji je obavezan prije mergea u `development`. ZIP za predaju i release tag ostaju za samu predaju.
- Sljedece: Tema iz dizajna v2 (task 09).

### [2026-11-19] Tema iz dizajna v2
- Kontekst: Aplikacije su još imale ljubičastu temu iz šablona, a font se skidao s interneta pri pokretanju. Upute traže čitljiv izgled bez jarkih boja i providnih elemenata i aplikaciju koja radi bez mreže. Izgled sada dolazi iz dizajna v2, a raspored ekrana i dalje prati prijavu.
- Danas uradjeno: Nova tema u `torvin_core` za svijetli i tamni način. Primarna boja je crvena `#E7000B`, a font Inter je spakovan u paket. Zaobljenja su 20/14/12, a površine pune. Statusne boje, sidebar, grafikoni i kartice su u `TorvinColors`. Ikone su Lucide, kao u dizajnu. Dodani su zajednički widgeti `StatusChip`, `MoneyText`, `EmptyState`, `ErrorState`, `SectionHeader` i `TorvinCard` (preko paketa `u_credit_card`), formateri `1.245,00 BAM` i `05.05.2026 · 14:22` te prekidač teme (sunce/mjesec), koji pamti izbor na uređaju. Sva polja forme su zajednički `TorvinTextField`: labela je iznad polja, a format ili greška s ikonom ispod, kao u dizajnu. Kod iz emaila se unosi u šest kućica (`OtpField`). Admin ima sidebar i top bar po dizajnu. 33 nova testa.
- Problemi/blokade: `2,675 * 100` u double-u daje `267,4999…`, pa bi obično zaokruživanje dalo `2,67`. Bijeli tekst na crvenoj u tamnoj temi ima kontrast 3,65:1. Paket kartice traži pun broj, ispisuje balans kao `$1245.00` i tip na engleskom. Dugme forme je imalo fiksnu visinu 48, a desktop po dizajnu ima 40. Material polje smanjuje labelu u ivicu, a grešku uvlači za padding polja. Aplikacije se ne pokreću na Macu, pa vizuelni pregled čeka Windows PC.
- Rjesenje/odluke: Backend čuva novac s 4 decimale, pa formater prvo pređe na cijeli broj desetohiljaditih dijelova i zaokružuje nad cijelim brojem. Tekst na primarnoj u tamnoj temi je tamni, kao u dizajnu, a test provjerava kontrast od najmanje 4,5:1 za sve parove u obje teme. Kartica dobija samo zadnje 4 cifre, sa zvjezdicama ispred. Tip i balans paketa su isključeni, pa ih prikazujemo ispod kartice. Visinu dugmadi i inputa daje tema: mobilna 48, desktop 40. Polje je napravljeno na `FormField`-u, pa labelu i grešku crtamo sami, a validacija i greške s backenda rade kao i prije. Mreža kartice (Visa, Mastercard) se još ne čuva, pa logo ne postoji.
- Sljedece: Release sprinta 0 u `main` i Windows build, pa API kartica (task 10).

### [2026-11-19] Kartice i prelazi statusa
- Kontekst: Prvi poslovni dio API-ja. Prijava traži dodavanje kartice uz kod iz emaila, blokiranje, prijavu gubitka ili krađe i ručnu promjenu statusa kod administratora. Upute traže da prelazi statusa budu na jednom mjestu, a ne razbacani po controllerima, i da korisnik dira samo svoje podatke.
- Danas uradjeno: `CardStateMachine` drži sve dozvoljene prelaze, posebno za vlasnika i administratora. Korisnik dodaje karticu unosom broja i datuma isteka, a server provjerava kontrolnu cifru i mrežu (Visa ili Mastercard) i čuva samo zadnje 4 cifre, mrežu i istek. Kod za aktivaciju je vezan za tu karticu. Svaki prelaz ostavlja audit zapis i šalje poruku u novi red `card-status`. Admin ima listu svih kartica s filterima i promjenu statusa uz razlog. Paginacija je izdvojena u jedan helper koji koriste i šifarnici. 76 novih testova.
- Problemi/blokade: Kod iz emaila je dosad tražen po emailu, pa bi novi kod jedne kartice poništio kod druge. Nova kolona mreže bi na bazi koja već ima kartice dobila 0, a ograničenje dozvoljava samo 1 i 2. Dva istovremena prelaza iste kartice bi korisniku dala grešku servera. Korisnik bi mogao sam deblokirati karticu koju je blokirao administrator. Ručna provjera na SQL Serveru i RabbitMQ-u nije urađena, jer Docker nije bio pokrenut.
- Rjesenje/odluke: Kod kartice se traži i poništava po kartici, a postojeći tokovi registracije i reseta rade isto kao prije. Postojeće kartice pri migraciji postaju Visa. Konflikt verzije reda daje poruku "Kartica je u međuvremenu promijenjena". Ko je blokirao karticu čita se iz audit traga, bez nove kolone. Historija statusa je u `AuditLogs`, bez nove tabele. Korisnik ima najviše 5 otvorenih kartica, a ista kartica se ne dodaje dvaput. Zatvaranje je samo za administratora i samo uz balans 0. Greška pri slanju maila s kodom ili poruke o statusu se loguje, jer je kartica već snimljena. Dvije istovremene aktivacije dobijaju istu poruku o konfliktu, a ne grešku servera.
- Sljedece: Ručni tok iz `Torvin.API.http` na compose stacku, pa interna transakcija (task 11).
