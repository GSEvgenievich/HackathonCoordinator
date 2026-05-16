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
-- 1. СОЗДАНИЕ ВСЕХ ТАБЛИЦ
-- ============================================

-- 1. Positions (Должности) - НОВАЯ ТАБЛИЦА
CREATE TABLE IF NOT EXISTS Positions (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Name VARCHAR(50) NOT NULL
) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

-- 2. ChatTypes (Типы чатов)
CREATE TABLE IF NOT EXISTS ChatTypes (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Name VARCHAR(50) NOT NULL
) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

-- 3. NotificationTypes (Типы уведомлений)
CREATE TABLE IF NOT EXISTS NotificationTypes (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Name VARCHAR(100) NOT NULL UNIQUE,
    Category VARCHAR(15) NOT NULL,
    Icon VARCHAR(10) NOT NULL
) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

-- 4. ProfileIcons (Иконки профилей)
CREATE TABLE IF NOT EXISTS ProfileIcons (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Name VARCHAR(50) NOT NULL
) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

-- 5. Roles (Роли пользователей)
CREATE TABLE IF NOT EXISTS Roles (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Name VARCHAR(25) NOT NULL UNIQUE
) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

-- 6. TaskStatuses (Статусы задач)
CREATE TABLE IF NOT EXISTS TaskStatuses (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Name VARCHAR(50) NOT NULL UNIQUE
) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

-- 7. TaskTypes (Типы задач)
CREATE TABLE IF NOT EXISTS TaskTypes (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Name VARCHAR(50) NOT NULL UNIQUE
) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

-- 8. Chats (Чаты)
CREATE TABLE IF NOT EXISTS Chats (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Name VARCHAR(150) NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    TypeId INT NOT NULL,
    FOREIGN KEY (TypeId) REFERENCES ChatTypes(Id)
) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

-- 9. Users (Пользователи) - с добавленным полем PositionId
CREATE TABLE IF NOT EXISTS Users (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Username VARCHAR(100) NOT NULL,
    Login VARCHAR(50) NOT NULL,
    Email VARCHAR(150) NOT NULL UNIQUE,
    PasswordHash VARCHAR(255) NOT NULL,
    RoleId INT NOT NULL DEFAULT 4,
    PositionId INT NOT NULL DEFAULT 1,
    TeamId INT NULL,
    ProfileIconId INT NULL,
    GitHubUsername VARCHAR(100) NULL,
    GitHubAccessToken VARCHAR(255) NULL,
    GitHubAvatarUrl VARCHAR(255) NULL,
    FOREIGN KEY (RoleId) REFERENCES Roles(Id),
    FOREIGN KEY (PositionId) REFERENCES Positions(Id),
    FOREIGN KEY (ProfileIconId) REFERENCES ProfileIcons(Id) ON DELETE SET NULL
) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

-- 10. Competitions (Соревнования) - добавлены новые поля
CREATE TABLE IF NOT EXISTS Competitions (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Name VARCHAR(200) NOT NULL,
    Description VARCHAR(1000) NOT NULL,
    StartDate DATETIME NOT NULL,
    EndDate DATETIME NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CreatedById INT NOT NULL,
    IsArchived BOOLEAN NOT NULL DEFAULT FALSE,
    HasResults BOOLEAN NOT NULL DEFAULT FALSE,
    IsStartNotified BOOLEAN NOT NULL DEFAULT FALSE,
    IsEndNotified BOOLEAN NOT NULL DEFAULT FALSE,
    ResultsCreatedAt DATETIME NULL,
    ResultsCreatedById INT NULL,
    ResultsUpdatedAt DATETIME NULL,
    ResultsUpdatedById INT NULL,
    FOREIGN KEY (CreatedById) REFERENCES Users(Id),
    FOREIGN KEY (ResultsCreatedById) REFERENCES Users(Id),
    FOREIGN KEY (ResultsUpdatedById) REFERENCES Users(Id)
) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

