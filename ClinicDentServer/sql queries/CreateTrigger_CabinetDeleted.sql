CREATE TRIGGER CabinetDeleted
ON Cabinets
INSTEAD OF DELETE
AS
BEGIN
    DELETE FROM Schedules
    WHERE CabinetId IN (SELECT Id FROM deleted);

	DELETE FROM CabinetComments
    WHERE CabinetId IN (SELECT Id FROM deleted);

    DELETE FROM Cabinets
    WHERE Id IN (SELECT Id FROM deleted);
END;