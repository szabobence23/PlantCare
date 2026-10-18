# MI-használat és döntések

## Eszköz, modell, munkamódszer
| | |
|---|---|
| Eszköz | **Claude Code** (VS Code bővítmény, Windows 11) |
| Modell | Claude Opus 5.5 (`claude-opus-5-5`) |
| Mód | ügynöki (agentic) munka: az MI fájlokat olvasott és írt, parancsokat futtatott (`dotnet`, `npm`, `docker`, `git`), és a futások eredményéből javított |
| Bizonyíték | a commitokban `Co-Authored-By: Claude` sor; ez a dokumentum; [AGENTS.md](../AGENTS.md), [.claude/](../.claude) |

**Munkamenet (2026-10-18).** A hallgató átadta a saját specifikációját (PDF) és a tárgy követelménykiírását
(PDF) azzal az utasítással, hogy a házi a specifikáció szerint, se többet, se kevesebbet ne valósítson meg,
legyen sötét, minimalista frontend, mindent dokumentáljon, és kérdés esetén kérdezzen. Az ügynök:
1. a spec alapján **négy tervezési kérdést tett fel** választható opciókkal és indoklással; ezekről a hallgató döntött (lásd a döntési eseteket);
2. megírta a backendet, futtatta, `curl`-lel végigpróbálta a végpontokat;
3. megírta a teszteket, futtatta őket, a frontend képernyőképeit megnézte és a stíluson javított;
4. a Pl@ntNet kliens feltételezéseit a hivatalos API-dokumentációval vetette össze;
5. elkészítette a dokumentációt és az MI-t támogató repóelemeket.

### Fázisok, ahol MI-t használtunk
| Fázis | Mit csinált az MI | Ellenőrzés |
|---|---|---|
| Követelményelemzés | a spec 8 feltételét számozott, mérhető AC1–AC8-ra bontotta; összegyűjtötte a bizonytalanságokat ([SPECIFICATION.md](../SPECIFICATION.md)); kérdéseket tett fel | hallgatói döntés a nyitott kérdésekről; minden AC-hez teszt ([TESTING.md](TESTING.md)) |
| Tervezés, implementáció | ASP.NET Core API, EF Core modell és migráció, React UI | build, `curl` smoke teszt, 91 backend + 14 frontend + 1 E2E teszt |
| Tesztelés | tesztstratégia, Testcontainers infrastruktúra, határeset-tesztek, szándékosan hibás változat | futtatási kimenetek: [test-results/](test-results/), [BUG_DEMO.md](BUG_DEMO.md) |
| Dokumentáció | README, TESTING, ez a fájl, AGENTS.md, skillek | a hivatkozott tesztnevek és fájlok a repóban vannak (a `spec-guardian` ügynök ezt ellenőrzi) |

## MI-t támogató elemek a repóban
| Elem | Szerep |
|---|---|
| [`AGENTS.md`](../AGENTS.md) | Közös útmutató minden kódolóügynöknek: felépítés, parancsok, **szabályok** (spec először, `IAppClock` a „ma”-hoz, tulajdonjog-szűrés, séma csak migrációval, titok nem kerül a repóba), „kész” definíció. |
| [`CLAUDE.md`](../CLAUDE.md) | `@AGENTS.md` importálása + Claude Code-specifikus gépi tudnivalók (5433-as port, DLL-zárolás, heredoc). |
| [`/run-tests` skill](../.claude/skills/run-tests/SKILL.md) | Minden tesztszint futtatása, a kimenet mentése a `docs/test-results/` alá, a TESTING.md számainak frissítése. Tiltja, hogy egy piros tesztet „zöldre írjon”. |
| [`/verify-ac` skill](../.claude/skills/verify-ac/SKILL.md) | Egy AC ellenőrzése: spec-szöveg, hozzárendelt tesztek futtatása trait szűrővel, kézi lépések. A védés „mutasd be egy követelmény ellenőrzését” részéhez. |
| [`/change-request` skill](../.claude/skills/change-request/SKILL.md) | A 3. mérföldkő változtatási igényének menete a `v1.0` tagtől: hatáselemzés → spec → elbukó teszt → implementáció → adatmegőrző migráció → regresszió → dokumentáció. |
| [`spec-guardian` ügynök](../.claude/agents/spec-guardian.md) | Csak olvasó bíráló: kód, tesztek, doksi és spec összevetése AC-nként. |
| [`test-reviewer` ügynök](../.claude/agents/test-reviewer.md) | Csak olvasó bíráló: AC-nként normál, határ-, hibás és regressziós eset megléte, assertek minősége. |
| [`.github/workflows/ci.yml`](../.github/workflows/ci.yml) | Minden pushnál lefuttatja a backend és frontend teszteket, így az MI által írt kód ellenőrzése nem csak helyben történik. |

