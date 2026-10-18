---
name: change-request
description: Implement the instructor's change request for PlantCare starting from the tagged first version (v1.0) - impact analysis, spec update, failing test first, data-preserving migration, regression run, and docs/CHANGE_REQUEST.md. Use when the user pastes or describes the change request.
argument-hint: "<a változtatási igény szövege>"
---

# Változtatási igény végrehajtása

Változtatási igény: $ARGUMENTS

A követelmény szerint a módosítást **a rögzített első verzióból** kell elvégezni, megmutatva az érintett
követelményeket, kódrészeket, a döntés indokát és a **regressziós, valamint adatmegőrzési** ellenőrzéseket.

## 0. Kiindulás
- `git tag --list v1.0` – léteznie kell. Ha nem létezik, állj meg, és kérdezd meg a felhasználót.
- Új ág: `git switch -c change-request v1.0` (vagy ha a main == v1.0 + csak doksi, akkor a mainről; kérdezz).
- Futtasd a `/run-tests` skillt, és mentsd a kiinduló eredményt: `docs/test-results/before-change-*.txt`.

## 1. Hatáselemzés (még kódírás előtt)
- Mely AC-k változnak, melyek maradnak? Mely fájlok érintettek (domain, validáció, kontroller, migráció, UI)?
- Kétértelműség esetén **kérdezd meg a felhasználót**, ne dönts helyette.
- Írd be a `docs/CHANGE_REQUEST.md` „Hatáselemzés” részébe.

## 2. Spec
- Frissítsd a `SPECIFICATION.md`-t: módosított/új AC, a „Változásnapló”-ba új sor (v1.1). Maradj 4000 karakter alatt.

## 3. Teszt először
- Írj a régi kódon **elbukó** tesztet az új szabályra, futtasd, mentsd a kimenetet (`docs/test-results/cr-red.txt`).

## 4. Implementáció
- Minimális változtatás, a meglévő mintákat követve (`CareSchedule`, `*Validator`, kontroller szűrés user szerint).
- Sémaváltozás: `dotnet ef migrations add <Nev>`. A migráció **meglévő sorokat nem törölhet**; új kötelező
  oszlopnál adj alapértéket vagy töltsd fel adatból (SQL a migrációban).

## 5. Adatmegőrzés ellenőrzése
- A `v1.0` sémával létrehozott adatbázison (régi migrációkig) vegyél fel adatot, majd futtasd az új migrációt,
  és ellenőrizd, hogy a növények, események és képek megmaradtak. Ehhez írj integrációs tesztet
  (`IMigrator.MigrateAsync("<előző migráció>")` → adat beszúrása → `MigrateAsync()` → ellenőrzés).

## 6. Regresszió
- Teljes `/run-tests`. Hasonlítsd össze a kiinduló futással: a korábbi tesztek csak akkor változhatnak, ha
  az érintett AC szándékosan változott – ezt egyenként indokold.

## 7. Dokumentáció
- `docs/CHANGE_REQUEST.md`: igény, hatáselemzés, döntés és indok, változott fájlok (`git diff --stat v1.0`),
  új/módosított tesztek, regressziós és adatmegőrzési eredmény.
- `docs/TESTING.md` mátrix és számok, `docs/AI_USAGE.md` ha érdemi MI-döntés történt.
