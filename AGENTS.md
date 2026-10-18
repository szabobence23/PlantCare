# AGENTS.md – útmutató MI-kódolóügynököknek

Ez a fájl minden MI-eszköznek (Claude Code, Codex, Copilot, Gemini) szól, amely ebben a repóban dolgozik.
A `CLAUDE.md` erre hivatkozik, ezért ez az egyetlen igazságforrás.

## A projekt
PlantCare: növénygondozó webalkalmazás, BME „Szoftverfejlesztés MI támogatással” házi feladat.
A követelmények forrása a [SPECIFICATION.md](SPECIFICATION.md) (AC1–AC8). **Csak azt valósítsd meg, ami ott le van írva.**
Ha a spec kétértelmű, ne találgass: kérdezd meg a felhasználót, és a választott értelmezést írd be a spec
„Bizonytalanságok” részébe.

## Felépítés
```
backend/src/PlantCare.Api      ASP.NET Core Web API (.NET 10), EF Core + PostgreSQL
  Domain/                      entitások + CareSchedule (tiszta esedékességi szabályok)
  Validation/                  bemeneti szabályok (növény, kép, regisztráció) – magyar hibaüzenetek
  Recognition/                 Pl@ntNet kliens (IPlantRecognitionService)
  Controllers/                 REST végpontok, mind a bejelentkezett user adataira szűr
  Data/Migrations/             EF Core migrációk – a séma csak migrációval változhat
backend/tests/PlantCare.Tests  xUnit: Unit/ (tiszta logika) és Integration/ (Testcontainers PostgreSQL)
frontend/                      React 19 + TypeScript + Vite; src/pages, src/components, src/api
frontend/e2e                   Playwright E2E (helyi Chrome, channel: 'chrome')
docs/                          TESTING.md, AI_USAGE.md, BUG_DEMO.md, CHANGE_REQUEST.md
```

## Parancsok
| Cél | Parancs (repó gyökeréből) |
|---|---|
| Adatbázis | `docker compose up -d` (PostgreSQL a **5433**-as porton) |
| Backend futtatás | `dotnet run --project backend/src/PlantCare.Api --launch-profile http` (http://localhost:5080) |
| Frontend futtatás | `cd frontend && npm run dev` (http://localhost:5173, `/api` proxy a backendre) |
| Backend tesztek | `dotnet test backend` (Docker kell a Testcontainers miatt) |
| Egy AC tesztjei | `dotnet test backend --filter "AC=AC3"` |
| Frontend tesztek | `cd frontend && npm test` |
| E2E | `cd frontend && npm run e2e` (DB fusson) |
| Lint + build | `cd frontend && npm run lint && npm run build` |
| Új migráció | `cd backend && dotnet ef migrations add <Nev> -p src/PlantCare.Api -o Data/Migrations` |

## Szabályok
1. **Spec először.** Minden viselkedésváltozás előtt nézd meg, melyik AC-t érinti. Ha új szabály kell, előbb a spec.
2. **Minden üzleti szabályhoz automatizált teszt**, `[Trait("AC", "ACx")]` jelöléssel (backend), illetve
   az AC-azonosító a teszt nevében (frontend). Lefedendő: normál eset, határeset, hibás bemenet.
3. **A dátumszabályok a `CareSchedule`-ban élnek** (tiszta függvények). „Ma” mindig `IAppClock.Today`
   (Europe/Budapest), soha ne `DateTime.Now`/`DateTime.Today`. Kivétel: a JWT lejárata valódi órát használ.
4. **Tulajdonjog:** minden lekérdezés `UserId`-ra szűr; idegen növény → 404 (nem 403). Új végpontnál
   bővítsd az `Other_users_plant_is_invisible_and_cannot_be_changed` tesztet.
5. **Validáció a szerveren** történik, magyar üzenettel, `{ errors: { mező: [üzenet] } }` formában;
   a frontend `noValidate` űrlapokkal ezeket jeleníti meg a mező alatt.
6. **Séma csak migrációval.** A `MigrationTests` elbukik, ha a modell migráció nélkül változik.
   Meglévő adatot a migráció nem dobhat el (lásd a `change-request` skillt).
7. **Titok nem kerül a repóba.** A Pl@ntNet kulcs user-secretsben vagy `PlantNet__ApiKey` környezeti változóban van.
8. **UI:** sötét téma, a `src/index.css` tokenjeit (`--surface`, `--accent`, …) használd, ne új színeket.
   Felület szövegei magyarul, kód és kommentek angolul.
9. **Dokumentáció együtt mozog a kóddal:** ha teszt, AC vagy viselkedés változik, frissítsd a
   `docs/TESTING.md` mátrixát és a tesztszámokat. Az MI-döntéseket a `docs/AI_USAGE.md`-be írd,
   és **ne tulajdoníts a hallgatónak olyan döntést, amit nem ő hozott**.

## Kész, ha
- `dotnet test backend`, `npm test`, `npm run lint`, `npm run build` és az E2E zöld;
- az érintett AC-k a `docs/TESTING.md`-ben a megfelelő tesztekre mutatnak;
- a commit üzenet leírja, mit és miért.

## Elérhető skillek és ügynökök (`.claude/`)
- `/run-tests` – az összes tesztszint futtatása és az eredmények rögzítése a `docs/test-results/` alá.
- `/verify-ac <ACx>` – egy elfogadási feltétel teljes ellenőrzése (tesztek + kézi lépések).
- `/change-request` – az oktatói változtatási igény végigvitele a `v1.0` tagtől, regresszióval.
- `spec-guardian` ügynök – csak olvas: összeveti a kódot, a teszteket és a dokumentációt a spec-kel.
- `test-reviewer` ügynök – csak olvas: AC-nként megnézi, van-e normál, határ-, hibás és regressziós eset.
