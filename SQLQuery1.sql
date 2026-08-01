CREATE DATABASE LOSTBOOKS;
GO

USE LOSTBOOKS;
GO

CREATE TABLE Consignors (
    ConsignorID INT IDENTITY(1,1) PRIMARY KEY,
    ConsignorName VARCHAR(100) NOT NULL,
    ContactNumber VARCHAR(20) NOT NULL,
    EmailAddress VARCHAR(100) NOT NULL,
    HomeAddress VARCHAR(200) NOT NULL,
    GcashNumber VARCHAR(20) NOT NULL,
    BankName VARCHAR(100),
    BankAccountNumber VARCHAR(30),
    AccountName VARCHAR(100)
);

CREATE TABLE Books (
    BookID INT IDENTITY(1,1) PRIMARY KEY,
    ISBN VARCHAR(20) NOT NULL,
    Title VARCHAR(200) NOT NULL,
    Author VARCHAR(100) NOT NULL,
    Condition VARCHAR(50) NOT NULL,
    Quantity INT NOT NULL,
    SellingPrice DECIMAL(10,2) NOT NULL,
    StoreSharePercentage DECIMAL(5,2) NOT NULL,
    ConsignorID INT NOT NULL,
    FOREIGN KEY (ConsignorID) REFERENCES Consignors(ConsignorID)
);

CREATE TABLE Merchandises (
    MerchandiseID INT IDENTITY(1,1) PRIMARY KEY,
    MerchandiseName VARCHAR(100) NOT NULL,
    Category VARCHAR(50) NOT NULL,
    Quantity INT NOT NULL,
    SellingPrice DECIMAL(10,2) NOT NULL,
    StoreSharePercentage DECIMAL(5,2) NOT NULL,
    ConsignorID INT NOT NULL,
    FOREIGN KEY (ConsignorID) REFERENCES Consignors(ConsignorID)
);

CREATE TABLE Products (
    ProductID INT IDENTITY(1,1) PRIMARY KEY,
    ProductName VARCHAR(100) NOT NULL,
    Category VARCHAR(50) NOT NULL,
    SellingPrice DECIMAL(10,2) NOT NULL
);

CREATE TABLE Services (
    ServiceID INT IDENTITY(1,1) PRIMARY KEY,
    CustomerName VARCHAR(100) NOT NULL,
    ContactNumber VARCHAR(20) NOT NULL,
    ServiceType VARCHAR(50) NOT NULL,
    Size VARCHAR(50) NOT NULL,
    NumberOfPages INT NOT NULL,
    CoverFinish VARCHAR(50) NOT NULL,
    Status VARCHAR(50) NOT NULL,
    AssessedPrice DECIMAL(10,2)
);