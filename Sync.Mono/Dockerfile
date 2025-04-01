FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Sync.Mono/Sync.Mono.csproj", "Sync.Mono/"]
RUN dotnet restore "Sync.Mono/Sync.Mono.csproj"
COPY . .
WORKDIR "/src/Sync.Mono"
RUN dotnet build "Sync.Mono.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Sync.Mono.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Sync.Mono.dll"] 