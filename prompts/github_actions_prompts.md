GitHub Copilot Chat Előzmények - RentGrid Projekt (10. rész)

Ez a dokumentum a GitHub Copilot-tal folytatott beszélgetést tartalmazza

1. Beszélgetés: GitHub Actions CI munkafolyamat beállítása

Prompt

Frissítsd a GitHub Actions CI munkafolyamatot a RentGrid projekthez. A backend .NET 10, a frontend Angular 21 (Node.js v24.12.0). A tesztek a ./RentGrid.Api.Tests mappában vannak.

 GitHub Copilot

 Gondolatmenet (Thinking)

Egy robusztus CI csővezetéket (pipeline) kell létrehozni, amely párhuzamosan futtatja a backend és frontend ellenőrzéseket.

Trigger: push és pull_request a main ágra.

Backend Job: .NET SDK 10 telepítése, NuGet csomagok gyorstárazása (cache), build és a tesztek futtatása a konkrét projektfájlon keresztül.

Frontend Job: Node.js 24 telepítése, függőségek telepítése és a produkciós build tesztelése.

Docker Job: Opcionális ellenőrzés, hogy a docker-compose fájl szintaktikailag helyes-e.

 Válasz

Elkészítettem a .github/workflows/ci.yml konfigurációt, amely automatizálja a teljes ellenőrzési folyamatot minden módosításkor.

.github/workflows/ci.yml:

name: RentGrid CI

on:
  push:
    branches: [ main ]
  pull_request:
    branches: [ main ]

jobs:
  API-Build-and-Test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Cache NuGet packages
        uses: actions/cache@v4
        with:
          path: ~/.nuget/packages
          key: ${{ runner.os }}-nuget-${{ hashFiles('**/*.csproj') }}
          restore-keys: |
            ${{ runner.os }}-nuget-

      - name: Restore dependencies
        run: dotnet restore ./RentGrid.Api/RentGrid.Api.csproj

      - name: Build API
        run: dotnet build ./RentGrid.Api/RentGrid.Api.csproj --no-restore -c Release

      - name: Run Backend Tests
        run: dotnet test ./RentGrid.Api.Tests/RentGrid.Api.Tests.csproj --no-build -c Release

  Web-Build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: '24.12.0'
          cache: 'npm'
          cache-dependency-path: './RentGrid.Web/package-lock.json'

      - name: Install dependencies
        run: |
          cd RentGrid.Web
          npm ci

      - name: Lint and Build Frontend
        run: |
          cd RentGrid.Web
          npm run build -- --configuration=production


2. Beszélgetés: Docker Compose validáció a CI-ben

Prompt

Érdemes lenne a Docker Compose fájlt is ellenőrizni a CI folyamat során?

 GitHub Copilot

 Válasz

Igen, javasolt hozzáadni egy gyors ellenőrzést, amely megbizonyosodik róla, hogy a Docker konfiguráció érvényes, így elkerülhetőek a telepítési hibák.

  Docker-Check:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Validate Docker Compose
        run: docker compose config -q