-- 11. Teams (Команды)
CREATE TABLE IF NOT EXISTS Teams (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    CompetitionId INT NOT NULL,
    Name VARCHAR(100) NOT NULL,
    InviteCode VARCHAR(36) NOT NULL UNIQUE,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    GitRepoName VARCHAR(100) NULL,
    ChatId INT NULL,
    FOREIGN KEY (CompetitionId) REFERENCES Competitions(Id),
    FOREIGN KEY (ChatId) REFERENCES Chats(Id) ON DELETE SET NULL
) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

-- 12. Обновляем Users для добавления связи с Teams
ALTER TABLE Users 
ADD CONSTRAINT FK_Users_Teams FOREIGN KEY (TeamId) REFERENCES Teams(Id) ON DELETE SET NULL;

-- 13. Messages (Сообщения)
CREATE TABLE IF NOT EXISTS Messages (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    ChatId INT NOT NULL,
    UserId INT NOT NULL,
    Text VARCHAR(1000) NOT NULL,
    SentAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    IsEdited BOOLEAN NOT NULL DEFAULT FALSE,
    EditedAt DATETIME NULL,
    HasAttachments BOOLEAN NOT NULL DEFAULT FALSE,
    FOREIGN KEY (ChatId) REFERENCES Chats(Id) ON DELETE CASCADE,
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

-- 14. MessageAttachments (Вложения сообщений) - НОВАЯ ТАБЛИЦА
CREATE TABLE IF NOT EXISTS MessageAttachments (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    MessageId INT NOT NULL,
    FileName VARCHAR(255) NOT NULL,
    FileSize BIGINT NOT NULL,
    ContentType VARCHAR(100) NOT NULL,
    FilePath VARCHAR(500) NOT NULL,
    Thumbnail LONGBLOB NULL,
    UploadedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (MessageId) REFERENCES Messages(Id) ON DELETE CASCADE
) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

-- 15. Notifications (Уведомления)
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

-- 16. Stages (Этапы соревнований) - НОВАЯ ТАБЛИЦА
CREATE TABLE IF NOT EXISTS Stages (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    CompetitionId INT NOT NULL,
    Name VARCHAR(50) NOT NULL,
    Description VARCHAR(300) NULL,
    StartTime DATETIME NOT NULL,
    EndTime DATETIME NOT NULL,
    Location VARCHAR(150) NULL,
    `Order` INT NOT NULL,
    IsFinal BOOLEAN NOT NULL DEFAULT FALSE,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    IsStartNotified BOOLEAN NOT NULL DEFAULT FALSE,
    FOREIGN KEY (CompetitionId) REFERENCES Competitions(Id) ON DELETE CASCADE,
    UNIQUE KEY UQ_Stages_Competition_Order (CompetitionId, `Order`)
) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

-- 17. Tasks (Задачи)
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

-- 18. Results (Результаты соревнований) - НОВАЯ ТАБЛИЦА
CREATE TABLE IF NOT EXISTS Results (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    CompetitionId INT NOT NULL,
    TeamId INT NOT NULL,
    Place INT NOT NULL,
    PlaceDisplay VARCHAR(10) NOT NULL,
    Comment VARCHAR(300) NULL,
    FOREIGN KEY (CompetitionId) REFERENCES Competitions(Id),
    FOREIGN KEY (TeamId) REFERENCES Teams(Id),
    UNIQUE KEY UQ_Results_Competition_Team (CompetitionId, TeamId)
) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

-- 19. FinalTeamMembers (Финальный состав команды) - НОВАЯ ТАБЛИЦА
CREATE TABLE IF NOT EXISTS FinalTeamMembers (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    TeamId INT NOT NULL,
    UserId INT NULL,
    Username VARCHAR(100) NOT NULL,
    PositionName VARCHAR(50) NOT NULL,
    RoleId INT NOT NULL,
    FixedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (TeamId) REFERENCES Teams(Id) ON DELETE CASCADE,
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE SET NULL,
    FOREIGN KEY (RoleId) REFERENCES Roles(Id)
) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

-- ============================================
-- 2. ЗАПОЛНЕНИЕ ДАННЫМИ
-- ============================================

-- Positions (должности)
INSERT IGNORE INTO Positions (Id, Name) VALUES
(1, 'Разработчик'),
(2, 'Дизайнер'),
(3, 'Менеджер');

-- ChatTypes
INSERT IGNORE INTO ChatTypes (Id, Name) VALUES
(1, 'Чат команды'),
(2, 'Чат по задаче');

-- NotificationTypes (расширенный список)
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
(16, 'Создано новое соревнование', 'competition', '🏆'),
(17, 'Соревнование началось', 'competition', '🏆'),
(18, 'Соревнование завершено', 'competition', '🎉'),
(19, 'Системное уведомление', 'system', 'ℹ️'),
(20, 'Команда удалена', 'system', '🗑️'),
(21, 'Завершение задачи отменено', 'task', '⚠'),
(22, 'Срок задачи истек', 'task', '⏰'),
(23, 'Изменение прав доступа', 'system', '👑'),
(24, 'Подведены итоги соревнования', 'competition', '🏆'),
(25, 'Обновлены итоги соревнования', 'competition', '🏆'),
(26, 'Начался новый этап соревнования', 'competition', '⏰'),
(27, 'Соревнование удалено', 'competition', '🗑️'),
(28, 'Соревнование архивировано', 'competition', '📦');

-- ProfileIcons
INSERT IGNORE INTO ProfileIcons (Id, Name) VALUES
(1, 'boy1'),
(2, 'boy2'),
(3, 'girl1'),
(4, 'girl2'),
(5, 'robot1'),
(6, 'robot2'),
(7, 'robot3'),
(8, 'cat1'),
(9, 'cat2');

-- Roles (исправлено: 1-Admin, 2-Organizer, 3-Captain, 4-Member)
INSERT IGNORE INTO Roles (Id, Name) VALUES
(1, 'Администратор'),
(2, 'Организатор'),
(3, 'Капитан'),
(4, 'Участник');

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
-- 3. ТЕСТОВЫЕ ПОЛЬЗОВАТЕЛИ
-- ============================================

-- Пароли захешированы BCrypt:
-- q -> 123 (Admin)
-- w -> 234 (Organizer)
-- e -> 345 (Captain)
-- r -> 456 (Member)
-- string -> string (User)

-- Admin (RoleId = 1)
INSERT IGNORE INTO Users (Username, Email, Login, PasswordHash, RoleId, PositionId, ProfileIconId) VALUES
('Admin', 'admin@hackathon.local', 'q', 
'$2a$11$4Z1LpSzns9GvCcS1oVZfiuXfpzK94uxmiGrKwEXgp7P6EradwwFjq', 1, 1, 1);

-- Organizer (RoleId = 2)
INSERT IGNORE INTO Users (Username, Email, Login, PasswordHash, RoleId, PositionId, ProfileIconId) VALUES
('Организатор', 'organizer@hackathon.local', 'w',
'$2a$11$gL5KSSMRGol9tpjLcKLD..53qHVld4E6/bIZwJct1tZ3V2fpHxuHG', 2, 3, 3);

-- User (RoleId = 4)
INSERT IGNORE INTO Users (Username, Email, Login, PasswordHash, RoleId, PositionId, ProfileIconId) VALUES
('Пользователь', 'user@hackathon.local', 'string',
'$2a$11$OKN4OwK6mYHPsxLGtgxJ3uMfceo4Bj3C/VZbhbaj40iPdZixzRyeO', 4, 1, 1);

-- ============================================
-- 4. ФИНАЛЬНАЯ ИНИЦИАЛИЗАЦИЯ
-- ============================================

SELECT '✅ База данных успешно инициализирована!' as Message;
SELECT COUNT(*) as UsersCount FROM Users;
SELECT COUNT(*) as RolesCount FROM Roles;
SELECT COUNT(*) as CompetitionsCount FROM Competitions;