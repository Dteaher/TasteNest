IF DB_ID(N'RecipeKeeperDb') IS NULL
BEGIN
    CREATE DATABASE RecipeKeeperDb;
END
GO

USE RecipeKeeperDb;
GO

IF OBJECT_ID(N'dbo.RecipeStats', N'U') IS NOT NULL DROP TABLE dbo.RecipeStats;
IF OBJECT_ID(N'dbo.OperatorActionLog', N'U') IS NOT NULL DROP TABLE dbo.OperatorActionLog;
IF OBJECT_ID(N'dbo.OperatorUserNotes', N'U') IS NOT NULL DROP TABLE dbo.OperatorUserNotes;
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
    Status INT NOT NULL CONSTRAINT DF_Users_Status DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT SYSUTCDATETIME(),
    LastLoginAt DATETIME2 NULL
);
GO

CREATE TABLE dbo.OperatorUserNotes
(
    UserId INT NOT NULL CONSTRAINT PK_OperatorUserNotes PRIMARY KEY,
    Note NVARCHAR(1000) NOT NULL CONSTRAINT DF_OperatorUserNotes_Note DEFAULT N'',
    UpdatedByUserId INT NULL,
    UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_OperatorUserNotes_UpdatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_OperatorUserNotes_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id) ON DELETE CASCADE,
    CONSTRAINT FK_OperatorUserNotes_Operators FOREIGN KEY (UpdatedByUserId) REFERENCES dbo.Users(Id)
);
GO

