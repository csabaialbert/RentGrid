# RentGrid

![.NET](https://img.shields.io/badge/.NET-10-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-Backend-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![Entity Framework](https://img.shields.io/badge/Entity%20Framework-ORM-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)

![Angular](https://img.shields.io/badge/Angular-21-DD0031?style=for-the-badge&logo=angular&logoColor=white)
![TypeScript](https://img.shields.io/badge/TypeScript-Frontend-3178C6?style=for-the-badge&logo=typescript&logoColor=white)

![SQL Server](https://img.shields.io/badge/SQL%20Server-Database-CC2927?style=for-the-badge&logo=microsoftsqlserver&logoColor=white)
![MongoDB](https://img.shields.io/badge/MongoDB-GridFS-47A248?style=for-the-badge&logo=mongodb&logoColor=white)

![Docker](https://img.shields.io/badge/Docker-Containerization-2496ED?style=for-the-badge&logo=docker&logoColor=white)

![Prometheus](https://img.shields.io/badge/Prometheus-Monitoring-E6522C?style=for-the-badge&logo=prometheus&logoColor=white)
![Grafana](https://img.shields.io/badge/Grafana-Visualization-F46800?style=for-the-badge&logo=grafana&logoColor=white)
![OpenTelemetry](https://img.shields.io/badge/OpenTelemetry-Observability-000000?style=for-the-badge&logo=opentelemetry&logoColor=white)

A **RentGrid** egy full-stack autókölcsönző menedzsment rendszer, amely egy MSc kurzusfeladat részeként készült.  
Bemutatja egy teljes webalapú rendszer megtervezését és megvalósítását, beleértve a backend szolgáltatásokat, a frontend alkalmazást, az adatbázis-integrációt és a rendszer megfigyelhetőségét.

---

## Akadémiai háttér

Ez a projekt egy egyetemi feladat követelményeinek teljesítésére készült, amely egy teljes webes rendszer felépítésére fókuszált.

### Teljesített követelmények

* Full-stack webalkalmazás (frontend + backend)
* RESTful API CRUD műveletekkel
* Hitelesítés és felhasználói regisztráció
* Munkamenet-kezelés (JWT alapú)
* Adatbázis-integráció több entitással és kapcsolatokkal
* Webes felhasználói felület (GUI)
* Szoftverdokumentáció (`/docs` mappa)
* AI használat és prompt elemzés (`/prompts` mappa)

---

## Funkciók

* JWT alapú hitelesítés és jogosultságkezelés  
* Felhasználói regisztráció és menedzsment  
* Járműkezelés (CRUD műveletek)  
* Foglalási rendszer  
* Képtárolás MongoDB GridFS segítségével  
* API monitorozás Prometheus és Grafana használatával  
* RESTful API architektúra  

---

## Technológiai stack

### Backend

* ASP.NET Core (.NET 10)
* Entity Framework Core
* SQL Server
* MongoDB (GridFS)

### Frontend

* Angular (v21)

### Megfigyelhetőség és DevOps

* Docker
* Prometheus
* Grafana
* OpenTelemetry

---

## Követelmények

### Backend

* .NET 10

### Frontend

* Node.js 20+
* Angular 21

### Adatbázisok

* SQL Server
* MongoDB

### Opcionális

* Docker

---

## Projekt struktúra

```plaintext
RentGrid/
├── RentGrid.Api/           # ASP.NET Core backend
├── RentGrid.Web/           # Angular frontend
├── RentGrid.Api.Tests/     # Tests
├── docs/                   # Documentation (assignment requirement)
├── prompts/                # AI usage analysis (assignment requirement)
├── docker-compose.yml      # Containerized setup
```
---

## Első lépések

### 1. lehetőség – Docker (ajánlott)

```bash
docker-compose up --build
```

Ez elindítja:

* Backend API  
* Frontend alkalmazás  
* Prometheus  
* Grafana  

---

### 2. lehetőség – Manuális beállítás

#### Backend

```bash
cd RentGrid.Api
dotnet restore
dotnet run
```

#### Frontend

```bash
cd RentGrid.Web
npm install
ng serve
```

---

## Hitelesítés

A rendszer **JWT Bearer alapú hitelesítést** használ.  
Csak hitelesített felhasználók végezhetnek CRUD műveleteket, a feladat követelményeinek megfelelően.

---

## Monitorozás

Az alkalmazás **megfigyelhetőségi (observability) funkciókat** tartalmaz az alábbi eszközökkel:

* OpenTelemetry – instrumentációhoz  
* Prometheus – metrikák gyűjtéséhez  
* Grafana – vizualizációhoz  

Ezek az eszközök betekintést nyújtanak:

* kérés késleltetés (latency)  
* hibaarányok  
* végpont használat  

---

## Képernyőképek

### API monitorozás (Grafana)

![Grafana Dashboard](docs/images/grafana-dashboard.png)

---

## Dokumentáció

* Részletes rendszerdokumentáció a `/docs` mappában található  
* Az AI használat és prompt elemzés a `/prompts` mappában érhető el  

---

## Technológiai döntések

A választott technológiák az alábbi szempontok alapján kerültek kiválasztásra:

* **ASP.NET Core** – robusztus és skálázható backend keretrendszer  
* **Angular** – strukturált frontend a jól karbantartható UI fejlesztéshez  
* **SQL Server** – megbízható relációs adattárolás  
* **MongoDB (GridFS)** – hatékony bináris adatkezelés (képek)  
* **Docker** – egyszerűsíti a környezet beállítását és biztosítja a konzisztenciát  
* **Prometheus & Grafana** – produkciós szintű monitorozást és diagnosztikát tesz lehetővé  

---

## További megjegyzések

Bár a projekt teljesíti az összes akadémiai követelményt, további funkciókat is tartalmaz (pl. monitorozás és konténerizáció), amelyek egy produkció-orientált fejlesztési megközelítést demonstrálnak.

---

## Szerző

Készítette: **Csabai Albert**
