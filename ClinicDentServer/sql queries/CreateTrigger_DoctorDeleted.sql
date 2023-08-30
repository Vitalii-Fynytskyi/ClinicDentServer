CREATE TRIGGER DoctorDeleted
ON Doctors
INSTEAD OF DELETE
AS
BEGIN
    DELETE FROM Schedules
    WHERE DoctorId IN (SELECT Id FROM deleted);

    DELETE FROM Stages
    WHERE DoctorId IN (SELECT Id FROM deleted);

	UPDATE Images SET DoctorId=NULL
    WHERE DoctorId IN (SELECT Id FROM deleted);


    DELETE FROM Doctors
    WHERE Id IN (SELECT Id FROM deleted);
END;