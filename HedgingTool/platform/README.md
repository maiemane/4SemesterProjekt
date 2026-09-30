# Hedging Tool Platform

Hedging Tool Platform er et eksamensprojekt udviklet med ASP.NET Core, React og TypeScript. Løsningen er bygget som et distribueret client-server system med separat frontend, separat API og ekstern Azure SQL Database.

Formålet med platformen er at danne grundlaget for et hedging-værktøj, hvor brugere senere kan arbejde med finansielle data, instrumenter, priser og portefølje-/hedgingfunktionalitet. På nuværende tidspunkt er der implementeret en grundlæggende autentifikationsløsning med JWT, så systemet har en sikker loginmekanisme, før de øvrige funktioner bygges videre.

## Arkitektur

Løsningen består af følgende projekter og mapper:

- `src/HedgingTool.Platform.Api` - backend API og ASP.NET Core host
- `src/HedgingTool.Platform.Application` - applikationslogik, use cases, DTOs og interfaces
- `src/HedgingTool.Platform.Domain` - domænemodeller
- `src/HedgingTool.Platform.Infrastructure` - konkrete implementationer til auth, databaseadgang og integrationer
- `web` - React/TypeScript frontend bygget med Vite

Arkitekturen følger samme overordnede lagdeling som det tidligere Slottet-projekt. Det betyder, at ansvar er delt op mellem API, application, domain og infrastructure.

Det ses blandt andet ved:

- interfaces i `src/HedgingTool.Platform.Application/Interfaces`
- use cases/services i `src/HedgingTool.Platform.Application/Services`
- konkrete repositories i `src/HedgingTool.Platform.Infrastructure/Repositories`
- domæneentiteter i `src/HedgingTool.Platform.Domain/Entities`
- API controllers i `src/HedgingTool.Platform.Api/Controllers`

Denne opdeling gør, at API’et ikke selv indeholder databaseforespørgsler eller passwordlogik. API’et modtager HTTP requests og sender arbejdet videre til application-laget. Application-laget beskriver, hvad systemet skal gøre, mens infrastructure-laget håndterer de konkrete tekniske detaljer.

## Teknologier

- .NET 10
- ASP.NET Core Web API
- React
- TypeScript
- Vite
- Microsoft SQL Server / Azure SQL Database
- JWT Bearer Authentication
- ASP.NET Core Identity PasswordHasher

## Systemtype

Løsningen er et distribueret **client-server system**.

Det betyder i denne løsning:

- klienten er frontend-applikationen i `web`
- serveren er backend-API’et i `src/HedgingTool.Platform.Api`
- databasen er en central ekstern ressource i Azure SQL

Frontend og backend køres separat under udvikling. Frontenden kører typisk på Vite development serveren, mens API’et kører som en ASP.NET Core applikation. Vite videresender API-kald til backend via proxy-konfigurationen i `web/vite.config.ts`.

## Systembeskrivelse

Hedging Tool Platform er udviklet som fundamentet for et finansielt værktøj, hvor brugere senere skal kunne arbejde med data som instrumenter, priser, NAV og datakilder. Databasen indeholder allerede centrale tabeller til finansielle data, blandt andet `instrument`, `daily_price`, `daily_nav` og `data_source`.

Autentifikation er implementeret som det første centrale sikkerhedslag. Brugeren logger ind via React-frontenden med email og password. Frontenden sender loginoplysningerne til API’et, som kontrollerer brugeren i Azure SQL-databasen. Hvis brugeren findes, er aktiv, og passwordet matcher det gemte password hash, udsteder API’et et JWT access token. Frontenden gemmer tokenet i browserens `sessionStorage` og viderestiller brugeren til en midlertidig tom hjemmeside.

Den nuværende hjemmeside er bevidst simpel og viser kun `Homepage`, fordi formålet på dette tidspunkt er at få loginflowet og sikkerhedsstrukturen på plads. Senere kan de egentlige hedging-funktioner bygges ind bag samme autentifikationslag.

## Autentifikation

Autentifikationen er baseret på samme principper som i det tidligere Slottet-projekt:

- brugeren logger ind med email og password
- passwords gemmes ikke i klartekst
- passwords verificeres med ASP.NET Core Identity `PasswordHasher`
- API’et udsteder JWT access tokens
- JWT valideres med issuer, audience, lifetime og signing key
- kun aktive brugere kan logge ind

