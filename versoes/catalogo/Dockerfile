FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY DropMonAPI/DropMonAPI.csproj DropMonAPI/
RUN dotnet restore DropMonAPI/DropMonAPI.csproj

COPY DropMonAPI/ DropMonAPI/
RUN dotnet publish DropMonAPI/DropMonAPI.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# O Render encaminha o tráfego para a porta 10000 por padrão.
ENV ASPNETCORE_HTTP_PORTS=10000
# Aplica migrations e insere a demonstração somente quando o banco configurado está vazio.
ENV Database__MigrateOnStartup=true
ENV Demo__Seed=true
ENV Admin__RequireAuthentication=true
EXPOSE 10000

ENTRYPOINT ["dotnet", "DropMonAPI.dll"]
