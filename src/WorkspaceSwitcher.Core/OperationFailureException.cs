using System;

namespace WorkspaceSwitcher.Core;

/// <summary>
/// Stable identifier of a known business failure. The UI maps these identifiers to
/// localized explanations; identification never depends on the English exception
/// text. <see cref="Unknown"/> marks every failure without a specific reason.
/// </summary>
public enum OperationFailureReason
{
    Unknown = 0,
    NameEmpty,
    ProfileMissing,
    SourceFileMissing,
    InvalidProfileFormat,
    HotkeyRegistrationFailed
}

/// <summary>
/// Known business failure carrying its <see cref="OperationFailureReason"/>. The
/// message text stays identical to the exception it replaces so existing callers
/// and CLI output keep their current wording; only the reason identifier is new.
/// </summary>
public class OperationFailureException : Exception
{
    public OperationFailureException(OperationFailureReason reason, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        Reason = reason;
    }

    public OperationFailureReason Reason { get; }
}
