FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

COPY . /app

ENTRYPOINT ["dotnet", "Media_microservice.dll"]