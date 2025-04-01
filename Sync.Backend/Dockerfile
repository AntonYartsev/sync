# Используем SDK для сборки проекта
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Копируем файлы проекта и восстанавливаем зависимости
COPY ["Sync.Backend.csproj", "./"]
RUN dotnet restore

# Копируем все остальные файлы и собираем проект
COPY . .
RUN dotnet publish -c Release -o /app/publish

# Создаем runtime образ
FROM --platform=$TARGETPLATFORM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Копируем опубликованные файлы
COPY --from=build /app/publish .

# Устанавливаем переменные окружения
ENV ASPNETCORE_URLS=http://+:80
ENV ASPNETCORE_ENVIRONMENT=Development

EXPOSE 80
ENTRYPOINT ["dotnet", "Sync.Backend.dll"] 