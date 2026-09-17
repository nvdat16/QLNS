namespace Qlns.BusinessLogic.Modules.CoreHr.Employees;

/// <summary>
/// A merge-patch field slot. <see cref="IsSet"/> distinguishes "property absent from the document"
/// (leave unchanged) from "property present with null" (clear the value).
/// </summary>
public readonly record struct Optional<T>(bool IsSet, T? Value)
{
    public static Optional<T> Unset => default;

    public static Optional<T> Of(T? value) => new(true, value);
}
