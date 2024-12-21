USE QuickTechSystem;

-- Categories
INSERT INTO Categories (Name, Description, IsActive) VALUES
('Electronics', 'Electronic devices and accessories', 1),
('Groceries', 'Food and household items', 1),
('Stationery', 'Office and school supplies', 1),
('Beverages', 'Drinks and liquids', 1),
('Hardware', 'Tools and equipment', 1);

-- Suppliers
INSERT INTO Suppliers (Name, ContactPerson, Phone, Email, Address, Balance, TaxNumber, IsActive, CreatedAt) VALUES
('Tech Supplies Inc', 'John Smith', '555-0101', 'john@techsupplies.com', '123 Tech St', 0.00, 'TAX001', 1, GETDATE()),
('Global Foods', 'Mary Johnson', '555-0102', 'mary@globalfoods.com', '456 Food Ave', 0.00, 'TAX002', 1, GETDATE()),
('Office Plus', 'Robert Brown', '555-0103', 'robert@officeplus.com', '789 Office Rd', 0.00, 'TAX003', 1, GETDATE());

-- Products
INSERT INTO Products (Barcode, Name, Description, CategoryId, SupplierId, PurchasePrice, SalePrice, CurrentStock, MinimumStock, IsActive, CreatedAt) VALUES
('1001', 'USB Flash Drive 32GB', 'High-speed USB 3.0 flash drive', 1, 1, 8.00, 15.99, 50, 10, 1, GETDATE()),
('1002', 'Whole Wheat Bread', 'Fresh baked daily', 2, 2, 1.50, 3.99, 30, 5, 1, GETDATE()),
('1003', 'Ballpoint Pen Pack', 'Pack of 10 blue pens', 3, 3, 2.00, 4.99, 100, 20, 1, GETDATE()),
('1004', 'Mineral Water 1L', 'Natural spring water', 4, 2, 0.50, 1.99, 200, 50, 1, GETDATE()),
('1005', 'Screwdriver Set', 'Set of 6 precision screwdrivers', 5, 1, 5.00, 12.99, 25, 5, 1, GETDATE());

-- Customers
INSERT INTO Customers (Name, Phone, Email, Address, Balance, IsActive, CreatedAt) VALUES
('Alice Wilson', '555-0201', 'alice@email.com', '321 Customer St', 0.00, 1, GETDATE()),
('Bob Anderson', '555-0202', 'bob@email.com', '654 Client Ave', 0.00, 1, GETDATE()),
('Carol Martinez', '555-0203', 'carol@email.com', '987 Buyer Rd', 0.00, 1, GETDATE());

-- Business Settings
INSERT INTO BusinessSettings ([Key], Value, Description, [Group], DataType, IsSystem, LastModified, ModifiedBy) VALUES
('CompanyName', 'QuickTech Systems', 'Company name for receipts', 'General', 'string', 1, GETDATE(), 'System'),
('Currency', 'USD', 'Default currency', 'Financial', 'string', 1, GETDATE(), 'System'),
('TaxRate', '0.15', 'Default tax rate', 'Financial', 'decimal', 1, GETDATE(), 'System');

-- System Preferences
INSERT INTO SystemPreferences (UserId, PreferenceKey, PreferenceValue, LastModified) VALUES
('default', 'Theme', 'Light', GETDATE()),
('default', 'Language', 'en-US', GETDATE()),
('default', 'DateFormat', 'MM/dd/yyyy', GETDATE());

-- Sample Transactions
INSERT INTO Transactions (CustomerId, TotalAmount, PaidAmount, Balance, TransactionDate, TransactionType, Status) VALUES
(1, 31.98, 31.98, 0.00, DATEADD(day, -1, GETDATE()), 0, 1), -- Sale, Completed
(2, 19.96, 19.96, 0.00, DATEADD(day, -2, GETDATE()), 0, 1); -- Sale, Completed

-- Transaction Details for the above transactions
INSERT INTO TransactionDetails (TransactionId, ProductId, Quantity, UnitPrice, Discount, Total) VALUES
(1, 1, 2, 15.99, 0.00, 31.98), -- 2 USB drives
(2, 4, 10, 1.99, 0.00, 19.96); -- 10 water bottles

-- Supplier Transactions
INSERT INTO SupplierTransactions (SupplierId, Amount, TransactionType, Reference, Notes, TransactionDate) VALUES
(1, -1000.00, 'Purchase', 'PO-001', 'Initial stock purchase', DATEADD(day, -5, GETDATE())),
(2, -500.00, 'Purchase', 'PO-002', 'Grocery items purchase', DATEADD(day, -4, GETDATE()));

-- Inventory History
INSERT INTO InventoryHistories (ProductId, QuantityChanged, OperationType, Reference, Date, Notes) VALUES
(1, 50, 2, 'PO-001', DATEADD(day, -5, GETDATE()), 'Initial stock'),
(2, 30, 2, 'PO-002', DATEADD(day, -4, GETDATE()), 'Initial stock');