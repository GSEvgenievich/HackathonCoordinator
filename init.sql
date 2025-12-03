SET NAMES utf8mb4 COLLATE utf8mb4_unicode_ci;
-- ============================================
-- ИНИЦИАЛИЗАЦИЯ БАЗЫ ДАННЫХ HackathonCoordinatorDb
-- ============================================

-- Создаем базу данных, если её нет
CREATE DATABASE IF NOT EXISTS HackathonCoordinatorDb 
CHARACTER SET utf8mb4 
COLLATE utf8mb4_unicode_ci;

USE HackathonCoordinatorDb;

-- Создаем пользователя
CREATE USER IF NOT EXISTS 'hackathon_user'@'%' IDENTIFIED BY 'UserPassword123!';
GRANT ALL PRIVILEGES ON HackathonCoordinatorDb.* TO 'hackathon_user'@'%';
FLUSH PRIVILEGES;

-- ============================================
-- СОЗДАНИЕ ВСЕХ ТАБЛИЦ (включая основные!)
-- ============================================

-- 1. ChatTypes (Типы чатов)
CREATE TABLE IF NOT EXISTS ChatTypes (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Name VARCHAR(50) NOT NULL
) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

-- 2. NotificationTypes (Типы уведомлений)
CREATE TABLE IF NOT EXISTS NotificationTypes (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Name VARCHAR(100) NOT NULL UNIQUE,
    Category VARCHAR(15) NOT NULL,
    Icon VARCHAR(10) NOT NULL
) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

-- 3. ProfileIcons (Иконки профилей)
CREATE TABLE IF NOT EXISTS ProfileIcons (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Name VARCHAR(50) NOT NULL
) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

-- 4. Roles (Роли пользователей)
CREATE TABLE IF NOT EXISTS Roles (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Name VARCHAR(25) NOT NULL UNIQUE
) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

-- 5. TaskStatuses (Статусы задач)
CREATE TABLE IF NOT EXISTS TaskStatuses (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Name VARCHAR(50) NOT NULL UNIQUE
) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

-- 6. TaskTypes (Типы задач)
CREATE TABLE IF NOT EXISTS TaskTypes (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Name VARCHAR(50) NOT NULL UNIQUE
) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

-- 7. Chats (Чаты) - ОСНОВНАЯ ТАБЛИЦА
CREATE TABLE IF NOT EXISTS Chats (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Name VARCHAR(150) NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    TypeId INT NOT NULL,
    FOREIGN KEY (TypeId) REFERENCES ChatTypes(Id)
) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

-- 8. Users (Пользователи) - ОСНОВНАЯ ТАБЛИЦА (нужна для Competitions)
CREATE TABLE IF NOT EXISTS Users (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Username VARCHAR(100) NOT NULL,
    Login VARCHAR(50) NOT NULL,
    Email VARCHAR(150) NOT NULL UNIQUE,
    PasswordHash VARCHAR(255) NOT NULL,
    RoleId INT NOT NULL DEFAULT 2,
    TeamId INT NULL,
    ProfileIconId INT NULL,
    GitHubUsername VARCHAR(100) NULL,
    GitHubAccessToken VARCHAR(255) NULL,
    GitHubAvatarUrl VARCHAR(255) NULL,
    FOREIGN KEY (RoleId) REFERENCES Roles(Id),
    FOREIGN KEY (ProfileIconId) REFERENCES ProfileIcons(Id) ON DELETE SET NULL
) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

-- 9. Competitions (Соревнования) - ОСНОВНАЯ ТАБЛИЦА
CREATE TABLE IF NOT EXISTS Competitions (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Name VARCHAR(200) NOT NULL,
    Description VARCHAR(1000) NOT NULL,
    StartDate DATETIME NOT NULL,
    EndDate DATETIME NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CreatedById INT NOT NULL,
    FOREIGN KEY (CreatedById) REFERENCES Users(Id)
) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

-- 10. Teams (Команды) - ОСНОВНАЯ ТАБЛИЦА
CREATE TABLE IF NOT EXISTS Teams (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    CompetitionId INT NOT NULL,
    Name VARCHAR(100) NOT NULL,
    InviteCode VARCHAR(36) NOT NULL UNIQUE,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    GitRepoName VARCHAR(100) NULL,
    ChatId INT NOT NULL,
    FOREIGN KEY (CompetitionId) REFERENCES Competitions(Id),
    FOREIGN KEY (ChatId) REFERENCES Chats(Id)
) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

-- 11. Обновляем Users для добавления связи с Teams (после создания Teams)
ALTER TABLE Users 
ADD FOREIGN KEY (TeamId) REFERENCES Teams(Id) ON DELETE SET NULL;

