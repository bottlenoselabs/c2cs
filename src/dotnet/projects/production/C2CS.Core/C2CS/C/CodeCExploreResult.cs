// Copyright (c) Craftwork Games. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

namespace C2CS
{
    internal readonly struct CodeCExploreResult
    {
        public static CodeCExploreResult Processed = new CodeCExploreResult(true);
        public static CodeCExploreResult Ignored = new CodeCExploreResult(false);

        private readonly bool _state;

        private CodeCExploreResult(bool canContinue)
        {
            _state = canContinue;
        }

        public override bool Equals(object? obj)
        {
            return obj is CodeCExploreResult result && this == result;
        }

        public override int GetHashCode()
        {
            return _state.GetHashCode();
        }

        public static bool operator ==(CodeCExploreResult first, CodeCExploreResult second)
        {
            return first._state == second._state;
        }

        public static bool operator !=(CodeCExploreResult first, CodeCExploreResult second)
        {
            return !(first == second);
        }
    }
}
