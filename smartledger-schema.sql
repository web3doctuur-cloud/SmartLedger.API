
-- SmartLedger Complete Database Schema for PostgreSQL/Neon
-- Generated from ApplicationDbContext and Models
-- Includes all Identity tables and custom application tables

-- Enable UUID extensions if needed
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- =============================================
-- IDENTITY TABLES (from ASP.NET Core Identity)
-- =============================================

-- AspNetRoles
CREATE TABLE "AspNetRoles" (
    "Id" TEXT NOT NULL,
    "Name" VARCHAR(256),
    "NormalizedName" VARCHAR(256),
    "ConcurrencyStamp" TEXT,
    CONSTRAINT "PK_AspNetRoles" PRIMARY KEY ("Id")
);

-- AspNetUsers
CREATE TABLE "AspNetUsers" (
    "Id" TEXT NOT NULL,
    "UserName" VARCHAR(256),
    "NormalizedUserName" VARCHAR(256),
    "Email" VARCHAR(256),
    "NormalizedEmail" VARCHAR(256),
    "EmailConfirmed" BOOLEAN NOT NULL DEFAULT FALSE,
    "PasswordHash" TEXT,
    "SecurityStamp" TEXT,
    "ConcurrencyStamp" TEXT,
    "PhoneNumber" TEXT,
    "PhoneNumberConfirmed" BOOLEAN NOT NULL DEFAULT FALSE,
    "TwoFactorEnabled" BOOLEAN NOT NULL DEFAULT FALSE,
    "LockoutEnd" TIMESTAMP WITH TIME ZONE,
    "LockoutEnabled" BOOLEAN NOT NULL DEFAULT FALSE,
    "AccessFailedCount" INTEGER NOT NULL DEFAULT 0,
    CONSTRAINT "PK_AspNetUsers" PRIMARY KEY ("Id")
);

-- AspNetRoleClaims
CREATE TABLE "AspNetRoleClaims" (
    "Id" SERIAL NOT NULL,
    "RoleId" TEXT NOT NULL,
    "ClaimType" TEXT,
    "ClaimValue" TEXT,
    CONSTRAINT "PK_AspNetRoleClaims" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_AspNetRoleClaims_AspNetRoles_RoleId" FOREIGN KEY ("RoleId") REFERENCES "AspNetRoles" ("Id") ON DELETE CASCADE
);

-- AspNetUserClaims
CREATE TABLE "AspNetUserClaims" (
    "Id" SERIAL NOT NULL,
    "UserId" TEXT NOT NULL,
    "ClaimType" TEXT,
    "ClaimValue" TEXT,
    CONSTRAINT "PK_AspNetUserClaims" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_AspNetUserClaims_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
);

-- AspNetUserLogins
CREATE TABLE "AspNetUserLogins" (
    "LoginProvider" TEXT NOT NULL,
    "ProviderKey" TEXT NOT NULL,
    "ProviderDisplayName" TEXT,
    "UserId" TEXT NOT NULL,
    CONSTRAINT "PK_AspNetUserLogins" PRIMARY KEY ("LoginProvider", "ProviderKey"),
    CONSTRAINT "FK_AspNetUserLogins_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
);

-- AspNetUserRoles
CREATE TABLE "AspNetUserRoles" (
    "UserId" TEXT NOT NULL,
    "RoleId" TEXT NOT NULL,
    CONSTRAINT "PK_AspNetUserRoles" PRIMARY KEY ("UserId", "RoleId"),
    CONSTRAINT "FK_AspNetUserRoles_AspNetRoles_RoleId" FOREIGN KEY ("RoleId") REFERENCES "AspNetRoles" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_AspNetUserRoles_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
);

-- AspNetUserTokens
CREATE TABLE "AspNetUserTokens" (
    "UserId" TEXT NOT NULL,
    "LoginProvider" TEXT NOT NULL,
    "Name" TEXT NOT NULL,
    "Value" TEXT,
    CONSTRAINT "PK_AspNetUserTokens" PRIMARY KEY ("UserId", "LoginProvider", "Name"),
    CONSTRAINT "FK_AspNetUserTokens_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
);

-- =============================================
-- CUSTOM APPLICATION TABLES
-- =============================================