-- 12. Messages (Сообщения) - ОСНОВНАЯ ТАБЛИЦА
CREATE TABLE IF NOT EXISTS Messages (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    ChatId INT NOT NULL,
    UserId INT NOT NULL,
    Text VARCHAR(1000) NOT NULL,
    SentAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    IsEdited BOOLEAN NOT NULL DEFAULT FALSE,
    EditedAt DATETIME NULL,
    FOREIGN KEY (ChatId) REFERENCES Chats(Id) ON DELETE CASCADE,
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

-- 13. Notifications (Уведомления) - ОСНОВНАЯ ТАБЛИЦА
CREATE TABLE IF NOT EXISTS Notifications (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    UserId INT NOT NULL,
    NotificationTypeId INT NOT NULL,
    Title VARCHAR(50) NOT NULL,
    Message VARCHAR(1000) NOT NULL,
    RelatedEntityType VARCHAR(20) NULL,
    RelatedEntityId INT NULL,
    IsRead BOOLEAN NOT NULL DEFAULT FALSE,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ReadAt DATETIME NULL,
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    FOREIGN KEY (NotificationTypeId) REFERENCES NotificationTypes(Id) ON DELETE CASCADE
) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

-- 14. Tasks (Задачи) - ОСНОВНАЯ ТАБЛИЦА
CREATE TABLE IF NOT EXISTS Tasks (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    TeamId INT NOT NULL,
    AssignedToId INT NULL,
    Title VARCHAR(200) NOT NULL,
    Description VARCHAR(1000) NOT NULL,
    TypeId INT NOT NULL,
    StatusId INT NOT NULL,
    Deadline DATETIME NULL,
    GithubBranchName VARCHAR(100) NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ChatId INT NOT NULL,
    IsDeadlineNotified BOOLEAN NOT NULL DEFAULT FALSE,
    IsDeadlineApproachNotified BOOLEAN NOT NULL DEFAULT FALSE,
    FOREIGN KEY (TeamId) REFERENCES Teams(Id) ON DELETE CASCADE,
    FOREIGN KEY (AssignedToId) REFERENCES Users(Id) ON DELETE SET NULL,
    FOREIGN KEY (TypeId) REFERENCES TaskTypes(Id),
    FOREIGN KEY (StatusId) REFERENCES TaskStatuses(Id),
    FOREIGN KEY (ChatId) REFERENCES Chats(Id) ON DELETE CASCADE
) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

-- ============================================
-- ЗАПОЛНЕНИЕ ДАННЫМИ
-- ============================================

-- ChatTypes
INSERT IGNORE INTO ChatTypes (Id, Name) VALUES
(1, 'Чат команды'),
(2, 'Чат по задаче');

-- NotificationTypes
INSERT IGNORE INTO NotificationTypes (Id, Name, Category, Icon) VALUES
(1, 'Новая задача', 'task', '🎯'),
(2, 'Задача назначена', 'task', '👤'),
(3, 'Задача завершена', 'task', '✅'),
(4, 'Задача подтверждена', 'task', '👍'),
(5, 'Задача отменена', 'task', '❌'),
(6, 'Срок задачи истекает', 'task', '⏰'),
(7, 'Важное сообщение от капитана в чате задачи', 'task', '💬'),
(8, 'Новый участник команды', 'team', '👥'),
(9, 'Участник вышел из команды', 'team', '🚶‍♂️'),
(10, 'Вас выгнали из команды', 'team', '🚪'),
(11, 'Был назначен новый капитан команды', 'team', '👑'),
(12, 'Вы стали капитаном', 'team', '👑'),
(13, 'Создан GitHub репозиторий', 'team', '🔗'),
(14, 'Важное сообщение от капитана в чате команды', 'team', '💬'),
(15, 'Создана новая команда', 'system', '👥'),
(16, 'Создано новое соревнование', 'system', '🏆'),
(17, 'Соревнование началось', 'system', '🏆'),
(18, 'Соревнование завершено', 'system', '🎉'),
(19, 'Системное уведомление', 'system', 'ℹ️'),
(20, 'Команда удалена', 'system', '🗑️'),
(21, 'Завершение задачи отменено', 'task', '⚠'),
(22, 'Срок задачи истек', 'task', '⏰');

-- ProfileIcons
INSERT IGNORE INTO ProfileIcons (Id, Name) VALUES
(1, 'boy1'),
(2, 'boy2'),
(3, 'girl1'),
(4, 'girl2'),
(5, 'robot1'),
(6, 'robot2');

-- Roles
INSERT IGNORE INTO Roles (Id, Name) VALUES
(1, 'Капитан'),
(2, 'Участник'),
(3, 'Организатор');

-- TaskStatuses
INSERT IGNORE INTO TaskStatuses (Id, Name) VALUES
(1, 'В планах'),
(2, 'В процессе'),
(3, 'На проверке'),
(4, 'Завершена'),
(5, 'Отменена');

-- TaskTypes
INSERT IGNORE INTO TaskTypes (Id, Name) VALUES
(1, 'Фича'),
(2, 'Баг'),
(3, 'Документация');

-- ============================================
-- ТЕСТОВЫЕ ПОЛЬЗОВАТЕЛЕЙ (ваши данные)
-- ============================================

INSERT IGNORE INTO Users (Username, Email, Login, PasswordHash, RoleId, ProfileIconId) VALUES
('Head1', 'admin@hackathon.local', 'q', 
'$2a$11$4Z1LpSzns9GvCcS1oVZfiuXfpzK94uxmiGrKwEXgp7P6EradwwFjq', 3, 5);

INSERT IGNORE INTO Users (Username, Email, Login, PasswordHash, RoleId, ProfileIconId) VALUES
('string', 'string@hackathon.local', 'string',
'$2a$11$OKN4OwK6mYHPsxLGtgxJ3uMfceo4Bj3C/VZbhbaj40iPdZixzRyeO', 2, 3);

-- ============================================
-- ФИНАЛЬНАЯ ИНИЦИАЛИЗАЦИЯ
-- ============================================

SELECT '✅ База данных успешно инициализирована!' as Message;