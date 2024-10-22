# Użycie obrazu z .NET Framework 4.7.2 na Windows Server
FROM mcr.microsoft.com/dotnet/framework/runtime:4.7.2-windowsservercore-ltsc2019

WORKDIR /app
COPY . /app

# Uruchomienie aplikacji .NET Framework
ENTRYPOINT ["C:\\Windows\\Microsoft.NET\\Framework64\\v4.0.30319\\aspnet_compiler.exe", "-v", "/app/BackendApp.dll"]
