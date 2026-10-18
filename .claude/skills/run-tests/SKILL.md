---
name: run-tests
description: Run every PlantCare test level (backend unit+integration, frontend unit, lint, build, E2E) and record the results in docs/test-results. Use before a commit, a milestone submission, or when asked whether the tests pass.
---

# Összes teszt futtatása és rögzítése

A cél, hogy a beadásnál a `docs/TESTING.md` számai **valódi, mentett futásból** származzanak.

## Lépések
1. Előfeltételek:
   - `docker compose up -d --wait` a repó gyökerében (PostgreSQL a 5433-on; a Testcontainers külön konténert indít).
   - Ha fut `dotnet run` a backenddel, állítsd le (zárolja a DLL-t).
2. Backend (unit + integráció):
   ```
   dotnet test backend --logger "console;verbosity=normal" > docs/test-results/backend.txt 2>&1
   ```
3. Frontend:
   ```
   cd frontend
   npm run lint  > ../docs/test-results/frontend-lint.txt 2>&1
   npm test      > ../docs/test-results/frontend-unit.txt 2>&1
   npm run build > ../docs/test-results/frontend-build.txt 2>&1
   npm run e2e   > ../docs/test-results/e2e.txt 2>&1
   ```
4. Olvasd ki az összesítő sorokat (`Passed!`/`Failed:`, `Tests  N passed`, `N passed`), és frissítsd a
   `docs/TESTING.md` „Legutóbbi futás” táblázatát (dátum, darabszám, eredmény).
5. Ha bármi piros: **ne írd át a tesztet, hogy zöld legyen.** Derítsd ki, a kód vagy a teszt hibás-e,
   és ezt jelezd a felhasználónak a hibaüzenettel együtt.

## Kimenet
Röviden: tesztszintenként darabszám és eredmény, plusz a bukott tesztek neve és oka.
