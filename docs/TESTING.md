# Tesztelés és ellenőrzés

## Tesztszintek
| Szint | Eszköz | Hol | Mit ellenőriz |
|---|---|---|---|
| Backend unit | xUnit | `backend/tests/PlantCare.Tests/Unit` | tiszta szabályok: esedékesség, validáció, képformátum, időzóna, Pl@ntNet válasz feldolgozása (HTTP stub) |
| Backend integráció | xUnit + `WebApplicationFactory` + **Testcontainers PostgreSQL** | `.../Integration` | a valódi API (kontrollerek, JWT, EF Core migrációk, constraint-ek, kaszkád törlés) valódi PostgreSQL-lel. Csak az óra (`FakeTimeProvider`, „ma” = 2026-10-18) és a külső felismerő API (`FakeRecognitionService`) hamisított |
| Frontend unit/komponens | Vitest + Testing Library | `frontend/src/**/*.test.*` | űrlapok, hibaüzenetek megjelenítése, javaslat elfogadása, státuszfeliratok (API mockolva) |
| E2E | Playwright (helyi Chrome) | `frontend/e2e` | a teljes fő forgatókönyv böngészőben, valódi backenddel és adatbázissal |
| Kézi | – | lent | valódi Pl@ntNet hívás, vizuális ellenőrzés |

A backend tesztek `[Trait("AC", "ACx")]` jelölést kapnak, így egy feltétel tesztjei külön futtathatók:
`dotnet test backend --filter "AC=AC5"`. A frontend tesztek nevében szerepel az AC-azonosító.

## Futtatás
```bash
docker compose up -d                 # PostgreSQL (E2E-hez és a kézi futtatáshoz)
dotnet test backend                  # unit + integráció (Docker kell, a Testcontainers saját konténert indít)
cd frontend
npm run lint && npm test && npm run build
npm run e2e                          # elindítja a backendet és a Vite szervert, ha még nem futnak
```

## Elfogadási feltétel → ellenőrzés
Jelölés: **N** = normál, **H** = határeset, **R** = hibás bemenet, **J** = jogosulatlan, **G** = regresszió.

