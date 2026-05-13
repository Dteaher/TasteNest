/*
    RecipeKeeper database for Microsoft SQL Server / SQL Server Management Studio.

    Как использовать:
    1. Открой SQL Server Management Studio.
    2. Подключись к серверу.
    3. Открой этот файл.
    4. Нажми Execute / Выполнить.

    Скрипт создаёт базу RecipeKeeperDb, таблицы, связи и начальные данные.
    WPF-приложение при этом не меняется и продолжает работать как раньше.
*/

IF DB_ID(N'RecipeKeeperDb') IS NULL
BEGIN
    CREATE DATABASE RecipeKeeperDb;
END
GO

USE RecipeKeeperDb;
GO

IF OBJECT_ID(N'dbo.RecipeStats', N'U') IS NOT NULL DROP TABLE dbo.RecipeStats;
IF OBJECT_ID(N'dbo.ShoppingItems', N'U') IS NOT NULL DROP TABLE dbo.ShoppingItems;
IF OBJECT_ID(N'dbo.MealPlan', N'U') IS NOT NULL DROP TABLE dbo.MealPlan;
IF OBJECT_ID(N'dbo.RecipeViews', N'U') IS NOT NULL DROP TABLE dbo.RecipeViews;
IF OBJECT_ID(N'dbo.Favorites', N'U') IS NOT NULL DROP TABLE dbo.Favorites;
IF OBJECT_ID(N'dbo.Products', N'U') IS NOT NULL DROP TABLE dbo.Products;
IF OBJECT_ID(N'dbo.Ingredients', N'U') IS NOT NULL DROP TABLE dbo.Ingredients;
IF OBJECT_ID(N'dbo.Recipes', N'U') IS NOT NULL DROP TABLE dbo.Recipes;
IF OBJECT_ID(N'dbo.Categories', N'U') IS NOT NULL DROP TABLE dbo.Categories;
IF OBJECT_ID(N'dbo.Users', N'U') IS NOT NULL DROP TABLE dbo.Users;
GO

CREATE TABLE dbo.Users
(
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
    Email NVARCHAR(256) NOT NULL CONSTRAINT UQ_Users_Email UNIQUE,
    PasswordHash NVARCHAR(128) NOT NULL,
    Role NVARCHAR(40) NOT NULL CONSTRAINT DF_Users_Role DEFAULT N'User',
    Status INT NOT NULL CONSTRAINT DF_Users_Status DEFAULT 1
);
GO

CREATE TABLE dbo.Categories
(
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Categories PRIMARY KEY,
    Name NVARCHAR(120) NOT NULL CONSTRAINT UQ_Categories_Name UNIQUE
);
GO

CREATE TABLE dbo.Recipes
(
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Recipes PRIMARY KEY,
    Title NVARCHAR(180) NOT NULL,
    Description NVARCHAR(700) NOT NULL,
    Instructions NVARCHAR(MAX) NOT NULL,
    CookingTime INT NOT NULL,
    Servings INT NOT NULL,
    CategoryId INT NOT NULL,
    UserId INT NULL,
    ImageUrl NVARCHAR(700) NOT NULL CONSTRAINT DF_Recipes_ImageUrl DEFAULT N'',
    Difficulty NVARCHAR(40) NOT NULL CONSTRAINT DF_Recipes_Difficulty DEFAULT N'Лёгкий',
    CONSTRAINT FK_Recipes_Categories FOREIGN KEY (CategoryId) REFERENCES dbo.Categories(Id),
    CONSTRAINT FK_Recipes_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id)
);
GO

CREATE TABLE dbo.Ingredients
(
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Ingredients PRIMARY KEY,
    RecipeId INT NOT NULL,
    Name NVARCHAR(120) NOT NULL,
    Amount NVARCHAR(80) NULL,
    CONSTRAINT FK_Ingredients_Recipes FOREIGN KEY (RecipeId) REFERENCES dbo.Recipes(Id) ON DELETE CASCADE
);
GO

CREATE TABLE dbo.Products
(
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Products PRIMARY KEY,
    UserId INT NOT NULL,
    Name NVARCHAR(120) NOT NULL,
    Quantity NVARCHAR(80) NULL,
    ExpiresAt DATE NULL,
    Unit NVARCHAR(40) NOT NULL CONSTRAINT DF_Products_Unit DEFAULT N'',
    Category NVARCHAR(120) NOT NULL CONSTRAINT DF_Products_Category DEFAULT N'',
    Note NVARCHAR(500) NOT NULL CONSTRAINT DF_Products_Note DEFAULT N'',
    CONSTRAINT FK_Products_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id) ON DELETE CASCADE
);
GO

CREATE TABLE dbo.Favorites
(
    UserId INT NOT NULL,
    RecipeId INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL,
    CONSTRAINT PK_Favorites PRIMARY KEY (UserId, RecipeId),
    CONSTRAINT FK_Favorites_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id) ON DELETE CASCADE,
    CONSTRAINT FK_Favorites_Recipes FOREIGN KEY (RecipeId) REFERENCES dbo.Recipes(Id) ON DELETE CASCADE
);
GO