CREATE TABLE dbo.OperatorActionLog
(
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_OperatorActionLog PRIMARY KEY,
    OperatorUserId INT NULL,
    TargetUserId INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_OperatorActionLog_CreatedAt DEFAULT SYSUTCDATETIME(),
    Reason NVARCHAR(120) NOT NULL,
    Comment NVARCHAR(1000) NOT NULL CONSTRAINT DF_OperatorActionLog_Comment DEFAULT N'',
    OldStatus BIT NOT NULL,
    NewStatus BIT NOT NULL,
    CONSTRAINT FK_OperatorActionLog_Operators FOREIGN KEY (OperatorUserId) REFERENCES dbo.Users(Id),
    CONSTRAINT FK_OperatorActionLog_TargetUsers FOREIGN KEY (TargetUserId) REFERENCES dbo.Users(Id) ON DELETE CASCADE
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
    Status NVARCHAR(40) NOT NULL CONSTRAINT DF_Recipes_Status DEFAULT N'Published',
    ModerationComment NVARCHAR(500) NOT NULL CONSTRAINT DF_Recipes_ModerationComment DEFAULT N'',
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Recipes_CreatedAt DEFAULT SYSUTCDATETIME(),
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
CREATE INDEX IX_Recipes_Status ON dbo.Recipes(Status);
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
(N'Банановые панкейки', N'Мягкие панкейки с бананом для завтрака.', N'Разомните банан, смешайте с яйцом, молоком и мукой. Жарьте небольшими порциями.', 20, 2, @Breakfast, N'Assets/Recipes/recipe-061.jpg', N'Лёгкий'),
(N'Куриный суп с лапшой', N'Домашний суп с курицей и овощами.', N'Сварите курицу, добавьте овощи и лапшу. Варите до готовности.', 55, 4, @Soup, N'Assets/Recipes/recipe-052.jpg', N'Средний'),
(N'Омлет с сыром и томатами', N'Нежный омлет на сковороде.', N'Взбейте яйца с молоком, добавьте томаты и сыр. Готовьте 6-8 минут.', 15, 2, @Breakfast, N'Assets/Recipes/recipe-051.jpg', N'Лёгкий'),
(N'Тыквенный суп-пюре', N'Нежный кремовый суп из тыквы.', N'Отварите тыкву с картофелем, пробейте блендером, добавьте сливки.', 40, 4, @Soup, N'Assets/Recipes/recipe-059.jpg', N'Средний'),
(N'Паста с томатным соусом', N'Паста с соусом из томатов и чеснока.', N'Отварите пасту. Обжарьте чеснок, добавьте томаты, смешайте с пастой.', 25, 3, @Grains, N'Assets/Recipes/recipe-053.jpg', N'Лёгкий'),
(N'Сырники с ягодами', N'Классические творожные сырники к завтраку.', N'Смешайте творог, яйцо, муку и сахар. Обжарьте сырники и подайте с ягодами.', 30, 3, @Dessert, N'Assets/Recipes/recipe-055.jpg', N'Лёгкий'),
(N'Овощной салат с фетой', N'Свежий салат из овощей и сыра.', N'Нарежьте овощи, добавьте фету, масло и зелень.', 12, 2, @Salad, N'Assets/Recipes/recipe-054.jpg', N'Лёгкий'),
(N'Яблочный пирог', N'Домашняя выпечка к чаю.', N'Смешайте тесто, добавьте яблоки и запекайте до румяной корочки.', 50, 6, @Bakery, N'Assets/Recipes/recipe-062.jpg', N'Средний'),
(N'Гречка с грибами', N'Простое горячее блюдо из крупы и шампиньонов.', N'Отварите гречку. Обжарьте лук и грибы, смешайте с крупой.', 35, 3, @Grains, N'Assets/Recipes/recipe-056.jpg', N'Лёгкий'),
(N'Домашний лимонад', N'Освежающий напиток с лимоном и мятой.', N'Смешайте лимонный сок, воду, сахар и мяту. Охладите перед подачей.', 10, 4, @Drinks, N'Assets/Recipes/recipe-063.jpg', N'Лёгкий'),
(N'Брускетта с томатами', N'Хрустящая закуска с томатами и зеленью.', N'Подсушите хлеб. Смешайте томаты, чеснок, масло и зелень, выложите сверху.', 15, 2, @Snacks, N'Assets/Recipes/recipe-058.jpg', N'Лёгкий'),
(N'Рис с овощами', N'Лёгкий гарнир или самостоятельное блюдо.', N'Обжарьте овощи, добавьте рис и немного воды. Тушите до готовности.', 30, 3, @Veg, N'Assets/Recipes/recipe-057.jpg', N'Лёгкий'),
(N'Курица с картофелем', N'Сытное блюдо для ужина.', N'Запеките курицу с картофелем, луком и специями до золотистой корочки.', 60, 4, @Main, N'Assets/Recipes/recipe-060.jpg', N'Средний');GO


UPDATE dbo.Recipes
SET ImageUrl = CASE Title
    WHEN N'Банановые панкейки' THEN N'Assets/Recipes/recipe-061.jpg'
    WHEN N'Куриный суп с лапшой' THEN N'Assets/Recipes/recipe-052.jpg'
    WHEN N'Омлет с сыром и томатами' THEN N'Assets/Recipes/recipe-051.jpg'
    WHEN N'Тыквенный суп-пюре' THEN N'Assets/Recipes/recipe-059.jpg'
    WHEN N'Паста с томатным соусом' THEN N'Assets/Recipes/recipe-053.jpg'
    WHEN N'Сырники с ягодами' THEN N'Assets/Recipes/recipe-055.jpg'
    WHEN N'Овощной салат с фетой' THEN N'Assets/Recipes/recipe-054.jpg'
    WHEN N'Яблочный пирог' THEN N'Assets/Recipes/recipe-062.jpg'
    WHEN N'Гречка с грибами' THEN N'Assets/Recipes/recipe-056.jpg'
    WHEN N'Домашний лимонад' THEN N'Assets/Recipes/recipe-063.jpg'
    WHEN N'Брускетта с томатами' THEN N'Assets/Recipes/recipe-058.jpg'
    WHEN N'Рис с овощами' THEN N'Assets/Recipes/recipe-057.jpg'
    WHEN N'Курица с картофелем' THEN N'Assets/Recipes/recipe-060.jpg'
    ELSE ImageUrl
END
WHERE (ImageUrl IS NULL OR ImageUrl = N'' OR ImageUrl LIKE N'http%')
  AND Title IN (N'Банановые панкейки', N'Куриный суп с лапшой', N'Омлет с сыром и томатами', N'Тыквенный суп-пюре', N'Паста с томатным соусом', N'Сырники с ягодами', N'Овощной салат с фетой', N'Яблочный пирог', N'Гречка с грибами', N'Домашний лимонад', N'Брускетта с томатами', N'Рис с овощами', N'Курица с картофелем');
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


-- Expanded TasteNest recipe catalog. Safe to run repeatedly.
DECLARE @ExpandedRecipes TABLE
(
    Title NVARCHAR(255) NOT NULL,
    Description NVARCHAR(MAX) NOT NULL,
    Instructions NVARCHAR(MAX) NOT NULL,
    CookingTime INT NOT NULL,
    Servings INT NOT NULL,
    CategoryName NVARCHAR(120) NOT NULL,
    Difficulty NVARCHAR(60) NOT NULL,
    ImageUrl NVARCHAR(700) NOT NULL,
    IngredientsText NVARCHAR(MAX) NOT NULL
);

INSERT INTO @ExpandedRecipes (Title, Description, Instructions, CookingTime, Servings, CategoryName, Difficulty, ImageUrl, IngredientsText)
VALUES
(N'Овсяная каша с ягодами', N'Сытный завтрак с овсянкой, молоком и свежими ягодами.', N'Сварите овсянку на молоке, добавьте мёд, ягоды и орехи. Подавайте горячей.', 15, 2, N'Завтраки', N'Лёгкий', N'Assets/Recipes/recipe-001.jpg', N'овсяные хлопья - 120 г' + CHAR(10) + N'молоко - 350 мл' + CHAR(10) + N'ягоды - 120 г' + CHAR(10) + N'мёд - 1 ст. л.' + CHAR(10) + N'орехи - 30 г'),
(N'Шакшука с перцем', N'Яйца в густом томатном соусе с болгарским перцем.', N'Обжарьте лук и перец, добавьте томаты и специи. Сделайте углубления, разбейте яйца и готовьте под крышкой.', 25, 2, N'Завтраки', N'Средний', N'Assets/Recipes/recipe-002.jpg', N'яйца - 4 шт.' + CHAR(10) + N'томаты - 350 г' + CHAR(10) + N'перец - 1 шт.' + CHAR(10) + N'лук - 1 шт.' + CHAR(10) + N'паприка - 1 ч. л.'),
(N'Тосты с авокадо и яйцом', N'Хрустящие тосты с авокадо, яйцом и зеленью.', N'Подсушите хлеб, разомните авокадо с лимонным соком. Сверху выложите яйцо и зелень.', 12, 2, N'Завтраки', N'Лёгкий', N'Assets/Recipes/recipe-003.jpg', N'хлеб - 4 ломтика' + CHAR(10) + N'авокадо - 1 шт.' + CHAR(10) + N'яйца - 2 шт.' + CHAR(10) + N'лимонный сок - 1 ч. л.' + CHAR(10) + N'зелень - 15 г'),
(N'Гранола с йогуртом', N'Быстрый завтрак с хрустящей гранолой, йогуртом и фруктами.', N'Выложите йогурт в чашу, добавьте гранолу, банан, ягоды и немного мёда.', 8, 2, N'Завтраки', N'Лёгкий', N'Assets/Recipes/recipe-004.jpg', N'йогурт - 300 г' + CHAR(10) + N'гранола - 120 г' + CHAR(10) + N'банан - 1 шт.' + CHAR(10) + N'ягоды - 100 г' + CHAR(10) + N'мёд - 1 ст. л.'),
(N'Блины на кефире', N'Тонкие домашние блины на кефире для завтрака или десерта.', N'Смешайте кефир, яйца, муку и сахар. Добавьте масло и жарьте тонкие блины на разогретой сковороде.', 35, 4, N'Завтраки', N'Средний', N'Assets/Recipes/recipe-005.jpg', N'кефир - 500 мл' + CHAR(10) + N'мука - 220 г' + CHAR(10) + N'яйца - 2 шт.' + CHAR(10) + N'сахар - 2 ст. л.' + CHAR(10) + N'масло - 2 ст. л.'),
(N'Рисовая каша с яблоком', N'Нежная молочная рисовая каша с яблоком и корицей.', N'Отварите рис в молоке, добавьте тёртое яблоко, сахар и корицу. Дайте настояться под крышкой.', 30, 3, N'Завтраки', N'Лёгкий', N'Assets/Recipes/recipe-006.jpg', N'рис - 160 г' + CHAR(10) + N'молоко - 500 мл' + CHAR(10) + N'яблоко - 1 шт.' + CHAR(10) + N'сахар - 1 ст. л.' + CHAR(10) + N'корица - 0.5 ч. л.'),
(N'Французские тосты', N'Сладкие тосты в яично-молочной смеси с ягодами.', N'Взбейте яйца с молоком и сахаром. Обмакните хлеб и обжарьте с двух сторон до золотистой корочки.', 18, 2, N'Завтраки', N'Лёгкий', N'Assets/Recipes/recipe-007.jpg', N'хлеб - 4 ломтика' + CHAR(10) + N'яйца - 2 шт.' + CHAR(10) + N'молоко - 120 мл' + CHAR(10) + N'сахар - 1 ст. л.' + CHAR(10) + N'ягоды - 80 г'),
(N'Борщ с говядиной', N'Классический насыщенный борщ с говядиной, свёклой и капустой.', N'Сварите бульон, добавьте картофель, капусту и овощную зажарку со свёклой. Подавайте со сметаной.', 90, 6, N'Супы', N'Сложный', N'Assets/Recipes/recipe-008.jpg', N'говядина - 500 г' + CHAR(10) + N'свёкла - 2 шт.' + CHAR(10) + N'капуста - 300 г' + CHAR(10) + N'картофель - 3 шт.' + CHAR(10) + N'томатная паста - 2 ст. л.'),
(N'Сырный суп с брокколи', N'Кремовый суп с брокколи, плавленым сыром и сухариками.', N'Отварите овощи, добавьте сыр и пробейте блендером. Прогрейте до однородности.', 35, 4, N'Супы', N'Лёгкий', N'Assets/Recipes/recipe-009.jpg', N'брокколи - 400 г' + CHAR(10) + N'картофель - 2 шт.' + CHAR(10) + N'сыр плавленый - 180 г' + CHAR(10) + N'сливки - 100 мл' + CHAR(10) + N'лук - 1 шт.'),
(N'Чечевичный суп', N'Питательный суп из красной чечевицы с морковью и специями.', N'Обжарьте овощи, добавьте чечевицу и воду. Варите до мягкости, приправьте зирой и паприкой.', 40, 4, N'Супы', N'Лёгкий', N'Assets/Recipes/recipe-010.jpg', N'чечевица - 220 г' + CHAR(10) + N'морковь - 1 шт.' + CHAR(10) + N'лук - 1 шт.' + CHAR(10) + N'томат - 1 шт.' + CHAR(10) + N'зира - 0.5 ч. л.'),
(N'Солянка мясная', N'Кисло-пряный суп с копчёностями, огурцами и маслинами.', N'Сварите бульон, добавьте мясные продукты, огурцы, томатную пасту и маслины. Дайте настояться.', 70, 6, N'Супы', N'Сложный', N'Assets/Recipes/recipe-011.jpg', N'говядина - 300 г' + CHAR(10) + N'копчёности - 250 г' + CHAR(10) + N'солёные огурцы - 3 шт.' + CHAR(10) + N'маслины - 80 г' + CHAR(10) + N'томатная паста - 2 ст. л.'),
(N'Грибной крем-суп', N'Бархатный крем-суп из шампиньонов со сливками.', N'Обжарьте грибы с луком, добавьте картофель и бульон. Пробейте блендером и влейте сливки.', 35, 4, N'Супы', N'Средний', N'Assets/Recipes/recipe-012.jpg', N'шампиньоны - 450 г' + CHAR(10) + N'картофель - 2 шт.' + CHAR(10) + N'лук - 1 шт.' + CHAR(10) + N'сливки - 150 мл' + CHAR(10) + N'бульон - 700 мл'),
(N'Рыбный суп с картофелем', N'Лёгкий рыбный суп с картофелем, морковью и зеленью.', N'Отварите картофель и морковь, добавьте рыбу и готовьте до мягкости. Подавайте с укропом.', 35, 4, N'Супы', N'Лёгкий', N'Assets/Recipes/recipe-013.jpg', N'рыба - 400 г' + CHAR(10) + N'картофель - 3 шт.' + CHAR(10) + N'морковь - 1 шт.' + CHAR(10) + N'лук - 1 шт.' + CHAR(10) + N'укроп - 20 г'),
(N'Лосось с овощами', N'Запечённый лосось с брокколи, морковью и лимоном.', N'Выложите лосось и овощи в форму, сбрызните маслом и лимоном. Запекайте до готовности.', 35, 2, N'Основные блюда', N'Средний', N'Assets/Recipes/recipe-014.jpg', N'лосось - 400 г' + CHAR(10) + N'брокколи - 250 г' + CHAR(10) + N'морковь - 1 шт.' + CHAR(10) + N'лимон - 0.5 шт.' + CHAR(10) + N'оливковое масло - 2 ст. л.'),
(N'Котлеты с картофельным пюре', N'Домашние котлеты с нежным картофельным пюре.', N'Сформируйте котлеты из фарша и обжарьте. Отварите картофель и разомните с молоком и маслом.', 55, 4, N'Основные блюда', N'Средний', N'Assets/Recipes/recipe-015.jpg', N'фарш - 500 г' + CHAR(10) + N'картофель - 800 г' + CHAR(10) + N'молоко - 150 мл' + CHAR(10) + N'лук - 1 шт.' + CHAR(10) + N'масло - 40 г'),
(N'Говядина в соусе', N'Тушёная говядина в густом соусе с луком и морковью.', N'Обжарьте мясо, добавьте овощи, бульон и тушите до мягкости. Подавайте с гарниром.', 85, 4, N'Основные блюда', N'Сложный', N'Assets/Recipes/recipe-016.jpg', N'говядина - 600 г' + CHAR(10) + N'лук - 1 шт.' + CHAR(10) + N'морковь - 1 шт.' + CHAR(10) + N'бульон - 400 мл' + CHAR(10) + N'томатная паста - 1 ст. л.'),
(N'Индейка с гречкой', N'Полезное горячее блюдо из индейки, гречки и овощей.', N'Обжарьте индейку, добавьте овощи и гречку. Влейте воду и тушите до готовности.', 45, 3, N'Основные блюда', N'Лёгкий', N'Assets/Recipes/recipe-017.jpg', N'индейка - 400 г' + CHAR(10) + N'гречка - 200 г' + CHAR(10) + N'морковь - 1 шт.' + CHAR(10) + N'лук - 1 шт.' + CHAR(10) + N'вода - 450 мл'),
(N'Запечённые перцы с фаршем', N'Болгарские перцы с мясной начинкой и рисом.', N'Смешайте фарш с рисом и овощами, наполните перцы. Запекайте под соусом до мягкости.', 70, 4, N'Основные блюда', N'Средний', N'Assets/Recipes/recipe-018.jpg', N'перец - 6 шт.' + CHAR(10) + N'фарш - 500 г' + CHAR(10) + N'рис - 120 г' + CHAR(10) + N'томатный соус - 300 мл' + CHAR(10) + N'лук - 1 шт.'),
(N'Куриное филе в сливочном соусе', N'Нежное куриное филе в сливках с чесноком и зеленью.', N'Обжарьте курицу, добавьте чеснок и сливки. Тушите до густого соуса.', 30, 3, N'Основные блюда', N'Лёгкий', N'Assets/Recipes/recipe-019.jpg', N'куриное филе - 500 г' + CHAR(10) + N'сливки - 200 мл' + CHAR(10) + N'чеснок - 2 зубчика' + CHAR(10) + N'зелень - 20 г' + CHAR(10) + N'масло - 1 ст. л.'),
(N'Тефтели в томатном соусе', N'Мясные тефтели с рисом в ароматном томатном соусе.', N'Сформируйте тефтели, обжарьте и тушите в томатном соусе до готовности.', 50, 4, N'Основные блюда', N'Средний', N'Assets/Recipes/recipe-020.jpg', N'фарш - 500 г' + CHAR(10) + N'рис - 100 г' + CHAR(10) + N'томатный соус - 350 мл' + CHAR(10) + N'лук - 1 шт.' + CHAR(10) + N'яйцо - 1 шт.'),
(N'Рагу из овощей и курицы', N'Домашнее рагу с курицей, картофелем и сезонными овощами.', N'Нарежьте курицу и овощи, обжарьте и тушите под крышкой до мягкости.', 55, 4, N'Основные блюда', N'Лёгкий', N'Assets/Recipes/recipe-021.jpg', N'курица - 500 г' + CHAR(10) + N'картофель - 4 шт.' + CHAR(10) + N'кабачок - 1 шт.' + CHAR(10) + N'морковь - 1 шт.' + CHAR(10) + N'лук - 1 шт.'),
(N'Свинина с рисом и овощами', N'Сытное блюдо из свинины, риса и овощной смеси.', N'Обжарьте свинину, добавьте рис, овощи и воду. Готовьте под крышкой до мягкости.', 50, 4, N'Основные блюда', N'Средний', N'Assets/Recipes/recipe-022.jpg', N'свинина - 500 г' + CHAR(10) + N'рис - 220 г' + CHAR(10) + N'перец - 1 шт.' + CHAR(10) + N'морковь - 1 шт.' + CHAR(10) + N'соевый соус - 2 ст. л.'),
(N'Лазанья домашняя', N'Слоёная лазанья с мясным соусом, бешамелем и сыром.', N'Приготовьте мясной соус и бешамель. Соберите слои с листами лазаньи и запекайте.', 90, 6, N'Основные блюда', N'Сложный', N'Assets/Recipes/recipe-023.jpg', N'листы лазаньи - 250 г' + CHAR(10) + N'фарш - 600 г' + CHAR(10) + N'томатный соус - 400 мл' + CHAR(10) + N'молоко - 500 мл' + CHAR(10) + N'сыр - 180 г'),
(N'Цезарь с курицей', N'Популярный салат с курицей, сухариками и сыром.', N'Обжарьте курицу, нарежьте салат, добавьте сухарики, сыр и соус.', 25, 2, N'Салаты', N'Лёгкий', N'Assets/Recipes/recipe-024.jpg', N'куриное филе - 250 г' + CHAR(10) + N'салат ромэн - 150 г' + CHAR(10) + N'сухарики - 50 г' + CHAR(10) + N'пармезан - 40 г' + CHAR(10) + N'соус цезарь - 60 г'),
(N'Греческий салат', N'Свежий салат с фетой, овощами и маслинами.', N'Нарежьте овощи крупно, добавьте фету и маслины. Заправьте маслом и орегано.', 15, 3, N'Салаты', N'Лёгкий', N'Assets/Recipes/recipe-025.jpg', N'томаты - 3 шт.' + CHAR(10) + N'огурцы - 2 шт.' + CHAR(10) + N'фета - 120 г' + CHAR(10) + N'маслины - 80 г' + CHAR(10) + N'оливковое масло - 2 ст. л.'),
(N'Салат с тунцом и фасолью', N'Белковый салат с тунцом, фасолью и красным луком.', N'Смешайте фасоль, тунец, лук и зелень. Заправьте лимонным соком и маслом.', 12, 2, N'Салаты', N'Лёгкий', N'Assets/Recipes/recipe-026.jpg', N'тунец - 1 банка' + CHAR(10) + N'фасоль - 200 г' + CHAR(10) + N'красный лук - 0.5 шт.' + CHAR(10) + N'лимонный сок - 1 ст. л.' + CHAR(10) + N'зелень - 20 г'),
(N'Винегрет', N'Классический овощной салат со свёклой, картофелем и огурцами.', N'Отварите овощи, нарежьте кубиками, добавьте огурцы, горошек и масло.', 60, 5, N'Салаты', N'Средний', N'Assets/Recipes/recipe-027.jpg', N'свёкла - 2 шт.' + CHAR(10) + N'картофель - 3 шт.' + CHAR(10) + N'морковь - 1 шт.' + CHAR(10) + N'огурцы солёные - 3 шт.' + CHAR(10) + N'горошек - 120 г'),
(N'Салат с курицей и ананасом', N'Нежный салат с курицей, ананасом, сыром и кукурузой.', N'Отварите курицу, нарежьте ингредиенты, смешайте с лёгкой заправкой.', 25, 4, N'Салаты', N'Лёгкий', N'Assets/Recipes/recipe-028.jpg', N'курица - 300 г' + CHAR(10) + N'ананас - 200 г' + CHAR(10) + N'сыр - 100 г' + CHAR(10) + N'кукуруза - 150 г' + CHAR(10) + N'йогурт - 80 г'),
(N'Тёплый салат с баклажанами', N'Салат из обжаренных баклажанов, томатов и зелени.', N'Обжарьте баклажаны, добавьте томаты, чеснок и зелень. Подавайте тёплым.', 25, 2, N'Салаты', N'Средний', N'Assets/Recipes/recipe-029.jpg', N'баклажан - 2 шт.' + CHAR(10) + N'томаты - 2 шт.' + CHAR(10) + N'чеснок - 1 зубчик' + CHAR(10) + N'кинза - 20 г' + CHAR(10) + N'масло - 2 ст. л.'),
(N'Тирамису без выпечки', N'Нежный десерт с кремом, кофе и печеньем савоярди.', N'Приготовьте крем, окуните печенье в кофе и соберите слои. Охладите несколько часов.', 30, 6, N'Десерты', N'Средний', N'Assets/Recipes/recipe-030.jpg', N'савоярди - 200 г' + CHAR(10) + N'маскарпоне - 400 г' + CHAR(10) + N'кофе - 200 мл' + CHAR(10) + N'сливки - 200 мл' + CHAR(10) + N'какао - 2 ст. л.'),
(N'Шоколадный брауни', N'Плотный шоколадный пирог с насыщенным вкусом.', N'Растопите шоколад с маслом, смешайте с яйцами, сахаром и мукой. Выпекайте до влажной середины.', 40, 8, N'Десерты', N'Средний', N'Assets/Recipes/recipe-031.jpg', N'шоколад - 200 г' + CHAR(10) + N'масло - 120 г' + CHAR(10) + N'яйца - 3 шт.' + CHAR(10) + N'сахар - 150 г' + CHAR(10) + N'мука - 90 г'),
(N'Панна-котта с ягодами', N'Сливочный итальянский десерт с ягодным соусом.', N'Прогрейте сливки с сахаром и желатином. Разлейте по формам и охладите, подайте с ягодами.', 25, 4, N'Десерты', N'Средний', N'Assets/Recipes/recipe-032.jpg', N'сливки - 400 мл' + CHAR(10) + N'сахар - 80 г' + CHAR(10) + N'желатин - 10 г' + CHAR(10) + N'ягоды - 150 г' + CHAR(10) + N'ваниль - 1 ч. л.'),
(N'Морковный кекс', N'Ароматный кекс с морковью, орехами и корицей.', N'Смешайте сухие и влажные ингредиенты, добавьте морковь и орехи. Выпекайте до готовности.', 55, 8, N'Десерты', N'Лёгкий', N'Assets/Recipes/recipe-033.jpg', N'морковь - 250 г' + CHAR(10) + N'мука - 220 г' + CHAR(10) + N'яйца - 2 шт.' + CHAR(10) + N'орехи - 80 г' + CHAR(10) + N'корица - 1 ч. л.'),
(N'Медовик ленивый', N'Упрощённая версия медового торта со сметанным кремом.', N'Испеките медовый корж, нарежьте и прослоите кремом. Дайте пропитаться.', 60, 8, N'Десерты', N'Средний', N'Assets/Recipes/recipe-034.jpg', N'мёд - 3 ст. л.' + CHAR(10) + N'мука - 260 г' + CHAR(10) + N'яйца - 2 шт.' + CHAR(10) + N'сметана - 500 г' + CHAR(10) + N'сахар - 160 г'),
(N'Творожная запеканка', N'Мягкая запеканка из творога с изюмом и ванилью.', N'Смешайте творог, яйца, манку и сахар. Добавьте изюм и запекайте до румяной корочки.', 50, 6, N'Десерты', N'Лёгкий', N'Assets/Recipes/recipe-035.jpg', N'творог - 600 г' + CHAR(10) + N'яйца - 2 шт.' + CHAR(10) + N'манка - 4 ст. л.' + CHAR(10) + N'сахар - 80 г' + CHAR(10) + N'изюм - 70 г'),
(N'Спагетти карбонара', N'Паста с беконом, яйцом и сыром в сливочной текстуре.', N'Отварите пасту, обжарьте бекон. Смешайте горячую пасту с яйцом, сыром и беконом.', 25, 3, N'Паста и крупы', N'Средний', N'Assets/Recipes/recipe-036.jpg', N'спагетти - 300 г' + CHAR(10) + N'бекон - 150 г' + CHAR(10) + N'яйца - 2 шт.' + CHAR(10) + N'пармезан - 70 г' + CHAR(10) + N'перец - 0.5 ч. л.'),
(N'Плов с курицей', N'Рассыпчатый плов с курицей, морковью и специями.', N'Обжарьте курицу с овощами, добавьте рис, специи и воду. Готовьте под крышкой.', 60, 5, N'Паста и крупы', N'Средний', N'Assets/Recipes/recipe-037.jpg', N'курица - 500 г' + CHAR(10) + N'рис - 350 г' + CHAR(10) + N'морковь - 2 шт.' + CHAR(10) + N'лук - 1 шт.' + CHAR(10) + N'зира - 1 ч. л.'),
(N'Ризотто с грибами', N'Кремовое ризотто с шампиньонами и пармезаном.', N'Обжарьте грибы и рис, постепенно вливайте бульон, постоянно помешивая. Добавьте сыр.', 45, 3, N'Паста и крупы', N'Сложный', N'Assets/Recipes/recipe-038.jpg', N'рис арборио - 240 г' + CHAR(10) + N'грибы - 250 г' + CHAR(10) + N'бульон - 800 мл' + CHAR(10) + N'пармезан - 60 г' + CHAR(10) + N'лук - 1 шт.'),
(N'Лапша удон с овощами', N'Быстрая лапша с овощами и соевым соусом.', N'Обжарьте овощи, добавьте удон и соус. Быстро прогрейте на сильном огне.', 20, 2, N'Паста и крупы', N'Лёгкий', N'Assets/Recipes/recipe-039.jpg', N'удон - 300 г' + CHAR(10) + N'перец - 1 шт.' + CHAR(10) + N'морковь - 1 шт.' + CHAR(10) + N'соевый соус - 3 ст. л.' + CHAR(10) + N'кунжут - 1 ч. л.'),
(N'Кускус с овощами', N'Лёгкое блюдо из кускуса с овощами и зеленью.', N'Залейте кускус кипятком, обжарьте овощи и смешайте. Добавьте зелень.', 18, 3, N'Паста и крупы', N'Лёгкий', N'Assets/Recipes/recipe-040.jpg', N'кускус - 220 г' + CHAR(10) + N'кабачок - 1 шт.' + CHAR(10) + N'перец - 1 шт.' + CHAR(10) + N'томаты - 2 шт.' + CHAR(10) + N'зелень - 20 г'),
(N'Хачапури по-аджарски', N'Лодочка из теста с сыром, яйцом и сливочным маслом.', N'Сформируйте лодочки, наполните сыром и выпекайте. В конце добавьте яйцо и масло.', 75, 4, N'Выпечка', N'Сложный', N'Assets/Recipes/recipe-041.jpg', N'мука - 450 г' + CHAR(10) + N'сыр сулугуни - 400 г' + CHAR(10) + N'яйца - 4 шт.' + CHAR(10) + N'дрожжи - 7 г' + CHAR(10) + N'масло - 40 г'),
(N'Киш с курицей и грибами', N'Открытый пирог с курицей, грибами и сливочной заливкой.', N'Подготовьте песочное тесто, начинку и заливку. Соберите киш и выпекайте.', 70, 6, N'Выпечка', N'Средний', N'Assets/Recipes/recipe-042.jpg', N'тесто - 300 г' + CHAR(10) + N'курица - 250 г' + CHAR(10) + N'грибы - 200 г' + CHAR(10) + N'сливки - 200 мл' + CHAR(10) + N'яйца - 3 шт.'),
(N'Домашний хлеб', N'Простой пшеничный хлеб с хрустящей корочкой.', N'Замесите тесто, дайте подняться, сформируйте буханку и выпекайте до готовности.', 140, 8, N'Выпечка', N'Средний', N'Assets/Recipes/recipe-043.jpg', N'мука - 500 г' + CHAR(10) + N'вода - 320 мл' + CHAR(10) + N'дрожжи - 7 г' + CHAR(10) + N'соль - 1 ч. л.' + CHAR(10) + N'сахар - 1 ч. л.'),
(N'Пирожки с капустой', N'Мягкие пирожки с тушёной капустой.', N'Приготовьте тесто, потушите капусту с луком. Сформируйте пирожки и выпекайте.', 100, 8, N'Выпечка', N'Средний', N'Assets/Recipes/recipe-044.jpg', N'мука - 500 г' + CHAR(10) + N'капуста - 500 г' + CHAR(10) + N'лук - 1 шт.' + CHAR(10) + N'дрожжи - 7 г' + CHAR(10) + N'яйцо - 1 шт.'),
(N'Смузи с бананом и шпинатом', N'Зелёный смузи с бананом, шпинатом и йогуртом.', N'Сложите ингредиенты в блендер и взбейте до гладкости.', 7, 2, N'Напитки', N'Лёгкий', N'Assets/Recipes/recipe-045.jpg', N'банан - 1 шт.' + CHAR(10) + N'шпинат - 60 г' + CHAR(10) + N'йогурт - 250 мл' + CHAR(10) + N'мёд - 1 ч. л.' + CHAR(10) + N'вода - 100 мл'),
(N'Морс из клюквы', N'Кисло-сладкий домашний морс из клюквы.', N'Разомните ягоды, отожмите сок. Жмых проварите, смешайте с соком и сахаром.', 25, 6, N'Напитки', N'Лёгкий', N'Assets/Recipes/recipe-046.jpg', N'клюква - 300 г' + CHAR(10) + N'вода - 1.5 л' + CHAR(10) + N'сахар - 120 г' + CHAR(10) + N'лимон - 0.5 шт.'),
(N'Имбирный чай с лимоном', N'Согревающий напиток с имбирём, лимоном и мёдом.', N'Залейте имбирь кипятком, добавьте лимон и мёд. Дайте настояться.', 10, 2, N'Напитки', N'Лёгкий', N'Assets/Recipes/recipe-047.jpg', N'имбирь - 20 г' + CHAR(10) + N'лимон - 0.5 шт.' + CHAR(10) + N'мёд - 1 ст. л.' + CHAR(10) + N'вода - 500 мл'),
(N'Хумус с питой', N'Нежная закуска из нута с тахини и лимоном.', N'Пробейте нут с тахини, чесноком, лимонным соком и маслом. Подавайте с питой.', 15, 4, N'Закуски', N'Лёгкий', N'Assets/Recipes/recipe-048.jpg', N'нут - 300 г' + CHAR(10) + N'тахини - 2 ст. л.' + CHAR(10) + N'лимонный сок - 2 ст. л.' + CHAR(10) + N'чеснок - 1 зубчик' + CHAR(10) + N'пита - 4 шт.'),
(N'Рулетики из лаваша', N'Быстрая закуска из лаваша с сыром, зеленью и ветчиной.', N'Смажьте лаваш, выложите начинку, сверните рулетом и нарежьте.', 15, 4, N'Закуски', N'Лёгкий', N'Assets/Recipes/recipe-049.jpg', N'лаваш - 2 листа' + CHAR(10) + N'сыр творожный - 160 г' + CHAR(10) + N'ветчина - 150 г' + CHAR(10) + N'зелень - 20 г' + CHAR(10) + N'огурец - 1 шт.'),
(N'Сырная тарелка с орехами', N'Простая закуска из разных сыров, орехов и мёда.', N'Нарежьте сыры, выложите орехи, мёд и фрукты. Подавайте охлаждённой.', 10, 4, N'Закуски', N'Лёгкий', N'Assets/Recipes/recipe-050.jpg', N'сыр твёрдый - 120 г' + CHAR(10) + N'сыр мягкий - 120 г' + CHAR(10) + N'орехи - 60 г' + CHAR(10) + N'мёд - 2 ст. л.' + CHAR(10) + N'виноград - 120 г');

INSERT INTO dbo.Categories (Name)
SELECT DISTINCT er.CategoryName
FROM @ExpandedRecipes er
WHERE NOT EXISTS (SELECT 1 FROM dbo.Categories c WHERE c.Name = er.CategoryName);

INSERT INTO dbo.Recipes (Title, Description, Instructions, CookingTime, Servings, CategoryId, ImageUrl, Difficulty)
SELECT er.Title, er.Description, er.Instructions, er.CookingTime, er.Servings, c.Id, er.ImageUrl, er.Difficulty
FROM @ExpandedRecipes er
JOIN dbo.Categories c ON c.Name = er.CategoryName
WHERE NOT EXISTS (SELECT 1 FROM dbo.Recipes r WHERE r.Title = er.Title);


DECLARE @SeedInstructions TABLE
(
    Title NVARCHAR(255) NOT NULL PRIMARY KEY,
    Instructions NVARCHAR(MAX) NOT NULL
);

INSERT INTO @SeedInstructions (Title, Instructions)
VALUES (N'Омлет с сыром и томатами', N'1. Разбейте яйца в миску, добавьте молоко и щепотку соли, взбейте вилкой до однородности.' + CHAR(10) +
        N'2. Нарежьте томат небольшими кубиками, сыр натрите или нарежьте тонкой стружкой.' + CHAR(10) +
        N'3. Разогрейте сковороду с небольшим количеством масла и вылейте яичную смесь.' + CHAR(10) +
        N'4. Через 2-3 минуты распределите томаты и сыр по поверхности омлета.' + CHAR(10) +
        N'5. Готовьте под крышкой 6-8 минут на слабом огне, пока омлет не схватится.' + CHAR(10) +
        N'6. Подавайте горячим, при желании посыпав зеленью.');

INSERT INTO @SeedInstructions (Title, Instructions)
VALUES (N'Куриный суп с лапшой', N'1. Положите курицу в кастрюлю, залейте водой и варите до готовности, снимая пену.' + CHAR(10) +
        N'2. Достаньте курицу, отделите мясо от костей и верните его в бульон.' + CHAR(10) +
        N'3. Нарежьте картофель, морковь и лук, добавьте овощи в кастрюлю.' + CHAR(10) +
        N'4. Варите суп до мягкости картофеля, затем всыпьте лапшу.' + CHAR(10) +
        N'5. Готовьте ещё 5-7 минут, посолите и приправьте по вкусу.' + CHAR(10) +
        N'6. Дайте супу настояться несколько минут и подавайте горячим.');

INSERT INTO @SeedInstructions (Title, Instructions)
VALUES (N'Паста с томатным соусом', N'1. Отварите пасту в подсоленной воде до состояния аль денте.' + CHAR(10) +
        N'2. Пока паста варится, измельчите чеснок и нарежьте томаты.' + CHAR(10) +
        N'3. Обжарьте чеснок на масле 30-40 секунд, добавьте томаты и тушите до мягкости.' + CHAR(10) +
        N'4. Посолите соус, при желании добавьте немного воды от пасты.' + CHAR(10) +
        N'5. Переложите пасту в соус и хорошо перемешайте.' + CHAR(10) +
        N'6. Подавайте горячей, посыпав сыром.');

INSERT INTO @SeedInstructions (Title, Instructions)
VALUES (N'Овощной салат с фетой', N'1. Вымойте огурцы, томаты и перец, обсушите овощи.' + CHAR(10) +
        N'2. Нарежьте овощи средними кусочками, чтобы салат оставался сочным.' + CHAR(10) +
        N'3. Нарежьте фету кубиками или раскрошите крупными кусочками.' + CHAR(10) +
        N'4. Смешайте овощи с фетой, добавьте масло и зелень.' + CHAR(10) +
        N'5. Аккуратно перемешайте и подавайте сразу после приготовления.');

INSERT INTO @SeedInstructions (Title, Instructions)
VALUES (N'Сырники с ягодами', N'1. Разомните творог вилкой, добавьте яйцо, сахар и муку.' + CHAR(10) +
        N'2. Перемешайте массу до плотного теста, которое держит форму.' + CHAR(10) +
        N'3. Сформируйте небольшие сырники и слегка обваляйте их в муке.' + CHAR(10) +
        N'4. Обжарьте сырники на среднем огне с двух сторон до золотистой корочки.' + CHAR(10) +
        N'5. Доведите под крышкой 2-3 минуты, если сырники получились высокими.' + CHAR(10) +
        N'6. Подавайте с ягодами, сметаной или мёдом.');

INSERT INTO @SeedInstructions (Title, Instructions)
VALUES (N'Гречка с грибами', N'1. Промойте гречку и отварите её в подсоленной воде до рассыпчатости.' + CHAR(10) +
        N'2. Нарежьте лук и грибы небольшими кусочками.' + CHAR(10) +
        N'3. Обжарьте лук на масле до мягкости, затем добавьте грибы.' + CHAR(10) +
        N'4. Готовьте грибы, пока не выпарится лишняя влага.' + CHAR(10) +
        N'5. Добавьте гречку, перемешайте и прогрейте 3-4 минуты.' + CHAR(10) +
        N'6. Подавайте горячей как гарнир или самостоятельное блюдо.');

INSERT INTO @SeedInstructions (Title, Instructions)
VALUES (N'Рис с овощами', N'1. Промойте рис до прозрачной воды.' + CHAR(10) +
        N'2. Нарежьте морковь и перец небольшими кубиками.' + CHAR(10) +
        N'3. Обжарьте овощи на масле 3-4 минуты, добавьте горошек.' + CHAR(10) +
        N'4. Всыпьте рис, перемешайте и влейте горячую воду.' + CHAR(10) +
        N'5. Тушите под крышкой на слабом огне до готовности риса.' + CHAR(10) +
        N'6. Перемешайте готовое блюдо и подавайте горячим.');

INSERT INTO @SeedInstructions (Title, Instructions)
VALUES (N'Брускетта с томатами', N'1. Подсушите ломтики хлеба на сухой сковороде или в духовке.' + CHAR(10) +
        N'2. Нарежьте томаты мелкими кубиками, измельчите чеснок и зелень.' + CHAR(10) +
        N'3. Смешайте томаты с чесноком, зеленью, маслом и щепоткой соли.' + CHAR(10) +
        N'4. Дайте начинке постоять 5 минут, чтобы появился сок.' + CHAR(10) +
        N'5. Выложите томатную смесь на хрустящий хлеб.' + CHAR(10) +
        N'6. Подавайте сразу, пока хлеб не размок.');

INSERT INTO @SeedInstructions (Title, Instructions)
VALUES (N'Тыквенный суп-пюре', N'1. Очистите тыкву, картофель и лук, нарежьте крупными кусочками.' + CHAR(10) +
        N'2. Сложите овощи в кастрюлю, залейте водой так, чтобы она едва покрывала овощи.' + CHAR(10) +
        N'3. Варите до мягкости тыквы и картофеля.' + CHAR(10) +
        N'4. Слейте часть отвара, пробейте овощи блендером до гладкости.' + CHAR(10) +
        N'5. Добавьте сливки, соль и прогрейте суп, не доводя до бурного кипения.' + CHAR(10) +
        N'6. Подавайте горячим, при желании с сухариками.');

INSERT INTO @SeedInstructions (Title, Instructions)
VALUES (N'Курица с картофелем', N'1. Нарежьте курицу порционными кусками, картофель - крупными дольками.' + CHAR(10) +
        N'2. Смешайте курицу и картофель с луком, маслом, солью и специями.' + CHAR(10) +
        N'3. Выложите всё в форму для запекания ровным слоем.' + CHAR(10) +
        N'4. Запекайте при 190 градусах около 45 минут.' + CHAR(10) +
        N'5. Перемешайте или переверните кусочки и допеките до румяной корочки.' + CHAR(10) +
        N'6. Подавайте горячим с овощами или зеленью.');

INSERT INTO @SeedInstructions (Title, Instructions)
VALUES (N'Банановые панкейки', N'1. Разомните банан вилкой до пюре.' + CHAR(10) +
        N'2. Добавьте яйцо, молоко и муку, перемешайте до густого теста.' + CHAR(10) +
        N'3. Разогрейте сковороду и слегка смажьте её маслом.' + CHAR(10) +
        N'4. Выкладывайте тесто небольшими порциями.' + CHAR(10) +
        N'5. Жарьте панкейки с двух сторон до золотистого цвета.' + CHAR(10) +
        N'6. Подавайте тёплыми с мёдом, ягодами или йогуртом.');

INSERT INTO @SeedInstructions (Title, Instructions)
VALUES (N'Яблочный пирог', N'1. Взбейте яйца с сахаром до светлой пены.' + CHAR(10) +
        N'2. Добавьте муку и перемешайте тесто до однородности.' + CHAR(10) +
        N'3. Очистите яблоки и нарежьте тонкими ломтиками.' + CHAR(10) +
        N'4. Вмешайте яблоки в тесто или выложите их в форму и залейте тестом.' + CHAR(10) +
        N'5. Выпекайте при 180 градусах 35-45 минут до сухой шпажки.' + CHAR(10) +
        N'6. Остудите пирог и нарежьте порциями.');

INSERT INTO @SeedInstructions (Title, Instructions)
VALUES (N'Домашний лимонад', N'1. Выжмите сок из лимонов, удалите косточки.' + CHAR(10) +
        N'2. Смешайте лимонный сок с сахаром до растворения.' + CHAR(10) +
        N'3. Добавьте холодную воду и перемешайте.' + CHAR(10) +
        N'4. Положите мяту и слегка разомните её ложкой.' + CHAR(10) +
        N'5. Охладите лимонад 20-30 минут.' + CHAR(10) +
        N'6. Подавайте со льдом и ломтиками лимона.');

INSERT INTO @SeedInstructions (Title, Instructions)
VALUES (N'Овсяная каша с ягодами', N'1. Влейте молоко в небольшую кастрюлю и доведите почти до кипения.' + CHAR(10) +
        N'2. Всыпьте овсяные хлопья, перемешайте и убавьте огонь.' + CHAR(10) +
        N'3. Варите кашу 5-7 минут, периодически помешивая.' + CHAR(10) +
        N'4. Добавьте мёд, перемешайте и снимите с огня.' + CHAR(10) +
        N'5. Разложите кашу по тарелкам, добавьте ягоды и орехи.' + CHAR(10) +
        N'6. Подавайте горячей.');

INSERT INTO @SeedInstructions (Title, Instructions)
VALUES (N'Шакшука с перцем', N'1. Нарежьте лук и перец мелкими кубиками.' + CHAR(10) +
        N'2. Обжарьте овощи на масле до мягкости.' + CHAR(10) +
        N'3. Добавьте томаты, паприку, соль и тушите соус до загустения.' + CHAR(10) +
        N'4. Сделайте в соусе углубления и аккуратно разбейте яйца.' + CHAR(10) +
        N'5. Готовьте под крышкой, пока белок не схватится, а желток останется мягким.' + CHAR(10) +
        N'6. Подавайте шакшуку горячей прямо со сковороды.');

INSERT INTO @SeedInstructions (Title, Instructions)
VALUES (N'Тосты с авокадо и яйцом', N'1. Подсушите ломтики хлеба до хрустящей корочки.' + CHAR(10) +
        N'2. Разомните авокадо с лимонным соком и щепоткой соли.' + CHAR(10) +
        N'3. Приготовьте яйца удобным способом: отварите, пожарьте или сделайте пашот.' + CHAR(10) +
        N'4. Намажьте авокадо на тосты ровным слоем.' + CHAR(10) +
        N'5. Выложите сверху яйцо и посыпьте зеленью.' + CHAR(10) +
        N'6. Подавайте сразу после сборки.');

INSERT INTO @SeedInstructions (Title, Instructions)
VALUES (N'Гранола с йогуртом', N'1. Подготовьте порционные bowls или стаканы.' + CHAR(10) +
        N'2. Выложите на дно часть йогурта.' + CHAR(10) +
        N'3. Добавьте слой гранолы, нарезанный банан и ягоды.' + CHAR(10) +
        N'4. Повторите слои, если посуда высокая.' + CHAR(10) +
        N'5. Полейте мёдом перед подачей.' + CHAR(10) +
        N'6. Подавайте сразу, чтобы гранола осталась хрустящей.');

INSERT INTO @SeedInstructions (Title, Instructions)
VALUES (N'Блины на кефире', N'1. Смешайте кефир с яйцами, сахаром и щепоткой соли.' + CHAR(10) +
        N'2. Постепенно всыпьте муку, размешивая тесто без комков.' + CHAR(10) +
        N'3. Добавьте масло и дайте тесту постоять 10 минут.' + CHAR(10) +
        N'4. Разогрейте сковороду и слегка смажьте её маслом.' + CHAR(10) +
        N'5. Наливайте тесто тонким слоем и жарьте блины с двух сторон.' + CHAR(10) +
        N'6. Складывайте готовые блины стопкой и подавайте тёплыми.');

INSERT INTO @SeedInstructions (Title, Instructions)
VALUES (N'Рисовая каша с яблоком', N'1. Промойте рис и переложите его в кастрюлю.' + CHAR(10) +
        N'2. Влейте молоко, добавьте сахар и варите на слабом огне.' + CHAR(10) +
        N'3. Натрите яблоко или нарежьте мелкими кубиками.' + CHAR(10) +
        N'4. Когда рис станет мягким, добавьте яблоко и корицу.' + CHAR(10) +
        N'5. Проварите ещё 3-5 минут и снимите с огня.' + CHAR(10) +
        N'6. Дайте каше настояться под крышкой и подавайте тёплой.');

INSERT INTO @SeedInstructions (Title, Instructions)
VALUES (N'Французские тосты', N'1. Взбейте яйца с молоком и сахаром.' + CHAR(10) +
        N'2. Обмакните ломтики хлеба в яично-молочную смесь с двух сторон.' + CHAR(10) +
        N'3. Разогрейте сковороду и смажьте её маслом.' + CHAR(10) +
        N'4. Обжарьте тосты до золотистой корочки с обеих сторон.' + CHAR(10) +
        N'5. Выложите на тарелку и добавьте ягоды.' + CHAR(10) +
        N'6. Подавайте сразу, пока тосты мягкие внутри и хрустящие снаружи.');

INSERT INTO @SeedInstructions (Title, Instructions)
VALUES (N'Борщ с говядиной', N'1. Залейте говядину водой и сварите насыщенный бульон, снимая пену.' + CHAR(10) +
        N'2. Достаньте мясо, нарежьте его кусочками и верните в кастрюлю.' + CHAR(10) +
        N'3. Добавьте картофель и капусту, варите до полуготовности.' + CHAR(10) +
        N'4. Отдельно обжарьте свёклу с томатной пастой, луком и морковью.' + CHAR(10) +
        N'5. Переложите зажарку в бульон и варите до мягкости овощей.' + CHAR(10) +
        N'6. Посолите, дайте борщу настояться 10-15 минут.' + CHAR(10) +
        N'7. Подавайте со сметаной и зеленью.');

INSERT INTO @SeedInstructions (Title, Instructions)
VALUES (N'Сырный суп с брокколи', N'1. Нарежьте картофель и лук, брокколи разделите на соцветия.' + CHAR(10) +
        N'2. Отварите овощи в небольшом количестве воды или бульона до мягкости.' + CHAR(10) +
        N'3. Добавьте плавленый сыр и перемешайте до растворения.' + CHAR(10) +
        N'4. Пробейте суп блендером до кремовой текстуры.' + CHAR(10) +
        N'5. Влейте сливки и прогрейте суп на слабом огне.' + CHAR(10) +
        N'6. Подавайте с сухариками или зеленью.');

INSERT INTO @SeedInstructions (Title, Instructions)
VALUES (N'Чечевичный суп', N'1. Промойте чечевицу до прозрачной воды.' + CHAR(10) +
        N'2. Нарежьте лук, морковь и томат.' + CHAR(10) +
        N'3. Обжарьте овощи в кастрюле 3-4 минуты.' + CHAR(10) +
        N'4. Добавьте чечевицу, воду, зиру и паприку.' + CHAR(10) +
        N'5. Варите до мягкости чечевицы, затем посолите.' + CHAR(10) +
        N'6. Подавайте суп горячим, при желании слегка размяв часть чечевицы.');

INSERT INTO @SeedInstructions (Title, Instructions)
VALUES (N'Солянка мясная', N'1. Сварите бульон на говядине до мягкости мяса.' + CHAR(10) +
        N'2. Нарежьте копчёности, солёные огурцы и готовое мясо.' + CHAR(10) +
        N'3. Обжарьте огурцы с томатной пастой несколько минут.' + CHAR(10) +
        N'4. Добавьте в бульон мясо, копчёности, огурцы и маслины.' + CHAR(10) +
        N'5. Варите солянку 10-15 минут на слабом огне.' + CHAR(10) +
        N'6. Дайте настояться и подавайте с лимоном и зеленью.');

INSERT INTO @SeedInstructions (Title, Instructions)
VALUES (N'Грибной крем-суп', N'1. Нарежьте шампиньоны, лук и картофель.' + CHAR(10) +
        N'2. Обжарьте грибы с луком до испарения влаги.' + CHAR(10) +
        N'3. Добавьте картофель и бульон, варите до мягкости.' + CHAR(10) +
        N'4. Пробейте суп блендером до гладкой текстуры.' + CHAR(10) +
        N'5. Влейте сливки, посолите и прогрейте.' + CHAR(10) +
        N'6. Подавайте горячим, украсив грибами или зеленью.');

INSERT INTO @SeedInstructions (Title, Instructions)
VALUES (N'Рыбный суп с картофелем', N'1. Нарежьте картофель, морковь и лук.' + CHAR(10) +
        N'2. Отварите овощи в воде до полуготовности.' + CHAR(10) +
        N'3. Добавьте кусочки рыбы и варите на слабом огне.' + CHAR(10) +
        N'4. Посолите, снимите пену при необходимости.' + CHAR(10) +
        N'5. Готовьте до мягкости рыбы и картофеля.' + CHAR(10) +
        N'6. Подавайте суп с укропом.');

INSERT INTO @SeedInstructions (Title, Instructions)
VALUES (N'Лосось с овощами', N'1. Разделите брокколи на соцветия, морковь нарежьте тонкими ломтиками.' + CHAR(10) +
        N'2. Выложите лосось и овощи в форму для запекания.' + CHAR(10) +
        N'3. Сбрызните маслом, лимонным соком, посолите.' + CHAR(10) +
        N'4. Запекайте при 190 градусах 20-25 минут.' + CHAR(10) +
        N'5. Проверьте готовность рыбы: она должна легко разделяться вилкой.' + CHAR(10) +
        N'6. Подавайте сразу с запечёнными овощами.');

INSERT INTO @SeedInstructions (Title, Instructions)
VALUES (N'Котлеты с картофельным пюре', N'1. Смешайте фарш с измельчённым луком, солью и специями.' + CHAR(10) +
        N'2. Сформируйте котлеты одинакового размера.' + CHAR(10) +
        N'3. Обжарьте котлеты с двух сторон и доведите под крышкой.' + CHAR(10) +
        N'4. Отварите картофель до мягкости.' + CHAR(10) +
        N'5. Разомните картофель с молоком и маслом до пюре.' + CHAR(10) +
        N'6. Подавайте котлеты с горячим пюре.');

INSERT INTO @SeedInstructions (Title, Instructions)
VALUES (N'Говядина в соусе', N'1. Нарежьте говядину средними кусочками.' + CHAR(10) +
        N'2. Обжарьте мясо до румяной корочки.' + CHAR(10) +
        N'3. Добавьте лук и морковь, готовьте ещё 5 минут.' + CHAR(10) +
        N'4. Вмешайте томатную пасту и влейте бульон.' + CHAR(10) +
        N'5. Тушите под крышкой до мягкости говядины.' + CHAR(10) +
        N'6. Доведите соус до нужной густоты и подавайте с гарниром.');

INSERT INTO @SeedInstructions (Title, Instructions)
VALUES (N'Индейка с гречкой', N'1. Нарежьте индейку небольшими кусочками.' + CHAR(10) +
        N'2. Обжарьте индейку с луком и морковью до лёгкой корочки.' + CHAR(10) +
        N'3. Промойте гречку и добавьте её в сковороду или сотейник.' + CHAR(10) +
        N'4. Влейте воду, посолите и перемешайте.' + CHAR(10) +
        N'5. Тушите под крышкой до готовности гречки.' + CHAR(10) +
        N'6. Дайте блюду постоять 5 минут и подавайте.');

INSERT INTO @SeedInstructions (Title, Instructions)
VALUES (N'Запечённые перцы с фаршем', N'1. Срежьте верхушки перцев и удалите семена.' + CHAR(10) +
        N'2. Смешайте фарш с рисом, луком и специями.' + CHAR(10) +
        N'3. Наполните перцы начинкой, не утрамбовывая слишком плотно.' + CHAR(10) +
        N'4. Выложите перцы в форму и залейте томатным соусом.' + CHAR(10) +
        N'5. Запекайте при 180 градусах до мягкости перцев.' + CHAR(10) +
        N'6. Подавайте горячими с соусом из формы.');

INSERT INTO @SeedInstructions (Title, Instructions)
VALUES (N'Куриное филе в сливочном соусе', N'1. Нарежьте куриное филе кусочками или пластинами.' + CHAR(10) +
        N'2. Обжарьте курицу на масле до лёгкой корочки.' + CHAR(10) +
        N'3. Добавьте измельчённый чеснок и прогрейте 30 секунд.' + CHAR(10) +
        N'4. Влейте сливки, посолите и убавьте огонь.' + CHAR(10) +
        N'5. Тушите до загустения соуса и готовности курицы.' + CHAR(10) +
        N'6. Посыпьте зеленью и подавайте горячим.');

INSERT INTO @SeedInstructions (Title, Instructions)
VALUES (N'Тефтели в томатном соусе', N'1. Смешайте фарш с рисом, яйцом, луком и специями.' + CHAR(10) +
        N'2. Сформируйте небольшие тефтели.' + CHAR(10) +
        N'3. Быстро обжарьте тефтели до лёгкой корочки.' + CHAR(10) +
        N'4. Залейте томатным соусом и убавьте огонь.' + CHAR(10) +
        N'5. Тушите под крышкой до готовности риса и мяса.' + CHAR(10) +
        N'6. Подавайте с гарниром или свежими овощами.');

INSERT INTO @SeedInstructions (Title, Instructions)
VALUES (N'Рагу из овощей и курицы', N'1. Нарежьте курицу, картофель, кабачок, морковь и лук.' + CHAR(10) +
        N'2. Обжарьте курицу до лёгкой корочки.' + CHAR(10) +
        N'3. Добавьте лук и морковь, затем картофель и кабачок.' + CHAR(10) +
        N'4. Влейте немного воды, посолите и накройте крышкой.' + CHAR(10) +
        N'5. Тушите до мягкости овощей и готовности курицы.' + CHAR(10) +
        N'6. Перемешайте и подавайте горячим.');

INSERT INTO @SeedInstructions (Title, Instructions)
VALUES (N'Свинина с рисом и овощами', N'1. Нарежьте свинину небольшими кусочками.' + CHAR(10) +
        N'2. Обжарьте мясо до румяности, добавьте морковь и перец.' + CHAR(10) +
        N'3. Всыпьте промытый рис и перемешайте.' + CHAR(10) +
        N'4. Добавьте соевый соус и горячую воду.' + CHAR(10) +
        N'5. Готовьте под крышкой до мягкости риса.' + CHAR(10) +
        N'6. Дайте блюду постоять 5 минут и подавайте.');

INSERT INTO @SeedInstructions (Title, Instructions)
VALUES (N'Лазанья домашняя', N'1. Обжарьте фарш, добавьте томатный соус и тушите мясной соус до густоты.' + CHAR(10) +
        N'2. Приготовьте бешамель из молока, масла и муки или используйте готовый соус.' + CHAR(10) +
        N'3. В форму выложите немного соуса, затем листы лазаньи.' + CHAR(10) +
        N'4. Чередуйте листы, мясной соус, бешамель и сыр.' + CHAR(10) +
        N'5. Завершите слоем соуса и сыра.' + CHAR(10) +
        N'6. Запекайте при 180 градусах 35-45 минут.' + CHAR(10) +
        N'7. Дайте лазанье постоять 10 минут перед нарезкой.');

INSERT INTO @SeedInstructions (Title, Instructions)
VALUES (N'Цезарь с курицей', N'1. Обжарьте куриное филе до готовности и нарежьте ломтиками.' + CHAR(10) +
        N'2. Нарвите салат ромэн крупными кусочками.' + CHAR(10) +
        N'3. Добавьте сухарики, курицу и тёртый пармезан.' + CHAR(10) +
        N'4. Заправьте соусом цезарь и аккуратно перемешайте.' + CHAR(10) +
        N'5. Подавайте сразу, чтобы сухарики остались хрустящими.');

INSERT INTO @SeedInstructions (Title, Instructions)
VALUES (N'Греческий салат', N'1. Нарежьте томаты, огурцы и фету крупными кусочками.' + CHAR(10) +
        N'2. Добавьте маслины в салатник.' + CHAR(10) +
        N'3. Заправьте оливковым маслом, посолите и посыпьте орегано.' + CHAR(10) +
        N'4. Аккуратно перемешайте, чтобы не раскрошить фету.' + CHAR(10) +
        N'5. Подавайте свежим сразу после приготовления.');

INSERT INTO @SeedInstructions (Title, Instructions)
VALUES (N'Салат с тунцом и фасолью', N'1. Слейте жидкость с тунца и фасоли.' + CHAR(10) +
        N'2. Нарежьте красный лук тонкими полукольцами.' + CHAR(10) +
        N'3. Смешайте фасоль, тунец, лук и зелень.' + CHAR(10) +
        N'4. Заправьте лимонным соком и маслом.' + CHAR(10) +
        N'5. Перемешайте и подавайте охлаждённым или комнатной температуры.');

INSERT INTO @SeedInstructions (Title, Instructions)
VALUES (N'Винегрет', N'1. Отварите свёклу, картофель и морковь до мягкости.' + CHAR(10) +
        N'2. Остудите овощи, очистите и нарежьте кубиками.' + CHAR(10) +
        N'3. Нарежьте солёные огурцы.' + CHAR(10) +
        N'4. Смешайте овощи, огурцы и горошек.' + CHAR(10) +
        N'5. Заправьте маслом, посолите и перемешайте.' + CHAR(10) +
        N'6. Дайте винегрету немного настояться перед подачей.');

INSERT INTO @SeedInstructions (Title, Instructions)
VALUES (N'Салат с курицей и ананасом', N'1. Отварите курицу до готовности и остудите.' + CHAR(10) +
        N'2. Нарежьте курицу, ананас и сыр небольшими кусочками.' + CHAR(10) +
        N'3. Добавьте кукурузу.' + CHAR(10) +
        N'4. Заправьте йогуртом и аккуратно перемешайте.' + CHAR(10) +
        N'5. Охладите салат 10-15 минут перед подачей.');

INSERT INTO @SeedInstructions (Title, Instructions)
VALUES (N'Тёплый салат с баклажанами', N'1. Нарежьте баклажаны кубиками или ломтиками.' + CHAR(10) +
        N'2. Обжарьте баклажаны на масле до мягкости.' + CHAR(10) +
        N'3. Добавьте нарезанные томаты и измельчённый чеснок.' + CHAR(10) +
        N'4. Прогрейте всё вместе 2-3 минуты.' + CHAR(10) +
        N'5. Посыпьте кинзой и подавайте салат тёплым.');

INSERT INTO @SeedInstructions (Title, Instructions)
VALUES (N'Тирамису без выпечки', N'1. Взбейте сливки с маскарпоне до устойчивого крема.' + CHAR(10) +
        N'2. Заварите крепкий кофе и остудите его.' + CHAR(10) +
        N'3. Быстро окунайте савоярди в кофе и выкладывайте первым слоем.' + CHAR(10) +
        N'4. Покройте печенье кремом, затем повторите слои.' + CHAR(10) +
        N'5. Посыпьте верх какао.' + CHAR(10) +
        N'6. Охладите десерт несколько часов перед подачей.');

INSERT INTO @SeedInstructions (Title, Instructions)
VALUES (N'Шоколадный брауни', N'1. Растопите шоколад с маслом на водяной бане или короткими импульсами в микроволновке.' + CHAR(10) +
        N'2. Взбейте яйца с сахаром до лёгкой пены.' + CHAR(10) +
        N'3. Соедините шоколадную массу с яйцами.' + CHAR(10) +
        N'4. Добавьте муку и перемешайте до однородности.' + CHAR(10) +
        N'5. Вылейте тесто в форму и выпекайте при 180 градусах.' + CHAR(10) +
        N'6. Достаньте брауни, когда середина остаётся слегка влажной.');

INSERT INTO @SeedInstructions (Title, Instructions)
VALUES (N'Панна-котта с ягодами', N'1. Замочите желатин по инструкции на упаковке.' + CHAR(10) +
        N'2. Прогрейте сливки с сахаром и ванилью, не доводя до кипения.' + CHAR(10) +
        N'3. Добавьте желатин и размешайте до растворения.' + CHAR(10) +
        N'4. Разлейте смесь по формам.' + CHAR(10) +
        N'5. Охладите до застывания.' + CHAR(10) +
        N'6. Подавайте с ягодами или ягодным соусом.');

INSERT INTO @SeedInstructions (Title, Instructions)
VALUES (N'Морковный кекс', N'1. Натрите морковь на мелкой или средней тёрке.' + CHAR(10) +
        N'2. Смешайте сухие ингредиенты с корицей.' + CHAR(10) +
        N'3. Отдельно смешайте яйца с влажными ингредиентами.' + CHAR(10) +
        N'4. Соедините смеси, добавьте морковь и орехи.' + CHAR(10) +
        N'5. Выложите тесто в форму и выпекайте до сухой шпажки.' + CHAR(10) +
        N'6. Остудите кекс перед нарезкой.');

INSERT INTO @SeedInstructions (Title, Instructions)
VALUES (N'Медовик ленивый', N'1. Смешайте мёд, яйца, сахар и муку до густого теста.' + CHAR(10) +
        N'2. Испеките медовый корж до золотистого цвета.' + CHAR(10) +
        N'3. Остудите корж и нарежьте его пластами или кусочками.' + CHAR(10) +
        N'4. Взбейте сметану с сахаром для крема.' + CHAR(10) +
        N'5. Прослоите коржи кремом.' + CHAR(10) +
        N'6. Охладите торт несколько часов, чтобы он пропитался.');

INSERT INTO @SeedInstructions (Title, Instructions)
VALUES (N'Творожная запеканка', N'1. Смешайте творог, яйца, манку и сахар.' + CHAR(10) +
        N'2. Добавьте изюм и перемешайте.' + CHAR(10) +
        N'3. Оставьте массу на 10 минут, чтобы манка набухла.' + CHAR(10) +
        N'4. Выложите творожную массу в форму.' + CHAR(10) +
        N'5. Запекайте при 180 градусах до румяной корочки.' + CHAR(10) +
        N'6. Подавайте тёплой или охлаждённой.');

INSERT INTO @SeedInstructions (Title, Instructions)
VALUES (N'Спагетти карбонара', N'1. Отварите спагетти до состояния аль денте.' + CHAR(10) +
        N'2. Нарежьте бекон и обжарьте до румяности.' + CHAR(10) +
        N'3. Смешайте яйца с тёртым пармезаном и перцем.' + CHAR(10) +
        N'4. Горячую пасту переложите к бекону.' + CHAR(10) +
        N'5. Снимите с огня, добавьте яичную смесь и быстро перемешайте.' + CHAR(10) +
        N'6. Подавайте сразу, пока соус кремовый.');

INSERT INTO @SeedInstructions (Title, Instructions)
VALUES (N'Плов с курицей', N'1. Нарежьте курицу, лук и морковь.' + CHAR(10) +
        N'2. Обжарьте курицу до румяности, добавьте овощи.' + CHAR(10) +
        N'3. Всыпьте промытый рис и зиру.' + CHAR(10) +
        N'4. Влейте горячую воду, чтобы она покрывала рис.' + CHAR(10) +
        N'5. Готовьте под крышкой до впитывания жидкости.' + CHAR(10) +
        N'6. Дайте плову постоять и аккуратно перемешайте.');

INSERT INTO @SeedInstructions (Title, Instructions)
VALUES (N'Ризотто с грибами', N'1. Обжарьте лук и грибы до мягкости.' + CHAR(10) +
        N'2. Добавьте рис арборио и прогрейте его 1-2 минуты.' + CHAR(10) +
        N'3. Вливайте горячий бульон небольшими порциями.' + CHAR(10) +
        N'4. Постоянно помешивайте, добавляя бульон по мере впитывания.' + CHAR(10) +
        N'5. Когда рис станет кремовым и мягким с лёгкой плотностью в центре, добавьте сыр.' + CHAR(10) +
        N'6. Перемешайте и подавайте сразу.');

INSERT INTO @SeedInstructions (Title, Instructions)
VALUES (N'Лапша удон с овощами', N'1. Нарежьте перец и морковь тонкой соломкой.' + CHAR(10) +
        N'2. Быстро обжарьте овощи на сильном огне.' + CHAR(10) +
        N'3. Добавьте удон и соевый соус.' + CHAR(10) +
        N'4. Прогрейте лапшу, постоянно перемешивая.' + CHAR(10) +
        N'5. Посыпьте кунжутом.' + CHAR(10) +
        N'6. Подавайте горячей сразу после приготовления.');

INSERT INTO @SeedInstructions (Title, Instructions)
VALUES (N'Кускус с овощами', N'1. Залейте кускус кипятком, накройте и оставьте набухать.' + CHAR(10) +
        N'2. Нарежьте кабачок, перец и томаты.' + CHAR(10) +
        N'3. Обжарьте овощи до мягкости.' + CHAR(10) +
        N'4. Разрыхлите кускус вилкой.' + CHAR(10) +
        N'5. Смешайте кускус с овощами и зеленью.' + CHAR(10) +
        N'6. Подавайте тёплым или комнатной температуры.');

INSERT INTO @SeedInstructions (Title, Instructions)
VALUES (N'Хачапури по-аджарски', N'1. Замесите тесто из муки, дрожжей, воды, соли и небольшого количества масла.' + CHAR(10) +
        N'2. Дайте тесту подняться в тёплом месте.' + CHAR(10) +
        N'3. Разделите тесто на части и сформируйте лодочки с бортиками.' + CHAR(10) +
        N'4. Наполните лодочки тёртым сулугуни.' + CHAR(10) +
        N'5. Выпекайте до румяного теста и расплавленного сыра.' + CHAR(10) +
        N'6. Вбейте в каждую лодочку яйцо и верните в духовку на 2-3 минуты.' + CHAR(10) +
        N'7. Добавьте кусочек масла и подавайте горячими.');

INSERT INTO @SeedInstructions (Title, Instructions)
VALUES (N'Киш с курицей и грибами', N'1. Раскатайте тесто и выложите его в форму с бортиками.' + CHAR(10) +
        N'2. Нарежьте курицу и грибы, обжарьте до готовности.' + CHAR(10) +
        N'3. Смешайте яйца со сливками, посолите.' + CHAR(10) +
        N'4. Выложите начинку на основу.' + CHAR(10) +
        N'5. Залейте сливочно-яичной смесью.' + CHAR(10) +
        N'6. Выпекайте киш до плотной начинки и румяного края.');

INSERT INTO @SeedInstructions (Title, Instructions)
VALUES (N'Домашний хлеб', N'1. Смешайте муку, дрожжи, соль и сахар.' + CHAR(10) +
        N'2. Влейте тёплую воду и замесите мягкое тесто.' + CHAR(10) +
        N'3. Накройте тесто и оставьте подниматься до увеличения в объёме.' + CHAR(10) +
        N'4. Обомните тесто и сформируйте буханку.' + CHAR(10) +
        N'5. Дайте заготовке подняться повторно.' + CHAR(10) +
        N'6. Выпекайте хлеб при 200 градусах до румяной корки.' + CHAR(10) +
        N'7. Остудите на решётке перед нарезкой.');

INSERT INTO @SeedInstructions (Title, Instructions)
VALUES (N'Пирожки с капустой', N'1. Замесите дрожжевое тесто и оставьте его подниматься.' + CHAR(10) +
        N'2. Нашинкуйте капусту и потушите её с луком до мягкости.' + CHAR(10) +
        N'3. Остудите начинку.' + CHAR(10) +
        N'4. Разделите тесто на кусочки, раскатайте лепёшки.' + CHAR(10) +
        N'5. Выложите начинку и защипните пирожки.' + CHAR(10) +
        N'6. Дайте пирожкам немного подойти.' + CHAR(10) +
        N'7. Выпекайте до румяного цвета.');

INSERT INTO @SeedInstructions (Title, Instructions)
VALUES (N'Смузи с бананом и шпинатом', N'1. Очистите банан и нарежьте его кусочками.' + CHAR(10) +
        N'2. Промойте шпинат.' + CHAR(10) +
        N'3. Сложите банан, шпинат, йогурт, мёд и воду в блендер.' + CHAR(10) +
        N'4. Взбейте до гладкости.' + CHAR(10) +
        N'5. Попробуйте и при необходимости добавьте немного воды.' + CHAR(10) +
        N'6. Подавайте сразу после приготовления.');

INSERT INTO @SeedInstructions (Title, Instructions)
VALUES (N'Морс из клюквы', N'1. Разомните клюкву и отожмите сок.' + CHAR(10) +
        N'2. Жмых залейте водой и проварите 10 минут.' + CHAR(10) +
        N'3. Процедите отвар.' + CHAR(10) +
        N'4. Добавьте сахар и перемешайте до растворения.' + CHAR(10) +
        N'5. Влейте клюквенный сок и добавьте лимон.' + CHAR(10) +
        N'6. Охладите морс перед подачей.');

INSERT INTO @SeedInstructions (Title, Instructions)
VALUES (N'Имбирный чай с лимоном', N'1. Нарежьте имбирь тонкими ломтиками.' + CHAR(10) +
        N'2. Залейте имбирь кипятком.' + CHAR(10) +
        N'3. Дайте настояться 5-7 минут.' + CHAR(10) +
        N'4. Добавьте лимон и мёд.' + CHAR(10) +
        N'5. Перемешайте и подавайте тёплым.');

INSERT INTO @SeedInstructions (Title, Instructions)
VALUES (N'Хумус с питой', N'1. Слейте жидкость с нута, часть оставьте для регулировки густоты.' + CHAR(10) +
        N'2. Сложите нут, тахини, чеснок и лимонный сок в блендер.' + CHAR(10) +
        N'3. Взбейте до гладкой пасты, добавляя немного жидкости при необходимости.' + CHAR(10) +
        N'4. Добавьте масло и соль, снова перемешайте.' + CHAR(10) +
        N'5. Подогрейте или подсушите питу.' + CHAR(10) +
        N'6. Подавайте хумус с питой.');

INSERT INTO @SeedInstructions (Title, Instructions)
VALUES (N'Рулетики из лаваша', N'1. Разложите лаваш на рабочей поверхности.' + CHAR(10) +
        N'2. Смажьте его творожным сыром.' + CHAR(10) +
        N'3. Выложите ветчину, зелень и тонко нарезанный огурец.' + CHAR(10) +
        N'4. Плотно сверните лаваш рулетом.' + CHAR(10) +
        N'5. Охладите 10 минут, чтобы рулет держал форму.' + CHAR(10) +
        N'6. Нарежьте порционными кусочками.');

INSERT INTO @SeedInstructions (Title, Instructions)
VALUES (N'Сырная тарелка с орехами', N'1. Нарежьте твёрдый и мягкий сыр удобными кусочками.' + CHAR(10) +
        N'2. Выложите сыры на тарелку, оставляя место для орехов и винограда.' + CHAR(10) +
        N'3. Добавьте орехи, виноград и мёд в небольшой соусник или прямо на тарелку.' + CHAR(10) +
        N'4. Охладите 10 минут и подавайте как закуску.');

UPDATE r
SET r.Instructions = si.Instructions
FROM dbo.Recipes r
JOIN @SeedInstructions si ON si.Title = r.Title
WHERE r.UserId IS NULL;



INSERT INTO dbo.Ingredients (RecipeId, Name, Amount)
SELECT r.Id,
       LOWER(LTRIM(RTRIM(LEFT(parts.Line, CHARINDEX(N' - ', parts.Line + N' - ') - 1)))) AS Name,
       LTRIM(RTRIM(SUBSTRING(parts.Line, CHARINDEX(N' - ', parts.Line + N' - ') + 3, 4000))) AS Amount
FROM @ExpandedRecipes er
JOIN dbo.Recipes r ON r.Title = er.Title
CROSS APPLY
(
    SELECT LTRIM(RTRIM(value)) AS Line
    FROM STRING_SPLIT(er.IngredientsText, CHAR(10))
    WHERE LTRIM(RTRIM(value)) <> N''
) parts
WHERE CHARINDEX(N' - ', parts.Line + N' - ') > 1
  AND NOT EXISTS
  (
      SELECT 1
      FROM dbo.Ingredients i
      WHERE i.RecipeId = r.Id
        AND i.Name = LOWER(LTRIM(RTRIM(LEFT(parts.Line, CHARINDEX(N' - ', parts.Line + N' - ') - 1))))
  );
GO

INSERT INTO dbo.RecipeStats (RecipeId, CookCount, Rating)
SELECT Id, ABS(CHECKSUM(NEWID())) % 20 + 1, 4 + ABS(CHECKSUM(NEWID())) % 2
FROM dbo.Recipes;
GO

SELECT 'RecipeKeeperDb создана для SSMS' AS Result;
GO
