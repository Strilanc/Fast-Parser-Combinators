﻿using System;
using System.Linq.Expressions;

namespace Strilanc.PickleJar.Internal.Structured {
    /// <summary>
    /// FieldJar names and exposes a generic Jar with known type as a non-generic Jar of unknown type.
    /// It is used as part of parsing types via reflection: each of the type's fields must be matched to a corresponding field Jar.
    /// </summary>
    internal sealed class FieldJar<T> : IFieldJarInternal {
        public readonly IJar<T> Jar;
        public CanonicalMemberName CanonicalName { get; private set; }

        public Type FieldType { get { return typeof(T); } }
        object IFieldJar.Jar { get { return Jar; } }
        public bool AreMemoryAndSerializedRepresentationsOfValueGuaranteedToMatch { get { return Jar.AreMemoryAndSerializedRepresentationsOfValueGuaranteedToMatch(); } }
        public int? OptionalConstantSerializedLength { get { return Jar.OptionalConstantSerializedLength(); } }
        public InlinedParserComponents TryMakeInlinedParserComponents(Expression array, Expression offset, Expression count) {
            return Jar.MakeInlinedParserComponents(array, offset, count);
        }

        public FieldJar(IJar<T> jar, CanonicalMemberName name) {
            Jar = jar;
            CanonicalName = name;
        }
    }
}