-- Accounts (Chart of Accounts)
CREATE TABLE "Accounts" (
    "Id" SERIAL NOT NULL,
    "AccountCode" VARCHAR(20) NOT NULL,
    "Name" VARCHAR(100) NOT NULL,
    "Type" VARCHAR(20) NOT NULL CHECK ("Type" IN ('Asset', 'Liability', 'Equity', 'Income', 'Expense')),
    "NormalSide" VARCHAR(6) NOT NULL CHECK ("NormalSide" IN ('DEBIT', 'CREDIT')),
    "ParentAccountId" INTEGER,
    "Balance" NUMERIC(18,2) NOT NULL DEFAULT 0,
    "UserId" TEXT NOT NULL,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    CONSTRAINT "PK_Accounts" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Accounts_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
);

-- JournalEntries
CREATE TABLE "JournalEntries" (
    "Id" SERIAL NOT NULL,
    "EntryNumber" VARCHAR(20) NOT NULL,
    "EntryDate" TIMESTAMP WITH TIME ZONE NOT NULL,
    "Description" VARCHAR(500),
    "UserId" TEXT NOT NULL,
    "IsApproved" BOOLEAN NOT NULL DEFAULT FALSE,
    "ApprovedAt" TIMESTAMP WITH TIME ZONE,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    CONSTRAINT "PK_JournalEntries" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_JournalEntries_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
);

-- JournalEntryLines
CREATE TABLE "JournalEntryLines" (
    "Id" SERIAL NOT NULL,
    "JournalEntryId" INTEGER NOT NULL,
    "AccountId" INTEGER NOT NULL,
    "Debit" NUMERIC NOT NULL DEFAULT 0,
    "Credit" NUMERIC NOT NULL DEFAULT 0,
    "LineDescription" VARCHAR(500),
    "ReferenceNumber" VARCHAR(50),
    "TaxAmount" NUMERIC NOT NULL DEFAULT 0,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    CONSTRAINT "PK_JournalEntryLines" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_JournalEntryLines_Accounts_AccountId" FOREIGN KEY ("AccountId") REFERENCES "Accounts" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_JournalEntryLines_JournalEntries_JournalEntryId" FOREIGN KEY ("JournalEntryId") REFERENCES "JournalEntries" ("Id") ON DELETE CASCADE
);

-- Products
CREATE TABLE "Products" (
    "Id" SERIAL NOT NULL,
    "Name" VARCHAR(100) NOT NULL,
    "Description" VARCHAR(500),
    "Category" VARCHAR(50),
    "Quantity" INTEGER NOT NULL DEFAULT 0,
    "CostPrice" NUMERIC(18,2) NOT NULL,
    "SellingPrice" NUMERIC(18,2) NOT NULL,
    "LowStockThreshold" INTEGER NOT NULL DEFAULT 10,
    "ImageUrl" TEXT,
    "SKU" VARCHAR(50),
    "UserId" TEXT NOT NULL,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    CONSTRAINT "PK_Products" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Products_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
);

-- InventoryTransactions
CREATE TABLE "InventoryTransactions" (
    "Id" SERIAL NOT NULL,
    "ProductId" INTEGER NOT NULL,
    "TransactionType" VARCHAR(20) NOT NULL CHECK ("TransactionType" IN ('PURCHASE', 'SALE', 'ADJUSTMENT')),
    "QuantityChange" INTEGER NOT NULL,
    "PreviousQuantity" INTEGER NOT NULL,
    "NewQuantity" INTEGER NOT NULL,
    "Notes" VARCHAR(500),
    "UnitPrice" NUMERIC(18,2),
    "ReferenceNumber" TEXT,
    "UserId" TEXT NOT NULL,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    CONSTRAINT "PK_InventoryTransactions" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_InventoryTransactions_Products_ProductId" FOREIGN KEY ("ProductId") REFERENCES "Products" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_InventoryTransactions_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
);

-- TodoItems
CREATE TABLE "TodoItems" (
    "Id" SERIAL NOT NULL,
    "Title" VARCHAR(200) NOT NULL,
    "Description" VARCHAR(1000),
    "Priority" VARCHAR(20) NOT NULL DEFAULT 'MEDIUM' CHECK ("Priority" IN ('HIGH', 'MEDIUM', 'LOW')),
    "Status" VARCHAR(20) NOT NULL DEFAULT 'PENDING' CHECK ("Status" IN ('PENDING', 'IN_PROGRESS', 'COMPLETED')),
    "DueDate" TIMESTAMP WITH TIME ZONE,
    "CompletedAt" TIMESTAMP WITH TIME ZONE,
    "UserId" TEXT NOT NULL,
    "RelatedProductId" INTEGER,
    "RelatedInvoiceId" INTEGER,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    CONSTRAINT "PK_TodoItems" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_TodoItems_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_TodoItems_Products_RelatedProductId" FOREIGN KEY ("RelatedProductId") REFERENCES "Products" ("Id") ON DELETE SET NULL
);

