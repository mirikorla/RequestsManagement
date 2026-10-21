namespace RequestsManagement.Core.Enums;

// Stored as tinyint. Numeric order matters: sorting by Priority sorts Low < Medium < High.
public enum Priority : byte
{
    Low = 1,
    Medium = 2,
    High = 3
}
