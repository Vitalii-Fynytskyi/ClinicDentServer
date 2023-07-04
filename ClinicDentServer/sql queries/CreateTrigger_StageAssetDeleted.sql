CREATE TRIGGER StageAssetDeleted
ON StageAssets
INSTEAD OF DELETE
AS
BEGIN
	DECLARE @Id int
	SELECT @Id=Id from deleted
	UPDATE Stages SET OperationId=NULL WHERE Id=@Id
	UPDATE Stages SET BondId=NULL WHERE Id=@Id
	UPDATE Stages SET DentinId=NULL WHERE Id=@Id
	UPDATE Stages SET EnamelId=NULL WHERE Id=@Id
	UPDATE Stages SET CanalMethodId=NULL WHERE Id=@Id
	UPDATE Stages SET SealerId=NULL WHERE Id=@Id
	UPDATE Stages SET CalciumId=NULL WHERE Id=@Id
	UPDATE Stages SET CementId=NULL WHERE Id=@Id
	UPDATE Stages SET TechnicianId=NULL WHERE Id=@Id
	UPDATE Stages SET PinId=NULL WHERE Id=@Id
	DELETE FROM StageAssets WHERE Id=@Id
END