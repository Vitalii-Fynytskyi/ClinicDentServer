CREATE TRIGGER StageAssetDeleted
ON StageAssets
INSTEAD OF DELETE
AS
BEGIN
    -- Updating related records in Stages
    UPDATE Stages
    SET OperationId = NULL
    WHERE Id IN (SELECT Id FROM deleted);
    
    UPDATE Stages
    SET BondId = NULL
    WHERE Id IN (SELECT Id FROM deleted);

    UPDATE Stages
    SET DentinId = NULL
    WHERE Id IN (SELECT Id FROM deleted);
    
    UPDATE Stages
    SET EnamelId = NULL
    WHERE Id IN (SELECT Id FROM deleted);

    UPDATE Stages
    SET CanalMethodId = NULL
    WHERE Id IN (SELECT Id FROM deleted);

    UPDATE Stages
    SET SealerId = NULL
    WHERE Id IN (SELECT Id FROM deleted);

    UPDATE Stages
    SET CalciumId = NULL
    WHERE Id IN (SELECT Id FROM deleted);

    UPDATE Stages
    SET CementId = NULL
    WHERE Id IN (SELECT Id FROM deleted);

    UPDATE Stages
    SET TechnicianId = NULL
    WHERE Id IN (SELECT Id FROM deleted);

    UPDATE Stages
    SET PinId = NULL
    WHERE Id IN (SELECT Id FROM deleted);

    -- Now, deleting the actual records from StageAssets
    DELETE FROM StageAssets
    WHERE Id IN (SELECT Id FROM deleted);
END;