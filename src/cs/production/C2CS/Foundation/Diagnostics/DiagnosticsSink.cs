// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using JetBrains.Annotations;

namespace C2CS;

[PublicAPI]
public sealed class DiagnosticsSink
{
    private readonly List<Diagnostic> _diagnostics = new();

    public bool HasError
    {
        get => _diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    public void Add(Diagnostic diagnostic)
    {
        _diagnostics.Add(diagnostic);
    }

    public void AddRange(IEnumerable<Diagnostic> diagnostics)
    {
        foreach (var diagnostic in diagnostics)
        {
            Add(diagnostic);
        }
    }

    public ImmutableArray<Diagnostic> GetAll()
    {
        return _diagnostics.ToImmutableArray();
    }
}
