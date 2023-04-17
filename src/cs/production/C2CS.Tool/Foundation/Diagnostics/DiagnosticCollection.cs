// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using JetBrains.Annotations;

namespace C2CS.Foundation;

[PublicAPI]
public sealed class DiagnosticCollection
{
    private readonly List<Diagnostic> _diagnostics = new();

    public bool HasFaulted => _diagnostics.Any(x => x.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Panic);

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
