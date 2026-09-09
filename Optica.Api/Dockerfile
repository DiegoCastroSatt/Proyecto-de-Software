# Etapa 1: compilación
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

# Copiamos primero el proyecto y restauramos dependencias
COPY Optica.Api.csproj ./
RUN dotnet restore

# Copiamos el resto del código
COPY . ./

# Compilamos y publicamos
RUN dotnet publish -c Release -o /app/publish


# Etapa 2: ejecución
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "Optica.Api.dll"]
