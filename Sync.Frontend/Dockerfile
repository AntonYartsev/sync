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

# Промежуточный этап для загрузки библиотек highlight.js
FROM alpine:latest AS highlight
WORKDIR /download

# Устанавливаем необходимые инструменты
RUN apk add --no-cache curl tar

# Создаем директории для highlight.js
RUN mkdir -p /download/highlight/languages

# Скачиваем highlight.js и стили
RUN curl -L https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.8.0/highlight.min.js -o /download/highlight/highlight.min.js
RUN curl -L https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.8.0/styles/atom-one-dark.min.css -o /download/highlight/atom-one-dark.min.css

# Скачиваем языковые пакеты для highlight.js
RUN curl -L https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.8.0/languages/javascript.min.js -o /download/highlight/languages/javascript.min.js
RUN curl -L https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.8.0/languages/typescript.min.js -o /download/highlight/languages/typescript.min.js
RUN curl -L https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.8.0/languages/csharp.min.js -o /download/highlight/languages/csharp.min.js
RUN curl -L https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.8.0/languages/python.min.js -o /download/highlight/languages/python.min.js
RUN curl -L https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.8.0/languages/java.min.js -o /download/highlight/languages/java.min.js
RUN curl -L https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.8.0/languages/css.min.js -o /download/highlight/languages/css.min.js
RUN curl -L https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.8.0/languages/html.min.js -o /download/highlight/languages/html.min.js
RUN curl -L https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.8.0/languages/json.min.js -o /download/highlight/languages/json.min.js

# Создаем runtime образ с Nginx
FROM nginx:alpine AS final
LABEL version="latest"
WORKDIR /usr/share/nginx/html

# Копируем опубликованные файлы
COPY --from=publish /app/publish/wwwroot .

# Создаем директории для CSS и JS
RUN mkdir -p /usr/share/nginx/html/css/bootstrap
RUN mkdir -p /usr/share/nginx/html/css/highlight
RUN mkdir -p /usr/share/nginx/html/js/highlight/languages

# Копируем Bootstrap CSS в нужную директорию
COPY --from=build /app/publish/wwwroot/lib/bootstrap/dist/css/bootstrap.min.css /usr/share/nginx/html/css/bootstrap/

# Копируем highlight.js файлы
COPY --from=highlight /download/highlight/highlight.min.js /usr/share/nginx/html/js/highlight/
COPY --from=highlight /download/highlight/atom-one-dark.min.css /usr/share/nginx/html/css/highlight/
COPY --from=highlight /download/highlight/languages/ /usr/share/nginx/html/js/highlight/languages/

# Копируем конфигурацию Nginx
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Устанавливаем переменные окружения
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]