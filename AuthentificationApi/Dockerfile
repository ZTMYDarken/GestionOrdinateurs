FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY AuthentificationApi/AuthentificationApi.csproj AuthentificationApi/
COPY GestionOrdinateurs.Shared/GestionOrdinateurs.Shared.csproj GestionOrdinateurs.Shared/
RUN dotnet restore AuthentificationApi/AuthentificationApi.csproj

COPY AuthentificationApi/ AuthentificationApi/
COPY GestionOrdinateurs.Shared/ GestionOrdinateurs.Shared/
RUN dotnet publish AuthentificationApi/AuthentificationApi.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
ENV Services__GestionOrdinateursApi=http://gestionordinateurs-api:8080/
EXPOSE 8080

ENTRYPOINT ["dotnet", "AuthentificationApi.dll"]
