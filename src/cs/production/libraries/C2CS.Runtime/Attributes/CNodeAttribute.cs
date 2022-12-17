using System;

#pragma warning disable CS1591
#pragma warning disable SA1600

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public sealed class CNodeAttribute : Attribute
{
    public string Kind { get; init; } = string.Empty;

    public string Platform { get; init; } = string.Empty;

    public string Location { get; init; } = string.Empty;
}