| AC | Ellenőrzés | Típus |
|---|---|---|
| **AC1** Regisztráció, bejelentkezés | `AuthApiTests.Valid_registration_creates_account_and_user_can_log_in` | N |
| | `AuthInputValidatorTests.Password_length_boundary` (7 kar. elutasítva, 8 elfogadva) | H |
| | `AuthApiTests.Invalid_or_missing_registration_data_is_rejected_with_field_errors`, `Empty_body_is_rejected`, `AuthInputValidatorTests.Invalid_email_is_rejected` | R |
| | `AuthApiTests.Email_already_registered_is_rejected_case_insensitively`, `Wrong_password_is_rejected`, `Unknown_email_gets_the_same_error_as_wrong_password` | R |
| | `AuthApiTests.Protected_endpoints_require_a_valid_token` (nincs / hamis token → 401) | J |
| | `AuthPage.test.tsx` (hibaüzenet megjelenik, sikeres regisztráció után továbblép); E2E | N, R |
| **AC2** Növény létrehozása | `PlantsApiTests.Valid_plant_is_created_and_appears_in_own_list`; E2E | N |
| | `PlantInputValidatorTests.Name_length_boundary` (100/101), `Last_care_dates_today_are_accepted_but_future_dates_are_not` | H |
| | `PlantsApiTests.Missing_name_and_future_dates_are_rejected`, `PlantInputValidatorTests.Missing_name_is_rejected` | R |
| | `PlantsApiTests.Other_users_plant_is_invisible_and_cannot_be_changed` (lista, GET, esemény, kép, törlés → 404) | J |
| **AC3** Esedékesség | `CareScheduleTests.NextDue_is_last_done_plus_interval`, `NextDue_crosses_month_and_year_boundaries` | N |
| | `CareScheduleTests.Task_falling_on_today_is_due` (**pont ma**), `Task_within_window_is_upcoming` (1–2 nap), `Task_after_window_is_ok` (3 nap), `Task_before_today_is_overdue` | H |
| | `AppClockTests` (budapesti éjfél utáni idő UTC-ben még tegnap; nyári/téli idő) | H |
| | `PlantsApiTests.List_shows_due_overdue_upcoming_and_ok_tasks_most_urgent_first` (API + DB, sorrend) | N, H |
| | `PlantsPage.test.tsx` „AC3: shows due and approaching tasks”, `care.test.ts`; E2E („Esedékes”, „Közeledik”) | N |
| **AC4** Esemény rögzítése | `CareEventsApiTests.Watering_records_todays_event_and_moves_next_due_date`; `PlantsPage.test.tsx` „AC4”; E2E | N |
| | `CareEventsApiTests.Feeding_moves_only_the_feeding_schedule`, `History_lists_every_event_newest_first`, `CareScheduleTests.Recording_care_moves_last_done_and_adds_event` | N, G |
| | `CareEventsApiTests.Unknown_care_type_is_rejected` (ismeretlen típus, üres, nem JSON) | R |
| **AC5** Hibás gyakoriság | `PlantsApiTests.Zero_or_negative_interval_is_rejected_and_nothing_is_saved` (0, −3, 0, −1 → 400, a lista üres marad) | R |
| | `PlantInputValidatorTests.Interval_boundaries` (1 és 365 elfogadva, 366 elutasítva), `Zero_or_negative_*` | H |
| | `PlantsApiTests.Non_numeric_interval_is_rejected`, `Missing_interval_is_rejected` | R |
| | `PlantsApiTests.Database_rejects_non_positive_interval_even_if_validation_is_bypassed` (CHECK constraint) | R |
| | `AddPlantPage.test.tsx` „AC5”; E2E (0 → hibaüzenet a mező alatt) | R |
| **AC6** Növényfelismerés | `RecognitionApiTests.Valid_image_is_sent_to_the_recognition_service_and_suggestions_returned` | N |
| | `RecognitionApiTests.Invalid_file_is_rejected_without_calling_the_recognition_service`, `Missing_file_is_rejected`; `ImageValidatorTests` (szövegfájl, GIF, üres, 10 MB határ) | R, H |
| | `RecognitionApiTests.Unavailable_service_returns_503_with_a_manual_entry_hint`, `Nothing_recognised_returns_empty_suggestion_list` | R |
| | `PlantNetRecognitionServiceTests` (kérés formátuma, válasz feldolgozása, 404 → üres, 401/429/500 → nem elérhető, nincs kulcs → nincs hívás) | N, R |
| | `PlantsApiTests.Species_can_be_given_manually_or_left_empty_without_photo` | N |
| | `AddPlantPage.test.tsx` „AC6” ×3 (csak „Elfogadom” után töltődik ki, szerkeszthető marad; API kiesésekor kézzel megadható; hibás fájl) | N, R |
| | **Kézi: M1** (valódi Pl@ntNet) | N |
| **AC7** Növekedés követése | `PhotosApiTests.Photo_given_at_creation_is_the_first_growth_photo`, `Uploaded_photos_are_kept_in_chronological_order_and_retrievable` (bájtra azonos visszaolvasás); E2E | N |
| | `PhotosApiTests.Non_image_file_is_rejected_and_not_stored`, `Plant_with_invalid_photo_is_not_created` | R |
| | `PlantsApiTests.Other_users_plant_is_invisible_and_cannot_be_changed` (idegen kép → 404) | J |
| **AC8** Törlés | `PlantsApiTests.Deleted_plant_disappears_together_with_its_events_and_photos` (közvetlen DB-ellenőrzés: 0 növény/esemény/kép, a másik növény érintetlen); E2E | N |
| | `PlantsApiTests.Deleting_a_missing_plant_returns_404`; idegen növény törlése → 404 | R, J |
| | **Kézi: M3** (adatbázis ellenőrzése psql-lel) | N |
| Séma | `MigrationTests.All_migrations_are_applied_and_model_has_no_unmigrated_changes` | G |

