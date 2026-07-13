# WebGL Async Chess — шахматы с ботом и CI/CD пайплайном

Веб-приложение для игры в шахматы против бота с автоматической обработкой игровых сессий. Проект демонстрирует построение полного CI/CD-пайплайна: непрерывная интеграция, контейнеризация и автоматическое развёртывание.

---

## Описание

Игрок играет белыми против бота (чёрные). Каждый ход отправляется на сервер, где сохраняется в базе данных PostgreSQL. Сервер вычисляет актуальную позицию через `chess.js`, запускает асинхронный расчёт хода бота и возвращает результат клиенту. Поддерживается определение завершения партии: мат, пат, ничья, троекратное повторение позиции.

Реализована вторая доска с автоматическим воспроизведением лучшей (самой короткой) завершённой партии. Игровой таймер ограничивает партию 15 минутами.

### Основные возможности

- Игра в шахматы против бота (случайные легальные ходы через `chess.js`)
- Автоматическое превращение пешки в ферзя
- Определение мата, пата, ничьей
- Таймер 15 минут с поражением по истечении времени
- Вторая доска с повтором лучшей партии
- Сохранение всех партий в PostgreSQL
- Панель результата (победа, поражение, ничья)

### API Endpoints

| Метод | Путь | Описание |
|-------|------|----------|
| `POST` | `/api/sessions` | Создать новую игровую сессию |
| `GET` | `/api/sessions` | Получить список всех сессий |
| `GET` | `/api/sessions/last` | Последняя завершённая сессия |
| `GET` | `/api/sessions/best` | Партия с минимальной длительностью |
| `GET` | `/api/sessions/:id` | Получить сессию по ID |
| `GET` | `/api/sessions/:id/result` | Получить результат партии |
| `PUT` | `/api/sessions/:id` | Обновить сессию (ход игрока) |
| `DELETE` | `/api/sessions/:id` | Удалить сессию |

---

## Технологии

- **Клиент:** Unity WebGL, C#
- **Сервер:** Node.js, Express
- **База данных:** PostgreSQL
- **Шахматный движок:** chess.js
- **Контейнеризация:** Docker, docker-compose
- **CI:** GitHub Actions (ESLint, тесты, сборка Docker)
- **CD:** Railway (автодеплой)
- **Хостинг клиента:** GitHub Pages

---

## Запуск локально

### Сервер

```bash
cd server
npm install
npm start

Сервер запустится на http://localhost:5000.

### Клиент (Unity)

Открыть проект в Unity, запустить сцену ChessScene2D. В редакторе автоматически используется http://localhost:5000.

---

## Запуск в Docker

### Сборка образа

docker build -t chess-server ./server

### Запуск контейнера

docker run -p 5000:5000 chess-server

### Запуск через docker-compose

docker compose up --build

Поднимает PostgreSQL и сервер. Сервер доступен на http://localhost:5000.

---

## CI/CD

При каждом Pull Request в ветку main автоматически запускаются:

1. ESLint — проверка стиля кода сервера (npm run lint)
2. ESLint (клиент) — проверка docs/api.js
3. Tests — 8 unit-тестов API с PostgreSQL
4. Docker build — сборка Docker-образа сервера

При ошибках на любом этапе слияние PR заблокировано.

При успешном merge в main Railway автоматически деплоит новую версию сервера. Включена опция Wait for CI — деплой ожидает успешного завершения GitHub Actions.

Файл пайплайна: .github/workflows/ci.yml

---

## Переменные окружения

| Переменная | Описание | Пример |
|-----------|----------|--------|
| PORT | Порт сервера | 5000 |
| DB_HOST | Хост базы данных | localhost |
| DB_PORT | Порт базы данных | 5432 |
| DB_NAME | Имя базы данных | chess_sessions |
| DB_USER | Пользователь БД | postgres |
| DB_PASSWORD | Пароль БД | postgres |
| PUBLIC_URL | Публичный URL сервера | https://server.up.railway.app |

---

## Публичный URL

- Клиент (игра): https://xpdamir.github.io/webgl-async-ci-cd/
- Сервер (API): https://webgl-async-ci-cd-production.up.railway.app/

---

## Структура проекта

webgl-async-ci-cd/
├── client/                     # Исходный код Unity
│   └── Assets/Scripts/         # C# скрипты
├── docs/                       # Билд WebGL + api.js + index.html
│   ├── api.js                  # Клиентский API-модуль
│   ├── Build/                  # Файлы билда Unity
│   └── index.html              # Точка входа
├── server/                     # Сервер Node.js
│   ├── routes/sessions.js      # CRUD API
│   ├── services/bot.js         # Логика бота (chess.js)
│   ├── db.js                   # Подключение к PostgreSQL
│   ├── index.js                # Точка входа сервера
│   ├── Dockerfile              # Инструкция сборки Docker
│   ├── tests/sessions.test.js  # Unit-тесты API
│   └── .env.example            # Пример переменных окружения
├── .github/workflows/ci.yml    # CI/CD пайплайн
├── docker-compose.yml          # Локальное окружение
└── README.md