CREATE TABLE dbo.RecipeViews
(
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_RecipeViews PRIMARY KEY,
    UserId INT NOT NULL,
    RecipeId INT NOT NULL,
    ViewedAt DATETIME2 NOT NULL,
    CONSTRAINT FK_RecipeViews_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id) ON DELETE CASCADE,
    CONSTRAINT FK_RecipeViews_Recipes FOREIGN KEY (RecipeId) REFERENCES dbo.Recipes(Id) ON DELETE CASCADE
);
GO

CREATE TABLE dbo.MealPlan
(
    UserId INT NOT NULL,
    DayName NVARCHAR(60) NOT NULL,
    RecipeId INT NOT NULL,
    CONSTRAINT PK_MealPlan PRIMARY KEY (UserId, DayName),
    CONSTRAINT FK_MealPlan_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id) ON DELETE CASCADE,
    CONSTRAINT FK_MealPlan_Recipes FOREIGN KEY (RecipeId) REFERENCES dbo.Recipes(Id) ON DELETE CASCADE
);
GO

CREATE TABLE dbo.ShoppingItems
(
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ShoppingItems PRIMARY KEY,
    UserId INT NOT NULL,
    Name NVARCHAR(120) NOT NULL,
    Quantity NVARCHAR(200) NULL,
    IsBought BIT NOT NULL CONSTRAINT DF_ShoppingItems_IsBought DEFAULT 0,
    CONSTRAINT FK_ShoppingItems_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id) ON DELETE CASCADE
);
GO

CREATE TABLE dbo.RecipeStats
(
    RecipeId INT NOT NULL CONSTRAINT PK_RecipeStats PRIMARY KEY,
    CookCount INT NOT NULL CONSTRAINT DF_RecipeStats_CookCount DEFAULT 0,
    Rating INT NOT NULL CONSTRAINT DF_RecipeStats_Rating DEFAULT 5,
    CONSTRAINT FK_RecipeStats_Recipes FOREIGN KEY (RecipeId) REFERENCES dbo.Recipes(Id) ON DELETE CASCADE
);
GO

CREATE INDEX IX_Recipes_CategoryId ON dbo.Recipes(CategoryId);
CREATE INDEX IX_Ingredients_RecipeId ON dbo.Ingredients(RecipeId);
CREATE INDEX IX_Products_UserId_Name ON dbo.Products(UserId, Name);
CREATE INDEX IX_RecipeViews_UserId_ViewedAt ON dbo.RecipeViews(UserId, ViewedAt DESC);
GO

INSERT INTO dbo.Users (Email, PasswordHash, Role, Status)
VALUES
(N'admin@recipe.local', N'8C6976E5B5410415BDE908BD4DEE15DFB167A9C873FC4BB8A81F6F2AB448A918', N'Admin', 1);
GO

INSERT INTO dbo.Categories (Name)
VALUES
(N'Завтраки'),
(N'Супы'),
(N'Основные блюда'),
(N'Салаты'),
(N'Десерты'),
(N'Паста и крупы'),
(N'Выпечка'),
(N'Напитки'),
(N'Закуски'),
(N'Вегетарианское');
GO

DECLARE @Breakfast INT = (SELECT Id FROM dbo.Categories WHERE Name = N'Завтраки');
DECLARE @Soup INT = (SELECT Id FROM dbo.Categories WHERE Name = N'Супы');
DECLARE @Main INT = (SELECT Id FROM dbo.Categories WHERE Name = N'Основные блюда');
DECLARE @Salad INT = (SELECT Id FROM dbo.Categories WHERE Name = N'Салаты');
DECLARE @Dessert INT = (SELECT Id FROM dbo.Categories WHERE Name = N'Десерты');
DECLARE @Grains INT = (SELECT Id FROM dbo.Categories WHERE Name = N'Паста и крупы');
DECLARE @Bakery INT = (SELECT Id FROM dbo.Categories WHERE Name = N'Выпечка');
DECLARE @Drinks INT = (SELECT Id FROM dbo.Categories WHERE Name = N'Напитки');
DECLARE @Snacks INT = (SELECT Id FROM dbo.Categories WHERE Name = N'Закуски');
DECLARE @Veg INT = (SELECT Id FROM dbo.Categories WHERE Name = N'Вегетарианское');

