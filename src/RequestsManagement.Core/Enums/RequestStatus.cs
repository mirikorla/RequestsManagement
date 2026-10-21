namespace RequestsManagement.Core.Enums;

// Stored as tinyint. Explicit values keep the DB stable if members are reordered.
public enum RequestStatus : byte
{
    New = 1,
    InProgress = 2,
    Waiting = 3,
    Completed = 4
}