Login-endpointet findes her:

- `POST /api/auth/login`

Controlleren ligger i:

- `src/HedgingTool.Platform.Api/Controllers/AuthController.cs`

Selve loginlogikken ligger i:

- `src/HedgingTool.Platform.Application/Services/Auth/LoginService.cs`

Den konkrete databaseadgang ligger i:

- `src/HedgingTool.Platform.Infrastructure/Repositories/UserRepository.cs`

JWT oprettes i:

- `src/HedgingTool.Platform.Infrastructure/Auth/JwtTokenGenerator.cs`

Password hashing og verification ligger i:

- `src/HedgingTool.Platform.Infrastructure/Auth/PasswordHashingService.cs`
- `src/HedgingTool.Platform.Infrastructure/Auth/PasswordVerificationService.cs`

## Loginflow

Loginflowet fungerer sådan:

1. Brugeren åbner React-frontenden.
2. Hvis brugeren ikke allerede har et gyldigt token i `sessionStorage`, vises login-siden.
3. Brugeren indtaster email og password.
4. Frontenden kalder `POST /api/auth/login`.
5. API’et kalder `LoginService`.
6. `LoginService` henter brugeren via `IUserRepository`.
7. `UserRepository` slår brugeren op i `dbo.users` i Azure SQL.
8. Hvis brugeren er aktiv, verificeres passwordet med `PasswordVerificationService`.
9. Hvis passwordet matcher, opretter `JwtTokenGenerator` et JWT token.
10. API’et returnerer token, udløbstidspunkt, navn, email og rolle.
11. Frontenden gemmer loginoplysningerne i `sessionStorage`.
12. Brugeren sendes til `/home`.

Hvis tokenet mangler eller er udløbet, fjernes det fra `sessionStorage`, og brugeren sendes tilbage til login.

## Databasetabel til brugere

Autentifikationen forventer, at databasen indeholder en tabel med navnet:

```sql
dbo.users
```

Tabellen skal som minimum have disse kolonner:

- `id` - unik bruger-id
- `email` - brugerens login-email
- `name` - brugerens navn
- `role` - brugerens rolle, for eksempel `Admin`
- `password_hash` - password hash lavet med ASP.NET Core Identity `PasswordHasher`
- `is_active` - angiver om brugeren må logge ind
- `created_at_utc` - tidspunkt for oprettelse
- `updated_at_utc` - tidspunkt for seneste ændring

Passwords må ikke gemmes i klartekst. Når en bruger oprettes manuelt i databasen, skal `password_hash` være et gyldigt ASP.NET Core Identity password hash. Det er dette hash, som API’et verificerer ved login.

Et eksempel på en bruger i databasen kan være:

```text
email: admin@nordic-cap.com
name: Admin User
role: Admin
is_active: 1
```

Passwordet til denne bruger skal kun kendes af udviklerne og må ikke skrives ind i Git.

## Konfiguration

API’et bruger konfiguration til databaseforbindelse, JWT og CORS.

De relevante settings er:

```json
{
  "ConnectionStrings": {
    "HedgingDb": ""
  },
  "Jwt": {
    "Issuer": "HedgingTool",
    "Audience": "HedgingTool.Web",
    "SigningKey": "",
    "ExpirationMinutes": 60
  },
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:5173",
      "https://localhost:5173"
    ]
  }
}
```

`ConnectionStrings:HedgingDb` skal være en ADO.NET connection string til Azure SQL. Den må ikke være en JDBC connection string, da JDBC kun bruges til Java-baserede klienter.

Eksempel på format:

```text
Server=tcp:<server>.database.windows.net,1433;Initial Catalog=<database>;User ID=<user>;Password=<password>;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

`Jwt:SigningKey` skal være en lang hemmelig nøgle, som bruges til at signere JWT tokens. Den skal holdes hemmelig og må ikke commits til Git. I production bør den sættes via environment variables, Azure App Settings eller en tilsvarende secret manager.

## Guide til lokal opsætning og kørsel

Denne guide viser, hvordan systemet køres lokalt under udvikling.

### Krav

- .NET 10 SDK
- Node.js og npm
- Adgang til Azure SQL Database
- En oprettet bruger i `dbo.users`

### 1) Klargør API-konfiguration

Der skal bruges lokal konfiguration til databaseforbindelse og JWT signing key.

Det kan sættes i `src/HedgingTool.Platform.Api/appsettings.Development.json`, i en lokal run configuration eller via environment variables. Hemmelige værdier som databasepassword og JWT signing key bør ikke commits til Git.

Eksempel på development-konfiguration uden rigtige secrets:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "HedgingDb": "Server=tcp:<server>.database.windows.net,1433;Initial Catalog=Hedging;User ID=<user>;Password=<password>;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
  },
  "Jwt": {
    "Issuer": "HedgingTool",
    "Audience": "HedgingTool.Web",
    "SigningKey": "<long-local-development-secret>",
    "ExpirationMinutes": 60
  }
}
```