Az AGENTS.md és a CLAUDE.md szabályai a kódból származnak, nem általános tanácsok: mindegyik egy itt meghozott
döntést rögzít, amit egy későbbi MI-munkamenetnek (például a változtatási igénynél) be kell tartania.
A skilleket és ügynököket ez a munkamenet hozta létre; a használatukat a 3. mérföldkőnél (változtatási igény,
végső ellenőrzés) kell itt rögzíteni.

---

## 1. döntési eset – mi számít „közeledő” teendőnek? (követelményelemzés)
**Probléma.** A spec szerint a listán látszik, „mely gondozási feladatok esedékesek vagy közelednek”, és „ha a teendő
az aktuális napra esik, azt esedékesként jelöli”. Nincs megadva, mi a „közeledik”, mi történik a lekésett teendővel,
és milyen időzóna szerint számít a „mai nap”.

**Eszköz, kontextus.** Claude Code; kontextus a specifikáció PDF és a követelménykiírás (amely normál-, határ- és
hibás eseteket vár). Utasítás: „pont annyit valósíts meg, ahogyan a specifikációban le van írva … ha bármilyen
kérdés merül fel, kérdezz”.

**MI-javaslat.** Négy állapot: `Lejárt` (korábbi nap), `Esedékes` (ma), `Közeledik` (N napon belül), `Rendben`.
N-re három opció 1, 2 vagy 3 nap, ajánlott a 2. A „ma” a Europe/Budapest naptári nap legyen, ne a szerver UTC-napja.

**Döntés (a hallgatóé).** 2 napos ablak, az ajánlás szerint. A négy állapotot és az időzónát az MI javasolta, és a
specifikáció „Bizonytalanságok” részébe került.

**Indok.** A „lejárt” külön állapot nélkül egy elmaradt locsolás a „rendben” vagy az „esedékes” közé esne, ami
félrevezető. Az 1 nap túl rövid ahhoz, hogy előre tervezni lehessen, a 3 napnál a lista nagy része folyamatosan
„közeledik”. Az időzóna azért kell, mert Budapesten 00:30-kor UTC szerint még előző nap van, és az esedékesség pont
napra pontos szabály.

**Ellenőrzés.**
- [`CareSchedule`](../backend/src/PlantCare.Api/Domain/CareSchedule.cs): `UpcomingWindowDays = 2`, a szabály egyetlen helyen.
- [`CareScheduleTests`](../backend/tests/PlantCare.Tests/Unit/CareScheduleTests.cs): ma → Esedékes; −1 → Lejárt; +1, **+2** → Közeledik; **+3** → Rendben (mindkét határ oldala).
- [`AppClockTests`](../backend/tests/PlantCare.Tests/Unit/AppClockTests.cs): 22:30 UTC (október) = másnap Budapesten; téli időben 23:30 UTC a határ.
- [`PlantsApiTests.List_shows_due_overdue_upcoming_and_ok_tasks_most_urgent_first`](../backend/tests/PlantCare.Tests/Integration/PlantsApiTests.cs): mind a négy állapot a valódi API-n és DB-n.
- [BUG_DEMO.md](BUG_DEMO.md): a határeset-teszt elkapja az `<` → `<=` hibát, a többi AC3 teszt nem.

**Korlát.** Az ablak és az időzóna konfiguráció/konstans, a felhasználó nem állíthatja; több időzónás használatnál
felhasználónkénti időzóna kellene.

---

## 2. döntési eset – hol tároljuk a képeket? (tervezés)
**Probléma.** A képek a felismeréshez és a növekedés követéséhez kellenek (AC6, AC7), és AC8 szerint a törléskor
„az adatbázisból is törlődik a növény, minden róla tárolt információval együtt”.

**Eszköz, kontextus.** Claude Code; kontextus az AC7 és AC8 szövege, valamint a választott PostgreSQL.

**MI-javaslat.** Két opció: (a) PostgreSQL `bytea` oszlop, ajánlott; (b) fájlrendszer, az adatbázisban csak az útvonal.
(a) mellett szól, hogy a törlés egy tranzakcióban, `ON DELETE CASCADE`-del viszi a képeket is, nem maradhat árva fájl,
és a mentés is egyben van. (b) mellett szól, hogy nagy rendszerekben ez az elterjedt, és nem növeli az adatbázist.

**Döntés (a hallgatóé).** PostgreSQL `bytea`, az ajánlás szerint.

**Indok.** Kis alkalmazásnál a méret nem szűk keresztmetszet, a „minden információval együtt” törlés viszont
követelmény. Fájlrendszernél a DB-törlés és a fájltörlés nem lehet egy tranzakcióban, egy félbeszakadt törlés után
megmaradhatnának a képek.

