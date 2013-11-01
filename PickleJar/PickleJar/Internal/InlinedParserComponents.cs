﻿using System;
using System.Linq.Expressions;
using System.Linq;

namespace Strilanc.PickleJar.Internal {
    internal struct ParsedValueStorage {
        public readonly ParameterExpression[] ForValue;
        public readonly ParameterExpression[] ForConsumedCount;

        public ParameterExpression[] ForConsumedCountIfValueAlreadyInScope {
            get { return ForConsumedCount.Except(ForValue).ToArray(); }
        }
        public ParameterExpression[] ForValueIfConsumedCountAlreadyInScope {
            get { return ForValue.Except(ForConsumedCount).ToArray(); }
        }
        public ParameterExpression[] ForBoth {
            get { return ForConsumedCount.Concat(ForValue).Distinct().ToArray(); }
        }

        public ParsedValueStorage(ParameterExpression[] variablesNeededForValue, ParameterExpression[] variablesNeededForConsumedCount) {
            ForValue = variablesNeededForValue;
            ForConsumedCount = variablesNeededForConsumedCount;
        }
    }
    internal sealed class InlinedMultiParserComponents {
        public readonly Expression ParseDoer;
        public readonly ParsedValueStorage Storage;
        public readonly Expression[] ValueGetters;
        public readonly Expression ConsumedCountGetter;

        public InlinedMultiParserComponents(Expression parseDoer, Expression[] valueGetters, Expression consumedCountGetter, ParsedValueStorage storage) {
            if (parseDoer == null) throw new ArgumentNullException("parseDoer");
            if (valueGetters == null) throw new ArgumentNullException("valueGetters");
            if (consumedCountGetter == null) throw new ArgumentNullException("consumedCountGetter");
            ParseDoer = parseDoer;
            ValueGetters = valueGetters;
            ConsumedCountGetter = consumedCountGetter;
            Storage = storage;
        }
    }

    internal delegate InlinedParserComponents InlinerMaker(Expression array, Expression offset, Expression count);
    internal sealed class InlinedParserComponents {
        public readonly Expression ParseDoer;
        public readonly ParsedValueStorage Storage;
        public readonly Expression ValueGetter;
        public readonly Expression ConsumedCountGetter;

        public InlinedParserComponents(Expression parseDoer, Expression valueGetter, Expression consumedCountGetter, ParsedValueStorage storage) {
            if (parseDoer == null) throw new ArgumentNullException("parseDoer");
            if (valueGetter == null) throw new ArgumentNullException("valueGetter");
            if (consumedCountGetter == null) throw new ArgumentNullException("consumedCountGetter");
            ParseDoer = parseDoer;
            ValueGetter = valueGetter;
            ConsumedCountGetter = consumedCountGetter;
            Storage = storage;
        }

        public static Func<ArraySegment<byte>, ParsedValue<T>> MakeParser<T>(InlinerMaker inlineMaker) {
            var paramData = Expression.Parameter(typeof(ArraySegment<byte>), "data");
            var paramDataArray = Expression.MakeMemberAccess(paramData, typeof(ArraySegment<byte>).GetProperty("Array"));
            var paramDataOffset = Expression.MakeMemberAccess(paramData, typeof(ArraySegment<byte>).GetProperty("Offset"));
            var paramDataCount = Expression.MakeMemberAccess(paramData, typeof(ArraySegment<byte>).GetProperty("Count"));

            var inl = inlineMaker(paramDataArray, paramDataOffset, paramDataCount);

            var parseAndBuild = Expression.Block(
                inl.Storage.ForBoth,
                new[] {
                    inl.ParseDoer,
                    Expression.New(typeof (ParsedValue<T>).GetConstructor(new[] {typeof (T), typeof (int)}).NotNull(),
                                   inl.ValueGetter,
                                   inl.ConsumedCountGetter)
                });

            var method = Expression.Lambda<Func<ArraySegment<byte>, ParsedValue<T>>>(
                parseAndBuild,
                new[] { paramData });

            return method.Compile();
        }
    }
}