### 2) Kør API’et

API’et kan køres fra IDE’et eller terminalen.

Fra terminalen i `platform`-mappen:

```bash
dotnet run --project src/HedgingTool.Platform.Api
```

Standardadressen i development er:

- API: `http://localhost:5259`

Hvis API’et logger denne besked, kører det korrekt:

```text
Now listening on: http://localhost:5259
```

En warning om HTTPS redirect kan forekomme lokalt, hvis der kun køres HTTP. Det er ikke i sig selv en loginfejl.

### 3) Kør frontenden

Åbn en anden terminal i `platform`-mappen og kør:

```bash
cd web
npm install
npm run dev
```

Standardadressen er:

- Web: `http://localhost:5173`

Frontenden kalder `/api/auth/login`. Under development videresender Vite dette kald til:

```text
http://localhost:5259
```

Proxyen er defineret i:

- `web/vite.config.ts`

### 4) Login

Når både API og frontend kører:

1. Åbn `http://localhost:5173`
2. Indtast email og password for en aktiv bruger i `dbo.users`
3. Ved korrekt login viderestilles brugeren til `/home`

Hvis login fejler, skyldes det typisk en af disse ting:

- API’et kører ikke på `http://localhost:5259`
- database connection string er forkert
- SQL-brugeren eller passwordet til Azure SQL er forkert
- brugeren findes ikke i `dbo.users`
- brugeren har `is_active = 0`
- `password_hash` er ikke et gyldigt ASP.NET Core Identity hash
- `Jwt:SigningKey` mangler

## Frontend

Frontenden ligger i `web` og er bygget med React, TypeScript og Vite.

De centrale filer for loginflowet er:

- `web/src/App.tsx` - login, session state og homepage routing
- `web/src/App.css` - styling af login og homepage
- `web/src/index.css` - globale styles og design tokens

Frontendens auth state gemmes i:

```text
sessionStorage
```

Det betyder, at login kun bevares for den aktuelle browser-session. Hvis browser-sessionen lukkes, eller tokenet er udløbet, skal brugeren logge ind igen.

## Backend

Backend ligger i `src/HedgingTool.Platform.Api`.

I `Program.cs` registreres:

- controllers
- CORS
- JWT Bearer authentication
- authorization
- application services
- infrastructure services
- repositories

JWT-valideringen kontrollerer:

- issuer
- audience
- signing key
- token lifetime

API’et har en fallback authorization policy, som kræver autentifikation som standard. Login-endpointet er markeret med `[AllowAnonymous]`, fordi brugeren netop skal kunne logge ind uden allerede at have et token.

## Sikkerhed

Der er taget udgangspunkt i samme sikkerhedsmodel som i det tidligere Slottet-projekt:

- passwords hashes med ASP.NET Core Identity PasswordHasher
- passwords gemmes ikke i klartekst
- login returnerer kun token og nødvendige brugeroplysninger
- JWT signeres med en hemmelig signing key
- signing key og databasepassword skal holdes ude af Git
- kun aktive brugere kan logge ind
- frontend gemmer token i `sessionStorage` og kontrollerer udløbstidspunkt

Ved videreudvikling bør følgende overvejes:

- refresh tokens eller kortere access token levetid
- rollebaseret autorisation på kommende API endpoints
- rate limiting på login-endpointet
- central secret management i Azure
- logging af sikkerhedsrelevante hændelser uden at logge passwords eller tokens

## Test og build

Frontend kan bygges med:

```bash
cd web
npm run build
```

.NET-løsningen kan bygges med:

```bash
dotnet build HedgingTool.Platform.sln
```

Når testprojekter bliver tilføjet, bør de kunne køres med:

```bash
dotnet test HedgingTool.Platform.sln
```