**Ellenőrzés.**
- Migráció: [`InitialCreate`](../backend/src/PlantCare.Api/Data/Migrations/) – a `CareEvents` és `PlantPhotos` idegen kulcsa `onDelete: ReferentialAction.Cascade`.
- A törlés egyetlen `ExecuteDeleteAsync` ([`PlantsController.Delete`](../backend/src/PlantCare.Api/Controllers/PlantsController.cs)), tehát **csak** az adatbázis kaszkádja törölheti a gyerekeket. Ha a kaszkád hiányozna, a teszt FK-hibával bukna.
- [`PlantsApiTests.Deleted_plant_disappears_together_with_its_events_and_photos`](../backend/tests/PlantCare.Tests/Integration/PlantsApiTests.cs): a törlés után **közvetlen DB-lekérdezés**, 0 növény / esemény / kép; a másik növény képe megmarad.
- A listák és a részletes nézet csak metaadatot kérdeznek le (projekció), a képbájtokat külön végpont adja, így a lista nem tölt be MB-okat.
- Kézi: [TESTING.md – M3](TESTING.md#kézi-ellenőrzések).

**Korlát.** Az adatbázis mérete a képekkel nő; ezért van 10 MB-os korlát és formátumellenőrzés (magic byte).
Bélyegkép nincs, a lista a teljes képet tölti le.

---

## 3. döntési eset – valódi PostgreSQL vagy InMemory a tesztekben? (tesztelés)
**Probléma.** Az integrációs tesztekhez kell adatbázis. A lényeges szabályok egy része az adatbázisban él
(kaszkád törlés, CHECK constraint, egyedi e-mail index, migrációk).

**Eszköz, kontextus.** Claude Code; kontextus az AC5 és AC8, valamint a 2. döntés (a törlés az adatbázis kaszkádjára épül).

**MI-javaslat.** (a) Testcontainers + PostgreSQL, ajánlott: ugyanaz a motor és a migrációk futnak, mint élesben,
cserébe Docker kell. (b) EF Core InMemory: gyors, Docker nélkül fut, de nem ismeri a constraint-eket, a kaszkádot
és a migrációkat.

**Döntés (a hallgatóé).** Testcontainers + PostgreSQL, az ajánlás szerint.

**Indok.** Az InMemory szolgáltató az AC8 törlését és az AC5 adatbázis-szintű védelmét **hamisan zöldnek** mutatná:
nem futtat SQL-t, így a CHECK constraint és az `ON DELETE CASCADE` nem létezik benne, és az `ExecuteDeleteAsync`-t
sem támogatja.

**Ellenőrzés.**
- [`PlantsApiTests.Database_rejects_non_positive_interval_even_if_validation_is_bypassed`](../backend/tests/PlantCare.Tests/Integration/PlantsApiTests.cs): a validációt megkerülve, közvetlenül a DB-be írt 0 → `DbUpdateException` a `CK_Plants_WateringIntervalDays_Positive` constraint miatt. InMemoryval ez a teszt nem írható meg.
- [`MigrationTests`](../backend/tests/PlantCare.Tests/Integration/MigrationTests.cs): az összes migráció lefut, és a modellnek nincs migrálatlan változása. Ez a változtatási igény regressziós alapja.
- A teljes készlet futása: [test-results/backend.txt](test-results/backend.txt) (91/91).

**Mellékes MI-lelet a tesztinfrastruktúra tervezésekor.** A tesztek rögzített, hamis „mostot” használnak
(`FakeTimeProvider`, 2026-10-18). Az ügynök észrevette, hogy a JWT tokent eredetileg ugyanezzel az órával
állítottuk ki, a JWT-ellenőrzés viszont a valódi rendszerórát használja. A 24 órás élettartam miatt a tesztek
**másnaptól minden kérésre 401-et kaptak volna**. A hiba a megírás napján még nem jelentkezett, mert a hamis és a
valódi idő ugyanarra a napra esett. Javítás: a [`TokenService`](../backend/src/PlantCare.Api/Infrastructure/TokenService.cs)
szándékosan a valódi órát használja (megjegyzéssel); az üzleti „ma” továbbra is `IAppClock`.

**Korlát.** A tesztekhez futó Docker kell, és az első futás lassabb (image letöltés). A CI-ben (GitHub Actions,
ubuntu) a Docker elérhető.

---

## Az MI-használat korlátai, amiket tapasztaltunk
- **Külső API vakon.** API-kulcs nélkül a Pl@ntNet klienst csak stubbal lehetett tesztelni. Az ügynök a hivatalos
  dokumentációval egyeztette a végpontot, a paramétereket és a válasz mezőit; a „nincs találat” státuszkódot a
  dokumentáció nem rögzíti, ezért ez kézi ellenőrzés (M1).
- **Környezet.** A gépen egy korábbi, törölt PlantCare-verzió Docker volume-ja maradt meg, ezért az első migráció
  „relation already exists” hibával elbukott. Az ügynök nem törölte a régi adatot, új volume-nevet használt
  (`plantcare-pgdata`). A régi `plantcare_plantcare-data` volume kézzel törölhető.
- **Eszközhiba.** Git Bash alatt a hosszú heredoc-os fájlírás kétszer elakadt; ezért ír a CLAUDE.md a Write eszközről.
- **Felelősség.** Az MI javaslatait a tesztek futása igazolja, nem az, hogy az MI állítja. A kód és a
  dokumentáció helyességéért a hallgató felel, a védésen MI nélkül kell elmagyaráznia.
