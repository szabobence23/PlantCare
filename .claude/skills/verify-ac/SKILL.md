---
name: verify-ac
description: Verify one PlantCare acceptance criterion (AC1–AC8) end to end - find its mapped tests in docs/TESTING.md, run them by trait filter, and walk through the manual check steps. Use when asked to prove or demo a requirement (e.g. for the defense).
argument-hint: "<AC azonosító, pl. AC3>"
---

# Elfogadási feltétel ellenőrzése: $ARGUMENTS

1. Olvasd el a feltétel szövegét a `SPECIFICATION.md`-ből, és mondd el egy mondatban, mi az elvárt
   megfigyelhető eredmény.
2. Keresd meg a `docs/TESTING.md` mátrixában az ehhez rendelt teszteket.
3. Futtasd őket:
   - backend: `dotnet test backend --filter "AC=$ARGUMENTS"`
   - frontend: `cd frontend && npx vitest run -t "$ARGUMENTS"`
4. Ellenőrizd, hogy a lefedés teljes-e: **normál eset, határeset, hibás bemenet** (és ha releváns,
   jogosulatlan hozzáférés). Ha valamelyik hiányzik, mondd meg, melyik, és javasolj tesztet – ne írd meg
   kérés nélkül.
5. Ha a mátrix kézi lépést is ír, sorold fel a lépéseket és az elvárt eredményt, hogy a felhasználó
   élőben megmutathassa.

## Kimenet
| Eset | Teszt | Eredmény |
|---|---|---|
Utána egy mondat: teljesül-e a feltétel, és mi a bizonyíték.
