# Użycie obrazu .NET SDK jako bazy do kompilacji aplikacji
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /app

# Kopiowanie pliku .csproj i przywrócenie zależności
COPY CourierApp.csproj ./
RUN dotnet restore

# Kopiowanie reszty plików projektu
COPY . ./

# Kompilacja projektu w trybie produkcyjnym
RUN dotnet publish -c Release -o out

# Użycie obrazu runtime do uruchomienia aplikacji
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS runtime
WORKDIR /app
COPY --from=build /app/out .

# Uruchomienie aplikacji
ENTRYPOINT ["dotnet", "CourierApp.dll"]
