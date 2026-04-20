# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

Multi-tenant data management platform with an Angular 20 frontend (`App.Web/`) and a .NET 10 Azure Functions backend (`App.Server/`). The core feature is a configurable grid system that supports multiple data sources (in-memory, Excel, Azure Data Lake, Cosmos DB, schema-driven).

## Commands

### Frontend (App.Web/)
```bash
npm start          # Dev server on port 4200
ng build           # Production build
ng build --localize  # Build with i18n bundles (en/de)
ng test            # Karma/Jasmine tests
ng lint            # ESLint
ng extract-i18n --format=json  # Extract translation strings
```

### Backend (App.Server/)
```bash
dotnet build       # Compile
dotnet run         # Dev server on port 7138
dotnet publish ./App.Server.csproj  # Release build
```

### VS Code
- F5 launches "App.Server" (.NET CoreCLR) or "App.Web" (Chrome with Angular dev server)
- `.vscode/launch.json` defines both configs

## Architecture

### Request/Response Flow
1. Frontend sends `RequestDto` (`commandName` + typed params) via `ServerApi` service
2. `Function.cs` → `UtilServer.Run()` handles session cookies, version checking, caching
3. `Generate.cs` / `ServerApi.cs` dispatches to command handler by `CommandName`
4. Command returns data into `ResponseDto` (result + notifications + navigation instruction)
5. Frontend updates Angular signals, re-renders

All API traffic goes through a single endpoint: `POST /api/data`.

### Backend (App.Server/App/)
- **`Function.cs`** — Two Azure Functions: `RunData` (HTTP trigger, main API) and `RunTrigger` (timer, warm-up every minute)
- **`UtilServer.cs`** — Core request handler: session resolution, JSON config, version enforcement
- **`Generate.cs`** — Command dispatcher (`switch` on `CommandName`)
- **`Command/`** — One file per command group: `CommandGrid`, `CommandUser`, `CommandStorage`, `CommandOrganisation`, `CommandTree`, `CommandVersion`, `CommandDebug`
- **`Sevice/`** (folder name has a typo, do not rename) — Services:
  - `Configuration.cs` — App config, connection strings, dev-mode flags
  - `CommandContext.cs` — Per-request context (session, org, notifications)
  - `CosmosDb.cs` / `CosmosDbCache.cs` — Cosmos DB TableAPI access and cache
  - `TableStorage.cs` — Azure Table Storage
  - `Storage.cs` — Azure Data Lake (hierarchical namespace)
  - `Cache.cs` — In-memory cache (Redis is a TODO)
  - `Grid/` — Grid engine implementations (see below)

### Grid System (App.Server/App/Sevice/Grid/)
The grid is the core domain abstraction. Each grid type implements a different storage backend:
- `GridBase.cs` — Shared interface
- `GridMemory.cs` — In-memory data
- `GridExcel.cs` — Excel file import/export (`DocumentFormat.OpenXml`)
- `GridStorage.cs` — File blobs in Azure Data Lake
- `GridArticle.cs` — CMS-style content
- `GridOrganisation.cs` — Multi-tenant org records
- `GridSchema.cs` — Schema-driven dynamic tables
- `UtilGrid.cs` (63KB) — Sorting, filtering, patching, cell type handling — the central utility for all grid operations

### Frontend (App.Web/src/app/)
- **`server-api.ts`** — Single HTTP service, all backend calls
- **`data.service.ts`** — App-wide state (user session, storage downloads, cache) using Angular signals
- **`notification.service.ts`** — Toast/notification management
- **`generate.ts`** — DTOs mirroring backend: `RequestDto`, `ResponseDto`, `GridRequestDto`, `GridResponseDto`, polymorphic `ComponentDto` variants
- **`app.routes.ts`** — 13 routes with lazy loading: auth pages, grid pages (Article, Storage, Product, Schema, Organisation), Home, About, Debug
- **`page-grid/`** — Reusable grid component used by all data pages

### Data Models
Both frontend (`generate.ts`) and backend (`Dto.cs`, `ComponentDto.cs`) share a mirrored DTO structure. The `ComponentDto` hierarchy is a polymorphic UI component tree used for server-driven UI rendering.

### Multi-Language
- English (`messages.json`) is the source; German (`messages.de.json`) is the localization
- `ng build --localize` produces separate bundles per locale
- Angular's `@angular/localize` (compile-time, not runtime)

### Authentication & Session
- Production: HttpOnly session cookies
- Development (Codespaces): `localStorage`-based session IDs (toggled by dev-mode config)
- Sessions and user records stored in Cosmos DB
- Multi-tenant: each session carries `OrganisationName`

### Azure Infrastructure
- Azure Functions v4 (isolated worker model)
- Cosmos DB (Table API) for documents and session cache
- Azure Table Storage for structured data
- Azure Data Lake (HNS enabled) for file storage

## Key Conventions
- `local.settings.json` holds dev connection strings (not committed with secrets); use `secrets.json` (user secrets) for local overrides
- The `Sevice/` folder name is intentionally left as-is (typo in original)
- `UtilGrid.cs` is the largest single file (~63KB); changes there affect all grid types
- Version mismatch between frontend and backend forces a page reload (version is embedded in responses)
