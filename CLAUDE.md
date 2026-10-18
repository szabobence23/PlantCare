@AGENTS.md

## Claude Code-specifikus megjegyzések
- Windows gépen fejlesztünk: a 5432-es portot egy másik projekt PostgreSQL-je foglalja, ezért a PlantCare DB a **5433**-on fut. Ne állítsd le a `text-to-sql-postgres` konténert.
- A futó backend (`dotnet run`) zárolja a `PlantCare.Api.dll`-t: tesztfuttatás vagy build előtt állítsd le.
- Hosszú fájlokat a Write eszközzel írj, ne bash heredoc-kal (Git Bash alatt a hosszú heredocok elakadtak).
