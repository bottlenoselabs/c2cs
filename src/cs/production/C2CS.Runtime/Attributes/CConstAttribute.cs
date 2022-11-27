using System;

#pragma warning disable CS1591
#pragma warning disable SA1600

[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
public sealed class CConstAttribute : Attribute
{
    // marker
}
