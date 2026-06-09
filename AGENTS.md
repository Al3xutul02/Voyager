# AGENTS.md — Voyager

Context for AI agents working in this repo. Read this first. It captures the
decisions and gotchas that aren't obvious from the code alone — the goal is to
avoid re-introducing bugs we've already fixed.

## What this is

Voyager is a **Discord bot** built on **DSharpPlus**, hosted inside an
**ASP.NET Core Web API** (the API also exposes a few HTTP endpoints + Swagger).
It persists user data to **MySQL** via **EF Core (Pomelo provider)**. Solo hobby
project; single-instance deployment.

## Solution layout

| Project | SDK | Role |
|---|---|---|
| `Voyager.API` | `Microsoft.NET.Sdk.Web` | Entry point / composition root. Discord client setup, slash commands (`Commands/`), interaction handlers (`Events/`), view builders (`Views/`), HTTP controllers (`Controllers/`). |
| `BusinessLogic` | `Microsoft.NET.Sdk` | Services, DTOs, AutoMapper profile, domain enums, JSON models. |
| `Repository` | `Microsoft.NET.Sdk` | `VoyagerDbContext`, repositories, `QueryBuilder`, EF Core **migrations**, entity models. |
| `VoyagerTests` | `Microsoft.NET.Sdk` | Unit + integration tests in one project (`Unit/` and `Integration/` folders). |

Layering: `Voyager.API → BusinessLogic → Repository`. Keep the dependency
direction one-way.

## Critical version pin — do not bump

- **EF Core is pinned to `9.0.9` everywhere. Do NOT upgrade to 10.x.** Pomelo
  (`Pomelo.EntityFrameworkCore.MySql 9.0.0`) only supports EF Core 9. Mixing in
  EF Core 10 produces `NU1605` downgrade errors and `NU1107` version conflicts.
  The target framework is still `net10.0`; only the EF packages stay on 9.x.

Other notable deps: DSharpPlus `4.5.1`, AutoMapper `16.1.1` (Lucky Penny —
needs a license key, see Secrets), Newtonsoft.Json. Tests: MSTest `4.0.2`,
NSubstitute `5.3.0`, `Microsoft.EntityFrameworkCore.InMemory 9.0.9`,
`Testcontainers.MySql 4.12.0`.

## Conventions & decisions (the "why" — don't undo these)

### Discord / DSharpPlus
- **Slash commands can't inject scoped services directly.** DSharpPlus
  instantiates command modules from the root provider, which fails DI scope
  validation. Pattern: inject `IServiceScopeFactory`, open
  `await using var scope = _scopeFactory.CreateAsyncScope()` per interaction,
  resolve scoped services from it. The static `Events.EventHandler` gets the
  factory via `EventHandler.Initialize(...)` called once at startup.
- **Commands are thin adapters; logic lives in testable handlers.** Example:
  `Commands/Profile.cs` only translates the interaction into primitives and
  delegates to `Commands/Handlers/ProfileHandler.cs`, which takes a `ulong` and
  returns a `DiscordMessageBuilder`. Reason: `InteractionContext` has no public
  constructor and non-virtual methods — it cannot be mocked. New commands that
  need testing must follow this split.
- **Intents are intentionally minimal (`DiscordIntents.Guilds`).** It's a
  slash-command bot. Subscribing to message events triggered NREs inside
  DSharpPlus 4.5.1's `MESSAGE_CREATE`/`MESSAGE_UPDATE` handlers. Only widen
  intents when a feature actually needs them.
- **Reusable component emojis use raw unicode** (e.g. `new DiscordComponentEmoji("❌")`),
  not `DiscordEmoji.FromName(client, ...)`. This keeps `Views/` constructible in
  tests without a live `DiscordClient`.

### AutoMapper
- **For positional record DTOs, use `ForCtorParam`, not `ForMember`.**
  `ForMember` is silently ignored for record constructor parameters, which
  previously caused a raw JSON blob to be smuggled into a property. See
  `BusinessLogic/Mapper/MappingProfile.cs`.
- Maps are loaded from the BusinessLogic assembly:
  `cfg.AddMaps(typeof(MappingProfile).Assembly)`. Don't switch this to the
  executing assembly.
- `User.Settings` is a **JSON string column**; the profile (de)serializes it
  with `VoyagerJsonSettings.Default`.

