use NorthwindIB
go
delete from InternationalOrder  where OrderID in (select [Order].OrderID from [Order] where CustomerID is null or EmployeeID is null)
go
delete from InternationalOrder where CustomsDescription = 'rare, exotic birds'
go
delete from OrderDetail where OrderID in (select [Order].OrderID from [Order] where CustomerID is null or EmployeeID is null)
go
delete from [Order] where CustomerID is null or EmployeeID is null or ShipAddress like 'Test%'
go
delete from [Order] where OrderID > 12000
go
update [Order] set CustomerID = 'FBCF888C-7EE3-4B00-980E-373CE8B7817D' where ShipName = 'Wartian Herkku'
go
delete from Employee where EmployeeID > 10
go
delete from Customer where CompanyName like 'Test%' or CompanyName like '''%' or CompanyName like 'error%'
go
delete from Territory where TerritoryID > 99999
go
delete from Product where ProductID > 77
go
delete from Supplier where SupplierID > 30
go
delete from Region where RegionDescription like 'Test%'
go
delete from Role where Id > 10
go
delete from TimeGroup where Id > 10
go
delete from TimeLimit where Id > 50
go
delete from Geospatial where Id > 10
go
delete from Region where RegionID > 4
go
delete from UnusualDate where Id > 10
go
delete from [User] where Id > 20
go
delete from Comment -- all!
go


