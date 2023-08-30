CREATE TRIGGER PatientDeleted
ON Patients
INSTEAD OF DELETE
AS
BEGIN
    -- Deleting related records from Schedules

    DELETE FROM Stages
    WHERE PatientId IN (SELECT Id FROM deleted);

    DELETE FROM Schedules
    WHERE PatientId IN (SELECT Id FROM deleted);

    -- Now, deleting the actual records from Patients
    DELETE FROM Patients
    WHERE Id IN (SELECT Id FROM deleted);
END;