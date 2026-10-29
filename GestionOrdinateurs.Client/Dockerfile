FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY GestionOrdinateurs.Client/GestionOrdinateurs.Client.csproj GestionOrdinateurs.Client/
RUN dotnet restore GestionOrdinateurs.Client/GestionOrdinateurs.Client.csproj

COPY GestionOrdinateurs.Client/ GestionOrdinateurs.Client/
RUN dotnet publish GestionOrdinateurs.Client/GestionOrdinateurs.Client.csproj -c Release -o /app/publish --no-restore

FROM nginx:alpine
COPY GestionOrdinateurs.Client/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html

EXPOSE 5000