-- =============================================
-- INDEXES for Performance
-- =============================================

CREATE INDEX "IX_Accounts_UserId" ON "Accounts" ("UserId");
CREATE INDEX "IX_JournalEntries_UserId" ON "JournalEntries" ("UserId");
CREATE INDEX "IX_JournalEntryLines_JournalEntryId" ON "JournalEntryLines" ("JournalEntryId");
CREATE INDEX "IX_JournalEntryLines_AccountId" ON "JournalEntryLines" ("AccountId");
CREATE INDEX "IX_Products_UserId" ON "Products" ("UserId");
CREATE INDEX "IX_InventoryTransactions_UserId" ON "InventoryTransactions" ("UserId");
CREATE INDEX "IX_InventoryTransactions_ProductId" ON "InventoryTransactions" ("ProductId");
CREATE INDEX "IX_TodoItems_UserId" ON "TodoItems" ("UserId");
CREATE INDEX "EmailIndex" ON "AspNetUsers" ("NormalizedEmail");
CREATE UNIQUE INDEX "UserNameIndex" ON "AspNetUsers" ("NormalizedUserName");
CREATE INDEX "IX_AspNetRoleClaims_RoleId" ON "AspNetRoleClaims" ("RoleId");
CREATE INDEX "IX_AspNetUserClaims_UserId" ON "AspNetUserClaims" ("UserId");
CREATE INDEX "IX_AspNetUserLogins_UserId" ON "AspNetUserLogins" ("UserId");
CREATE INDEX "IX_AspNetUserRoles_RoleId" ON "AspNetUserRoles" ("RoleId");
CREATE UNIQUE INDEX "RoleNameIndex" ON "AspNetRoles" ("NormalizedName");

-- =============================================
-- SEED INITIAL DATA
-- =============================================

-- Seed Admin and User Roles
INSERT INTO "AspNetRoles" ("Id", "Name", "NormalizedName", "ConcurrencyStamp")
VALUES 
    ('user-role-id', 'User', 'USER', 'user-role-concurrency-stamp'),
    ('admin-role-id', 'Admin', 'ADMIN', 'admin-role-concurrency-stamp');

-- Seed Default Chart of Accounts (optional, user-facing)
-- Users can customize, but these are good defaults
INSERT INTO "Accounts" ("Id", "AccountCode", "Name", "Type", "NormalSide", "UserId", "IsActive")
SELECT 
    1, '1000', 'Cash', 'Asset', 'DEBIT', u."Id", TRUE
FROM "AspNetUsers" u
LIMIT 1
ON CONFLICT DO NOTHING;

INSERT INTO "Accounts" ("Id", "AccountCode", "Name", "Type", "NormalSide", "UserId", "IsActive")
SELECT 
    2, '2000', 'Accounts Payable', 'Liability', 'CREDIT', u."Id", TRUE
FROM "AspNetUsers" u
LIMIT 1
ON CONFLICT DO NOTHING;

INSERT INTO "Accounts" ("Id", "AccountCode", "Name", "Type", "NormalSide", "UserId", "IsActive")
SELECT 
    3, '3000', 'Owner''s Equity', 'Equity', 'CREDIT', u."Id", TRUE
FROM "AspNetUsers" u
LIMIT 1
ON CONFLICT DO NOTHING;

INSERT INTO "Accounts" ("Id", "AccountCode", "Name", "Type", "NormalSide", "UserId", "IsActive")
SELECT 
    4, '4000', 'Sales Revenue', 'Income', 'CREDIT', u."Id", TRUE
FROM "AspNetUsers" u
LIMIT 1
ON CONFLICT DO NOTHING;

INSERT INTO "Accounts" ("Id", "AccountCode", "Name", "Type", "NormalSide", "UserId", "IsActive")
SELECT 
    5, '5000', 'Cost of Goods Sold', 'Expense', 'DEBIT', u."Id", TRUE
FROM "AspNetUsers" u
LIMIT 1
ON CONFLICT DO NOTHING;
