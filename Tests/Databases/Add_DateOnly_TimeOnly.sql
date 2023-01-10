USE [NorthwindIB]
GO

BEGIN TRANSACTION
GO
ALTER TABLE [dbo].[UnusualDate] ADD
	[DateOnly] [date] NULL,
	[TimeOnly] [time](7) NULL
GO
COMMIT
GO
