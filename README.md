# 📊 FintaCharts API

FintaCharts — це .NET API для роботи з фінансовими інструментами, провайдерами та цінами в реальному часі. Проєкт використовує PostgreSQL як базу даних і підтримує запуск у Docker через `docker-compose`.

---

## 🚀 Швидкий старт

### 📦 Вимоги

- [Docker](https://www.docker.com/)
- [Docker Compose](https://docs.docker.com/compose/)
- (опційно) [.NET SDK 9.0](https://dotnet.microsoft.com/) — для локального запуску без контейнерів

---

### ⚙️ Запуск у Docker

1. Клонуй репозиторій:

```bash
git clone https://github.com/Bohdan1812/FintaChartsApi.git
```

2. Запусти `docker-compose` в папці FintaChartsApi:

```bash
docker-compose up --build
```

3. API буде доступне за адресою:

```
http://localhost:8080
```

---

### 📡 Доступні ендпоінти

| Метод | Шлях                                      | Опис                              |
|-------|-------------------------------------------|-----------------------------------|
| GET   | `/fintacharts/providers`                  | Отримати список провайдерів       |
| GET   | `/fintacharts/instruments`                | Отримати список інструментів      |
| GET   | `/fintacharts/instrumentPrice`            | Отримати останню ціну інструменту |
|       | Параметри: `instrumentId`, `provider`     |                                   |


> ❗ Swagger не використовується. Тестування рекомендується через Postman або curl.

---

### 🐘 Конфігурація бази даних

PostgreSQL запускається в контейнері з такими параметрами:

- База: `fintacharts_db`
- Користувач: `fintacharts_user`
- Пароль: `db_password12!`
- Порт: `5432`

---

### 🛠 Корисні команди

| Команда                          | Опис                                 |
|----------------------------------|--------------------------------------|
| `docker-compose up --build`     | Запуск проєкту з перескладанням      |
| `docker-compose down`           | Зупинити та видалити контейнери      |
| `docker-compose logs -f`        | Перегляд логів у реальному часі      |
| `docker ps`                     | Перевірити, чи контейнери працюють   |

---


## 🧠 Примітки


- Для зручного дебагу в Visual Studio використовуй профіль `Docker Compose`.

---

