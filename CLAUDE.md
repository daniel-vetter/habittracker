# HabitTracker — Konventionen

Stack: .NET Aspire (AppHost) orchestriert ASP.NET Core Web API (Controller, EF Core/Postgres) + Angular SPA mit PrimeNG. Der NSwag-TypeScript-Client wird bei jedem Debug-Backend-Build nach `src/frontend/src/app/server.ts` generiert.

## Backend / .NET

* EF Core Migrationen: nur den Up-Step behalten (Down entfernen). Namen in normaler Schreibweise, z.B. `"Added habit table"` statt `AddedHabitTable`.
* Neue Migration anlegen: `dotnet ef migrations add "..." --output-dir Database/Migrations` (im Projekt `HabitTracker.WebApp`).
* Bevorzuge `ImmutableArray` statt `Array`/`List` in Controller-Responses.
* `ImmutableArray` mit `.ToImmutableArray()` erzeugen, **nicht** mit Collection-Expression-Spread (`[.. x]`). Leere `[]` ist ok.
* Wenn ein Service ein zugehöriges Interface hat (z.B. `IHabitService` + `HabitService`), beides in **eine** Datei schreiben, benannt nach der Implementierung (`HabitService.cs`, ohne `I`-Prefix).
* Keine statischen Methoden auf Services aufrufen — immer über DI injizieren und Instanzmethoden verwenden.
* Methoden **nicht** mit `Async`-Suffix benennen, auch wenn sie `async`/`Task` zurückgeben (z.B. `GetReview()` statt `GetReviewAsync()`). Gilt für eigene Methoden; Framework-Methoden (`SaveChangesAsync`, `ToListAsync`, `MigrateAsync`) behalten ihren Namen.
* Keine `CancellationToken`-Parameter durchreichen — die DB-Calls sind kurzlebig. EF-Methoden ohne `ct` aufrufen (`ToListAsync()`, `SaveChangesAsync()` etc.).
* Nach Änderungen immer `dotnet build --no-incremental` verwenden, um Warnings zu prüfen (inkrementelle Builds verschlucken Warnings).
* Feature-Folder-Konvention: Controller + DTOs nahe der UI-Seite, die sie bedienen, unter `Features/Ui/<Feature>/`; Domänen-Services unter `Features/Core/<Domain>/`.
* DTO-Benennung: eingehende Request-Bodies enden auf `Request` (z.B. `HabitRequest`, `ToggleRequest`), zurückgegebene DTOs auf `Response` (z.B. `HabitResponse`, `ReviewResponse`) — **kein** `Input`/`Dto`-Suffix.

## Frontend / TypeScript

* Kein `null` verwenden, immer `undefined` (z.B. `private foo: string | undefined;` oder `foo?: string`). `null` nur akzeptieren, wo es von außen reinkommt (JSON-APIs, DOM), und am Boundary auf `undefined` mappen.
* Generierte API-Clients aus `server.ts` importieren (sie sind `providedIn: 'root'`, also direkt injizierbar).

## Infrastruktur

* Wenn ein Befehl Docker braucht (z.B. Postgres-Container, Tests mit Testcontainers) und der Daemon nicht läuft: Docker Desktop selbst starten und `docker info` pollen, bis bereit, dann den Befehl erneut ausführen — nicht den User bitten.

## Git

* **Kein** `Co-Authored-By` oder sonstiges AI-Branding in Commits.
* Niemals ohne explizite Ansage committen oder pushen. „Fix das" ≠ Commit-Erlaubnis. „Einchecken"/„commit" ist das Signal zum Committen, „pushen"/„push" zum Pushen — getrennt voneinander, und gilt nur für den aktuell anstehenden Commit/Push.
* Wenn ein Commit explizit angefordert wird, nur die genannten Änderungen committen — keine unrelated WIP-Drift mitnehmen. Im Zweifel nachfragen.
