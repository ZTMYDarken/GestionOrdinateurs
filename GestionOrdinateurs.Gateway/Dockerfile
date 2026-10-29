FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY GestionOrdinateurs.Gateway/GestionOrdinateurs.Gateway.csproj GestionOrdinateurs.Gateway/
RUN dotnet restore GestionOrdinateurs.Gateway/GestionOrdinateurs.Gateway.csproj

COPY GestionOrdinateurs.Gateway/ GestionOrdinateurs.Gateway/
RUN dotnet publish GestionOrdinateurs.Gateway/GestionOrdinateurs.Gateway.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
ENV ReverseProxy__Clusters__gestionOrdinateursApi__Destinations__gestionOrdinateursApi__Address=http://gestionordinateurs-api:8080/
ENV ReverseProxy__Clusters__authentificationApi__Destinations__authentificationApi__Address=http://authentificationapi:8080/
EXPOSE 8080

ENTRYPOINT ["dotnet", "GestionOrdinateurs.Gateway.dll"]
