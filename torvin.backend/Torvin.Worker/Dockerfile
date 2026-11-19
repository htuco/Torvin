# Faza 1: kompajliranje sa SDK imageom
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Prvo samo projektni fajlovi, da Docker kešira restore dok se paketi ne promijene
COPY Torvin.slnx Directory.Build.props ./
COPY Torvin.API/Torvin.API.csproj Torvin.API/
COPY Torvin.Model/Torvin.Model.csproj Torvin.Model/
COPY Torvin.Services/Torvin.Services.csproj Torvin.Services/
COPY Torvin.Tests/Torvin.Tests.csproj Torvin.Tests/
COPY Torvin.Worker/Torvin.Worker.csproj Torvin.Worker/
RUN dotnet restore Torvin.slnx

COPY . .
RUN dotnet publish Torvin.Worker/Torvin.Worker.csproj -c Release --no-restore -o /app

# Faza 2: samo objavljeni fajlovi, bez kompajlera. Image je aspnet, ne runtime, jer Torvin.Services
# (dijele ga API i Worker) koristi ASP.NET Core biblioteke, pa ih Worker mora imati pri pokretanju
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
ENTRYPOINT ["dotnet", "Torvin.Worker.dll"]