### JSON
- **Never set `JsonConvert.DefaultSettings` globally.** DSharpPlus uses
  Newtonsoft internally with its own `[JsonProperty]` attributes; a global
  `CamelCasePropertyNamesContractResolver` collides with them (e.g. *"A member
  with the name 'components' already exists on DiscordActionRowComponent"*).
  Use `BusinessLogic/Json/VoyagerJsonSettings.Default` explicitly on each
  `JsonConvert` call instead.

### Colors / enums
- `EnumService` maps the domain `Color` enum ↔ `DiscordColor` and the
  `InteractionIdType` enum ↔ custom-id strings, backed by `FrozenDictionary`.
  The forward `Palette` is the single source of truth; the reverse lookup is
  derived from it. **Every `Color` enum value must have a `Palette` entry** (a
  unit test enforces this and the round-trip).

### Data access
- `AddDbContextPool<VoyagerDbContext>` is used, and `DbContext` (the base type
  `BaseRepository` depends on) is redirected to the pooled instance:
  `AddScoped<DbContext>(sp => sp.GetRequiredService<VoyagerDbContext>())`.
  Without that redirect the pool is bypassed.
- `User.Id` is the **Discord snowflake**, assigned by the app — configured
  `ValueGeneratedNever()` so EF doesn't treat it as an auto-increment column.

## Configuration & secrets

All secrets live in **dotnet user-secrets** (UserSecretsId is on
`Voyager.API`), never in the repo:
- `Discord:Token`
- `ConnectionStrings:VoyagerDbDevConnection`
- `AutoMapper:LicenseKey`

For EF tooling, the design-time factory reads `VOYAGER_DB_CONNECTION` from the
environment (see Migrations). `.gitignore` already excludes build output, local
DB files, and `*.user`. **Do not commit connection strings or data.**

## Build / test / run

```bash
# Build everything
dotnet build

# Unit tests only — fast, no Docker required
dotnet test --filter "TestCategory!=Integration"

# Integration tests — requires Docker (spins up a real MySQL container)
dotnet test --filter "TestCategory=Integration"

# Everything
dotnet test
```

Run the bot from `Voyager.API` (or via VS). It connects to Discord and applies
pending migrations on startup.

## Database & migrations

- Migrations live in **`Repository/Migrations/`** (same assembly as the
  `DbContext`). Baseline migration: `InitialCreate`.
- `dotnet-ef` is pinned as a **local tool** (`dotnet-tools.json`). Run via
  `dotnet dotnet-ef ...`.
- The bot **auto-applies migrations on startup** via `Database.Migrate()` in
  `Program.cs` (`ApplicationStarted`). This doubles as the DB connectivity
  check. Fine for single-instance; for multi-instance you'd apply migrations as
  a separate deploy step.
- `Repository/Context/VoyagerDbContextFactory.cs` is the design-time factory so
  EF tooling never boots the Discord-connecting `Program.cs`. Generating a
  migration needs no DB; applying one does — set `VOYAGER_DB_CONNECTION` first.

```bash
# Add a migration after a model change
dotnet dotnet-ef migrations add <Name> --project Repository --startup-project Repository

# Apply explicitly (bot also auto-applies on launch)
#   PowerShell: $env:VOYAGER_DB_CONNECTION = "server=...;database=...;user=...;password=..."
dotnet dotnet-ef database update --project Repository --startup-project Repository
```

Always **review the generated `Up()`/`Down()`** and commit migration files
(they are versioned schema, not data).

## Testing strategy

- **One project, two folders.** `Unit/` runs in parallel (method-level, see
  `MSTestSettings.cs`). `Integration/` is marked `[DoNotParallelize]` +
  `[TestCategory("Integration")]` and runs sequentially (all tests share one
  container and reset the same table).
- **Unit tests** use the EF Core in-memory provider and NSubstitute mocks.
  Note: in-memory does **not** validate relational schema/SQL translation — it's
  for logic, not schema.
- **Integration tests** use Testcontainers MySQL and apply real migrations
  (`MigrateAsync`), so they validate the migration + Pomelo SQL translation +
  `ulong`/`text` column behavior. The container starts lazily in
  `[ClassInitialize]`, so **unit-only runs never need Docker**.

## Known quirks / tech debt (don't "fix" without asking)

- `Program.DiscordClient` static handle is currently unused (its only consumer
  was refactored away) but intentionally kept.

## Working agreement

- Keep this file current when a convention or major decision changes.
- Prefer the established patterns above over introducing new ones.
- When build/tests are involved, verify with `dotnet build` / `dotnet test` and
  report real results.
