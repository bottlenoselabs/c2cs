// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using C2CS.Feature.BuildLibraryC.Data;

namespace C2CS.Feature.BuildLibraryC.Domain;

// NOTE: Maps from Data layer to Domain layer and/or vice-versa
public static class DomainMapper
{
    public static Input InputFrom(InputData data)
    {
        var buildTargets = BuildTargetsFrom(data.BuildTargets);
        var input = new Input(buildTargets);

        return input;
    }

    private static ImmutableArray<BuildTarget> BuildTargetsFrom(ImmutableArray<BuildTargetData?>? data)
    {
        if (data == null || data.Value.IsDefaultOrEmpty)
        {
            return ImmutableArray<BuildTarget>.Empty;
        }

        var builder = ImmutableArray.CreateBuilder<BuildTarget>();
        foreach (var buildTargetData in data)
        {
            if (buildTargetData == null)
            {
                continue;
            }

            var buildTarget = MapBuildTargetFrom(buildTargetData);
            builder.Add(buildTarget);
        }

        return builder.ToImmutable();
    }

    private static BuildTarget MapBuildTargetFrom(BuildTargetData data)
    {
        var operatingSystem = MapOperatingSystemFrom(data.OperatingSystem);
        var targetArchitectures = MapTargetArchitecturesFrom(operatingSystem, data.TargetArchitectures);
        var isEnabledCombineTargetArchitectures = MapIsEnabledCombineTargetArchitecturesFrom(operatingSystem, data.IsEnabledCombineArchitectures);

        var buildTarget = new BuildTarget
        {
            OperatingSystem = operatingSystem,
            TargetArchitectures = targetArchitectures,
            IsEnabledCombineTargetArchitectures = isEnabledCombineTargetArchitectures
        };

        return buildTarget;
    }

    private static RuntimeOperatingSystem MapOperatingSystemFrom(string? operatingSystemString)
    {
        if (!Enum.TryParse<RuntimeOperatingSystem>(operatingSystemString, out var operatingSystem) ||
            operatingSystem is RuntimeOperatingSystem.Unknown)
        {
            return Platform.HostOperatingSystem;
        }

        return operatingSystem;
    }

    private static ImmutableArray<RuntimeArchitecture> MapTargetArchitecturesFrom(
        RuntimeOperatingSystem operatingSystem,
        ImmutableArray<string?>? architectureStrings)
    {
        var results = ImmutableArray.CreateBuilder<RuntimeArchitecture>();
        if (architectureStrings == null)
        {
            results.Add(Platform.HostArchitecture);
            return results.ToImmutable();
        }

        var architecturesHashSetBuilder = ImmutableHashSet.CreateBuilder<RuntimeArchitecture>();
        foreach (var architectureString in architectureStrings)
        {
            if (string.IsNullOrEmpty(architectureString) ||
                !Enum.TryParse<RuntimeArchitecture>(architectureString, out var architecture) ||
                architecture is RuntimeArchitecture.Unknown)
            {
                continue;
            }

            architecturesHashSetBuilder.Add(architecture);
        }

        if (architecturesHashSetBuilder.Count == 0)
        {
            architecturesHashSetBuilder.Add(Platform.HostArchitecture);
        }

        var architecturesHashSet = architecturesHashSetBuilder.ToImmutable();

        switch (operatingSystem)
        {
            case RuntimeOperatingSystem.Windows:
            case RuntimeOperatingSystem.macOS:
            case RuntimeOperatingSystem.Linux:
                FilterDesktopArchitectures(architecturesHashSet, results);
                break;
            case RuntimeOperatingSystem.FreeBSD:
            case RuntimeOperatingSystem.Android:
            case RuntimeOperatingSystem.iOS:
            case RuntimeOperatingSystem.tvOS:
            case RuntimeOperatingSystem.Browser:
            case RuntimeOperatingSystem.PlayStation:
            case RuntimeOperatingSystem.Xbox:
            case RuntimeOperatingSystem.Switch:
                // TODO: Needs testing; requires hardware.
                throw new NotImplementedException();
            case RuntimeOperatingSystem.Unknown:
                throw new NotSupportedException();
            default:
                throw new ArgumentOutOfRangeException(nameof(operatingSystem), operatingSystem, null);
        }

        static void FilterDesktopArchitectures(
            ImmutableHashSet<RuntimeArchitecture> architectures,
            ImmutableArray<RuntimeArchitecture>.Builder builder)
        {
            if (architectures.Contains(RuntimeArchitecture.X64))
            {
                builder.Add(RuntimeArchitecture.X64);
            }
            else if (architectures.Contains(RuntimeArchitecture.X86))
            {
                builder.Add(RuntimeArchitecture.X86);
            }
            else if (architectures.Contains(RuntimeArchitecture.ARM64))
            {
                builder.Add(RuntimeArchitecture.ARM64);
            }
            else if (architectures.Contains(RuntimeArchitecture.ARM32))
            {
                builder.Add(RuntimeArchitecture.ARM32);
            }
        }

        return results.ToImmutable();
    }

    private static bool MapIsEnabledCombineTargetArchitecturesFrom(
        RuntimeOperatingSystem operatingSystem, bool? combineTargetArchitectures)
    {
        if (combineTargetArchitectures == null)
        {
            return false;
        }

        if (operatingSystem != RuntimeOperatingSystem.macOS &&
            operatingSystem != RuntimeOperatingSystem.iOS &&
            operatingSystem != RuntimeOperatingSystem.tvOS)
        {
            return false;
        }

        return combineTargetArchitectures.Value;
    }
}
