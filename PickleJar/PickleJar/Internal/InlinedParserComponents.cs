﻿using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Strilanc.PickleJar.Internal {
    internal delegate InlinedParserComponents InlinerMaker(Expression array, Expression offset, Expression count);
    internal delegate InlinedParserComponents InlinerBulkMaker(Expression array, Expression offset, Expression count, Expression itemCount);

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

            if (new object() == new object()) {
                ReflectionUtil.WriteMethodToAssembly(method, "test");
            }

            return method.Compile();
        }

        public static Func<ArraySegment<byte>, int, ParsedValue<IReadOnlyList<T>>> MakeBulkParser<T>(InlinerBulkMaker inlineMaker) {
            var paramItemCount = Expression.Parameter(typeof(int), "itemCount");
            var paramData = Expression.Parameter(typeof(ArraySegment<byte>), "data");
            var paramDataArray = Expression.MakeMemberAccess(paramData, typeof(ArraySegment<byte>).GetProperty("Array"));
            var paramDataOffset = Expression.MakeMemberAccess(paramData, typeof(ArraySegment<byte>).GetProperty("Offset"));
            var paramDataCount = Expression.MakeMemberAccess(paramData, typeof(ArraySegment<byte>).GetProperty("Count"));

            var inl = inlineMaker(paramDataArray, paramDataOffset, paramDataCount, paramItemCount);

            var parseAndBuild = Expression.Block(
                inl.Storage.ForBoth,
                new[] {
                    inl.ParseDoer,
                    Expression.New(typeof (ParsedValue<IReadOnlyList<T>>).GetConstructor(new[] {typeof (IReadOnlyList<T>), typeof (int)}).NotNull(),
                                   inl.ValueGetter,
                                   inl.ConsumedCountGetter)
                });

            var method = Expression.Lambda<Func<ArraySegment<byte>, int, ParsedValue<IReadOnlyList<T>>>>(
                parseAndBuild,
                new[] { paramData, paramItemCount });

            return method.Compile();
        }
    }
}