# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

恒隆面料管理系统 — an ASP.NET Core 10.0 MVC web app for managing fabric product images. Products are stored in MySQL (via Dapper), images in Aliyun OSS. The frontend is Vue 3 + TDesign, served as static assets from `wwwroot/`.

## Build & Run

```bash
# Build
dotnet build

# Run (Development)
dotnet run

# The app starts on HTTPS (default ASP.NET Core ports).
```

## Configuration & Secrets

Sensitive credentials (MySQL password, Aliyun OSS keys) are stored via `dotnet user-secrets`, **not** in `appsettings.json`. The `appsettings.json` files contain only placeholder values. To configure locally:

```bash
dotnet user-secrets set "ConnectionStrings:MySql" "<connection-string>"
dotnet user-secrets set "AliyunOss:EndPoint" "<oss-endpoint>"
dotnet user-secrets set "AliyunOss:AccessKey" "<access-key>"
dotnet user-secrets set "AliyunOss:AccessSecret" "<access-secret>"
dotnet user-secrets set "AliyunOss:BucketName" "<bucket-name>"
```

The `appsettings.*.local.json` pattern is gitignored.

There are no tests in this project.

## Architecture

### Backend (ASP.NET Core MVC)

- **`Program.cs`** — Host setup. Registers `IMySqlHelper`/`MySqlHelper` as scoped, MVC with controllers+views, health checks at `/healthz`, calls `EnsureTableCreated()` on startup, and maps the default route `{controller=Home}/{action=Index}/{id?}`.
- **`Controllers/HomeController.cs`** — Landing page (`/`).
- **`Controllers/ProductController.cs`** — Main CRUD controller at `/Product`:
  - `Index()` — serves the Vue SPA view.
  - `GetImgs(QueryVm, CancellationToken)` — paginated product list (POST). Clamps pageSize 1-100.
  - `ExportFile(CancellationToken)` — multi-file upload with server-side file type validation (.jpg/.jpeg/.png/.webp) and 20MB size limit. Reads image dimensions via ImageSharp, inserts DB row, uploads to OSS as public-read.
  - `GetImg(guid, CancellationToken)` — streams image from OSS.
  - `Update(UpdateVm)` — toggle `Status`. Validates guid is not empty.
  - `UpdateLevel(UpdateLevelVm, CancellationToken)` — update all product metadata. Validates guid.
  - `UpdateSize(UpdateSizeVm, CancellationToken)` — update width/height. Validates guid.
  - `Del(DelVm, CancellationToken)` — delete by guid. Validates guid.
- **`Controllers/ImageController.cs`** — API controller at `api/Image/Get`. Returns filtered (`Status=1`) images for external consumers. Supports pagination via `index`/`pageSize` query params (clamped 0-100).
- **`Common/IMySqlHelper.cs` + `Common/MySqlHelper.cs`** — Data access layer. Uses Dapper + MySqlConnector with parameterized queries (no string interpolation). All methods log exceptions via `ILogger<MySqlHelper>` instead of silently swallowing. `EnsureTableCreated()` runs at startup. Parameters use `offset`/`limit` naming. All async methods accept `CancellationToken`.
- **`Models/`** — ViewModels: `ImagesVm` (core product entity), `QueryVm`, `UpdateVm`, `UpdateLevelVm`, `UpdateSizeVm`, `DelVm`.

### Frontend (Vue 3 + TDesign)

- **`Views/Shared/_Layout.cshtml`** — Global layout with sticky header nav (首页 / 产品管理). Loads Vue 3, TDesign, and Axios from `wwwroot/lib/`.
- **`Views/Product/Index.cshtml`** — Product management SPA. Uses TDesign components throughout: `t-pagination` (top + bottom), `t-dialog` for edit form, `t-card` grid, `t-upload`, `t-image`, `t-switch`. Empty state shown when no data.
- **`Views/Home/Index.cshtml`** — Landing page with QR code and link to `/Product`.
- **`wwwroot/js/product.js`** — Vue 3 Composition API. All UI is declarative (no DOM manipulation). Manages product list, CRUD, edit dialog (reactive `editForm`), and pagination via `onPageChange`.

### External Dependencies

- **MySQL** — connection string from `dotnet user-secrets`. The `products` table is auto-created on startup via `EnsureTableCreated()`.
- **Aliyun OSS** — endpoint, keys, and bucket name from `dotnet user-secrets`. Uploaded images set to `PublicRead` ACL.

## Key Conventions

- C# namespace: `henglong.Web`
- Target framework: `net10.0` with `<ImplicitUsings>enable</ImplicitUsings>` and `<Nullable>enable</Nullable>`
- Data access via `IMySqlHelper` (scoped), constructor takes `IConfiguration` + `ILogger<MySqlHelper>`
- All async methods accept `CancellationToken` (defaults to `default` for backward compat)
- Dapper queries use `CommandDefinition` with parameterized values — never string interpolation
- API responses use Chinese strings (`"成功"`/`"失败"`) for success/failure messages
- Image filenames in OSS are GUIDs with `.jpg` extension
- `Level` field controls sort order (higher = first); `Status` controls visibility (1 = enabled)
- Health check endpoint: `GET /healthz`
