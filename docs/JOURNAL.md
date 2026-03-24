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
- Danas uradjeno: Uklonjene nepotrebne distribucije aplikacije za Windows, Linux, macOS i Web.
- Problemi/blokade: Nema.
- Rjesenje/odluke: Smanjen scope projekta i kompleksnost odrzavanja build konfiguracija.
- Sljedece: Nastaviti razvoj na prioritetnim platformama prema planu.

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