INSERT INTO dbo.Recipes (Title, Description, Instructions, CookingTime, Servings, CategoryId, ImageUrl, Difficulty)
VALUES
(N'Банановые панкейки', N'Мягкие панкейки с бананом для завтрака.', N'Разомните банан, смешайте с яйцом, молоком и мукой. Жарьте небольшими порциями.', 20, 2, @Breakfast, N'https://images.unsplash.com/photo-1528207776546-365bb710ee93?auto=format&fit=crop&w=900&q=80', N'Лёгкий'),
(N'Куриный суп с лапшой', N'Домашний суп с курицей и овощами.', N'Сварите курицу, добавьте овощи и лапшу. Варите до готовности.', 55, 4, @Soup, N'', N'Средний'),
(N'Омлет с сыром и томатами', N'Нежный омлет на сковороде.', N'Взбейте яйца с молоком, добавьте томаты и сыр. Готовьте 6-8 минут.', 15, 2, @Breakfast, N'', N'Лёгкий'),
(N'Тыквенный суп-пюре', N'Нежный кремовый суп из тыквы.', N'Отварите тыкву с картофелем, пробейте блендером, добавьте сливки.', 40, 4, @Soup, N'', N'Средний'),
(N'Паста с томатным соусом', N'Паста с соусом из томатов и чеснока.', N'Отварите пасту. Обжарьте чеснок, добавьте томаты, смешайте с пастой.', 25, 3, @Grains, N'', N'Лёгкий'),
(N'Сырники с ягодами', N'Классические творожные сырники к завтраку.', N'Смешайте творог, яйцо, муку и сахар. Обжарьте сырники и подайте с ягодами.', 30, 3, @Dessert, N'', N'Лёгкий'),
(N'Овощной салат с фетой', N'Свежий салат из овощей и сыра.', N'Нарежьте овощи, добавьте фету, масло и зелень.', 12, 2, @Salad, N'', N'Лёгкий'),
(N'Яблочный пирог', N'Домашняя выпечка к чаю.', N'Смешайте тесто, добавьте яблоки и запекайте до румяной корочки.', 50, 6, @Bakery, N'', N'Средний'),
(N'Гречка с грибами', N'Простое горячее блюдо из крупы и шампиньонов.', N'Отварите гречку. Обжарьте лук и грибы, смешайте с крупой.', 35, 3, @Grains, N'', N'Лёгкий'),
(N'Домашний лимонад', N'Освежающий напиток с лимоном и мятой.', N'Смешайте лимонный сок, воду, сахар и мяту. Охладите перед подачей.', 10, 4, @Drinks, N'', N'Лёгкий'),
(N'Брускетта с томатами', N'Хрустящая закуска с томатами и зеленью.', N'Подсушите хлеб. Смешайте томаты, чеснок, масло и зелень, выложите сверху.', 15, 2, @Snacks, N'', N'Лёгкий'),
(N'Рис с овощами', N'Лёгкий гарнир или самостоятельное блюдо.', N'Обжарьте овощи, добавьте рис и немного воды. Тушите до готовности.', 30, 3, @Veg, N'', N'Лёгкий'),
(N'Курица с картофелем', N'Сытное блюдо для ужина.', N'Запеките курицу с картофелем, луком и специями до золотистой корочки.', 60, 4, @Main, N'', N'Средний');
GO

INSERT INTO dbo.Ingredients (RecipeId, Name, Amount)
SELECT r.Id, v.Name, v.Amount
FROM dbo.Recipes r
CROSS APPLY
(
    VALUES
    (N'Банановые панкейки', N'банан', N'1 шт.'),
    (N'Банановые панкейки', N'мука', N'150 г'),
    (N'Банановые панкейки', N'молоко', N'120 мл'),
    (N'Банановые панкейки', N'яйцо', N'1 шт.'),
    (N'Куриный суп с лапшой', N'курица', N'300 г'),
    (N'Куриный суп с лапшой', N'картофель', N'3 шт.'),
    (N'Куриный суп с лапшой', N'лапша', N'80 г'),
    (N'Омлет с сыром и томатами', N'яйца', N'3 шт.'),
    (N'Омлет с сыром и томатами', N'сыр', N'60 г'),
    (N'Омлет с сыром и томатами', N'томаты', N'1 шт.'),
    (N'Паста с томатным соусом', N'паста', N'250 г'),
    (N'Паста с томатным соусом', N'томаты', N'300 г'),
    (N'Паста с томатным соусом', N'чеснок', N'2 зубчика'),
    (N'Сырники с ягодами', N'творог', N'400 г'),
    (N'Сырники с ягодами', N'мука', N'4 ст. л.'),
    (N'Овощной салат с фетой', N'огурцы', N'2 шт.'),
    (N'Овощной салат с фетой', N'фета', N'80 г'),
    (N'Гречка с грибами', N'гречка', N'200 г'),
    (N'Гречка с грибами', N'грибы', N'250 г')
) AS v(RecipeTitle, Name, Amount)
WHERE r.Title = v.RecipeTitle;
GO

INSERT INTO dbo.Favorites (UserId, RecipeId, CreatedAt)
SELECT 1, Id, SYSDATETIME()
FROM dbo.Recipes
WHERE Title IN (N'Банановые панкейки', N'Куриный суп с лапшой', N'Омлет с сыром и томатами', N'Паста с томатным соусом');
GO

INSERT INTO dbo.RecipeStats (RecipeId, CookCount, Rating)
SELECT Id, ABS(CHECKSUM(NEWID())) % 20 + 1, 4 + ABS(CHECKSUM(NEWID())) % 2
FROM dbo.Recipes;
GO

SELECT 'RecipeKeeperDb создана для SSMS' AS Result;
GO
