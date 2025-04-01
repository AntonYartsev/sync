# Используем SDK для сборки проекта
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Копируем файлы проекта и восстанавливаем зависимости
COPY ["Sync.Frontend.csproj", "./"]
RUN dotnet restore

# Копируем все остальные файлы и собираем проект
COPY . .
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

# Создаем runtime образ с Nginx
FROM nginx:alpine AS final
LABEL version="latest"
WORKDIR /usr/share/nginx/html

# Копируем опубликованные файлы
COPY --from=publish /app/publish/wwwroot .

# Создаем директорию для Bootstrap CSS
RUN mkdir -p /usr/share/nginx/html/css/bootstrap

# Копируем Bootstrap CSS в нужную директорию
COPY --from=build /app/publish/wwwroot/lib/bootstrap/dist/css/bootstrap.min.css /usr/share/nginx/html/css/bootstrap/

# Копируем конфигурацию Nginx
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Устанавливаем переменные окружения
ENV ASPNETCORE_ENVIRONMENT=Development

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]