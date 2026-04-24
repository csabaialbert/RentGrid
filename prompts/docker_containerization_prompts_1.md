GitHub Copilot Chat Előzmények - RentGrid Projekt (9. rész)

Ez a dokumentum a GitHub Copilot-tal folytatott beszélgetést tartalmazza

1. Beszélgetés: Docker fájlok létrehozása

Prompt

Készíts Docker fájlokat a projekthez.

 GitHub Copilot

 Gondolatmenet (Thinking)

A projekt egy többrétegű alkalmazás (Multi-tier application), így külön Docker fájlokra van szükség a backend (ASP.NET Core) és a frontend (Angular) számára, valamint egy docker-compose.yml fájlra az egységes futtatáshoz.

Backend: .NET 10 SDK használata a buildeléshez és ASP.NET runtime a futtatáshoz.

Frontend: Node.js a build fázishoz és Nginx a statikus fájlok kiszolgálásához.

Összehangolás: Környezeti változók beállítása a MongoDB eléréséhez.

 Eszközhasználat

cat RentGrid.Api/RentGrid.Api.csproj: Célplatform ellenőrzése.

cat RentGrid.Web/package.json: Build scriptek ellenőrzése.

 Válasz

Elkészítettem a szükséges konfigurációkat a konténerizációhoz.

RentGrid.Api/Dockerfile:

# Build fázis
FROM [mcr.microsoft.com/dotnet/sdk:10.0](https://mcr.microsoft.com/dotnet/sdk:10.0) AS build
WORKDIR /src
COPY ["RentGrid.Api/RentGrid.Api.csproj", "RentGrid.Api/"]
RUN dotnet restore "RentGrid.Api/RentGrid.Api.csproj"
COPY . .
WORKDIR "/src/RentGrid.Api"
RUN dotnet build "RentGrid.Api.csproj" -c Release -o /app/build

# Publish fázis
FROM build AS publish
RUN dotnet publish "RentGrid.Api.csproj" -c Release -o /app/publish

# Futási fázis
FROM [mcr.microsoft.com/dotnet/aspnet:10.0](https://mcr.microsoft.com/dotnet/aspnet:10.0)
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RentGrid.Api.dll"]


2. Beszélgetés: Angular Dockerizálás

Prompt

Hogyan néz ki a frontend Docker fájlja, ha Nginx-et akarunk használni?

 GitHub Copilot

 Válasz

Az Angular alkalmazáshoz egy kétlépcsős (multi-stage) buildet alkalmazunk, ahol az első lépésben legyártjuk a dist mappát, a másodikban pedig átadjuk az Nginx-nek.

RentGrid.Web/Dockerfile:

# Build fázis
FROM node:20 AS build
WORKDIR /app
COPY package*.json ./
RUN npm install
COPY . .
RUN npm run build --configuration=production

# Kiszolgálás fázis
FROM nginx:alpine
COPY --from=build /app/dist/rent-grid-web /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]


3. Beszélgetés: Docker Compose összeállítás

Prompt

Készíts egy docker-compose.yml fájlt, ami elindítja a backendet, a frontendet és egy MongoDB adatbázist is.

 GitHub Copilot

 Válasz

A docker-compose fájl összefogja az összes szolgáltatást egy közös hálózatba.

docker-compose.yml:

services:
  mongodb:
    image: mongo:latest
    ports:
      - "27017:27017"
    volumes:
      - mongo-data:/data/db

  api:
    build:
      context: .
      dockerfile: RentGrid.Api/Dockerfile
    ports:
      - "5000:80"
    depends_on:
      - mongodb
    environment:
      - ConnectionStrings__MongoDb=mongodb://mongodb:27017/RentGrid

  web:
    build:
      context: .
      dockerfile: RentGrid.Web/Dockerfile
    ports:
      - "4200:80"
    depends_on:
      - api

volumes:
  mongo-data:

