// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory (https://github.com/lithiumtoast/c2cs) for full license information.

using System;
using System.Collections.Immutable;

namespace C2CS.Bindgen.ExploreCCode
{
    public readonly struct GenericCodeFunctionExtern : IComparable<GenericCodeFunctionExtern>
    {
        public readonly string Name;
        public readonly GenericCodeInfo Info;
        public readonly GenericCodeType ReturnType;
        public readonly GenericCodeFunctionCallingConvention CallingConvention;
        public readonly ImmutableArray<GenericCodeFunctionParameter> Parameters;

        public GenericCodeFunctionExtern(
            string name,
            GenericCodeInfo info,
            GenericCodeType returnType,
            GenericCodeFunctionCallingConvention callingConvention,
            ImmutableArray<GenericCodeFunctionParameter> parameters)
        {
            Name = name;
            Info = info;
            ReturnType = returnType;
            CallingConvention = callingConvention;
            Parameters = parameters;
        }

        public override string ToString()
        {
            return Name;
        }

        public int CompareTo(GenericCodeFunctionExtern other)
        {
            return string.Compare(Name, other.Name, StringComparison.Ordinal);
        }

        public override bool Equals(object? obj)
        {
            if (obj is GenericCodeFunctionExtern other)
            {
                return Equals(other);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public bool Equals(GenericCodeFunctionExtern other)
        {
            return Name.Equals(other.Name, StringComparison.Ordinal);
        }

        public static bool operator ==(GenericCodeFunctionExtern left, GenericCodeFunctionExtern right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GenericCodeFunctionExtern left, GenericCodeFunctionExtern right)
        {
            throw new NotImplementedException();
        }

        public static bool operator <(GenericCodeFunctionExtern left, GenericCodeFunctionExtern right)
        {
            throw new NotImplementedException();
        }

        public static bool operator >(GenericCodeFunctionExtern left, GenericCodeFunctionExtern right)
        {
            throw new NotImplementedException();
        }

        public static bool operator <=(GenericCodeFunctionExtern left, GenericCodeFunctionExtern right)
        {
            throw new NotImplementedException();
        }

        public static bool operator >=(GenericCodeFunctionExtern left, GenericCodeFunctionExtern right)
        {
            throw new NotImplementedException();
        }
    }
}