### A kötelező esetek kiemelve
| Követelmény | Teszt |
|---|---|
| Normál eset | `PlantsApiTests.Valid_plant_is_created_and_appears_in_own_list`, `CareEventsApiTests.Watering_records_todays_event_and_moves_next_due_date` |
| 1. érdemi határeset | `CareScheduleTests.Task_falling_on_today_is_due` + `Task_within_window_is_upcoming(2)` / `Task_after_window_is_ok(3)` (a spec kifejezetten a mai napot nevezi meg) |
| 2. érdemi határeset | `PlantInputValidatorTests.Interval_boundaries` (1 / 365 / 366) és a 0 elutasítása |
| Hibás bemenet | `PlantsApiTests.Zero_or_negative_interval_is_rejected_and_nothing_is_saved` |
| Jogosulatlan művelet | `PlantsApiTests.Other_users_plant_is_invisible_and_cannot_be_changed`, `AuthApiTests.Protected_endpoints_require_a_valid_token` |
| Regresszió | `MigrationTests` (séma = modell), `CareEventsApiTests.Feeding_moves_only_the_feeding_schedule`, a teljes E2E forgatókönyv; a változtatási igénynél a teljes készlet a regressziós alap |
| Hibás változaton bukik, javítotton átmegy | [BUG_DEMO.md](BUG_DEMO.md): `Task_falling_on_today_is_due` és `List_shows_due_...` |

## Kézi ellenőrzések
**M1 – Valódi növényfelismerés (AC6).** Előfeltétel: Pl@ntNet API kulcs beállítva (lásd README).
1. Bejelentkezés után „Új növény”.
2. Válassz egy jól látható szobanövényről (pl. fikusz, monstera) készült JPEG képet.
3. Elvárt: rövid „Felismerés folyamatban…” után megjelenik a „Javasolt faj” panel tudományos névvel, magyar közönséges névvel (ha van) és százalékkal; a Faj mező üres marad.
4. „Elfogadom” → a Faj mezőben a javasolt név áll, átírható. Mentés után a növény oldalán ez a faj és a kép látszik.
5. Kulcs nélkül ugyanez: „A növényfelismerés jelenleg nem érhető el, add meg a fajt kézzel.” üzenet, a mentés kézzel megadott fajjal működik.

**M2 – Felület.** Asztali (1280 px) és mobil (375 px, DevTools) szélességen: sötét téma, nincs vízszintes görgetés; a státuszok színe: Lejárt = piros, Esedékes = sárga, Közeledik = kék, Rendben = szürke; a kártya bal szélén a legsürgősebb teendő színe.

**M3 – Törlés az adatbázisban (AC8).** Hozz létre növényt képpel, rögzíts locsolást, jegyezd fel az azonosítót az URL-ből, majd töröld. Utána:
```bash
docker exec plantcare-postgres psql -U plantcare -d plantcare -c \
 "select (select count(*) from \"Plants\" where \"Id\"='<id>') plants, (select count(*) from \"CareEvents\" where \"PlantId\"='<id>') events, (select count(*) from \"PlantPhotos\" where \"PlantId\"='<id>') photos;"
```
Elvárt: `0 | 0 | 0`.

## Legutóbbi futás
2026-10-18, Windows 11, .NET 10.0.401, Node 24.19, Docker 29.7. A kimenetek: [`test-results/`](test-results/).

| Szint | Eredmény |
|---|---|
| Backend (unit + integráció) | **91/91 sikeres** (64 tesztmetódus, a `Theory`-k esetenként számolva) – `backend.txt` |
| Frontend unit | **14/14 sikeres** (4 fájl) – `frontend-unit.txt` |
| Frontend lint / build | 0 figyelmeztetés / sikeres – `frontend-lint.txt`, `frontend-build.txt` |
| E2E | **1/1 sikeres** – `e2e.txt` |
| Hibás változat (AC3) | 2 bukott / 13 – `bug-demo/run-buggy.txt`; javított: 13/13 – `bug-demo/run-fixed.txt` |

## Korlátok
- A Pl@ntNet API-t az automatizált tesztek nem hívják (kulcs, kvóta, nem determinisztikus eredmény). A kliens kérésformátumát és válaszfeldolgozását HTTP stubbal teszteljük, a hivatalos dokumentációval egyeztetve; a „nincs találat” esetre a kód 404-et vár. Ezt a dokumentáció nem rögzíti, ezért **M1** kézi lépés ellenőrzi.
- A tesztképek csak a magic byte-okat tartalmazzák, nem dekódolható képek; a valódi megjelenítést az E2E egy 1×1-es PNG-vel ellenőrzi.
- Az integrációs tesztek rögzített „mai napot” használnak; az éjfél körüli viselkedést az `AppClockTests` fedi le.
