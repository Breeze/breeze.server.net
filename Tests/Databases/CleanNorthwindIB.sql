use NorthwindIB
go
delete from InternationalOrder  where OrderID in (select [Order].OrderID from [Order] where CustomerID is null or EmployeeID is null)
go
delete from OrderDetail where OrderID in (select [Order].OrderID from [Order] where CustomerID is null or EmployeeID is null)
go
delete from [Order] where CustomerID is null or EmployeeID is null or ShipAddress like 'Test%'
go
update [Order] set CustomerID = 'FBCF888C-7EE3-4B00-980E-373CE8B7817D' where ShipName = 'Wartian Herkku'
go
delete from Employee where LastName like 'Test%' or LastName like '''%'
go
delete from Customer where CompanyName like 'Test%' or CompanyName like '''%' or CompanyName like 'error%'
go
delete from Territory where TerritoryDescription like 'Test%'
go
delete from Supplier where CompanyName like 'Test%' or CompanyName like '''%'
go
delete from Region where RegionDescription like 'Test%'
go
delete from Role where Name like 'Test%'
go
delete from TimeLimit where Id > 50
go
