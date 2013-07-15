﻿using System;
using System.Linq.Expressions;

namespace Strilanc.Parsing.Internal.RepetitionParsers {
    internal sealed class FixedRepeatParser<T> : IParserInternal<T[]> {
        public bool IsBlittable { get { return false; } }
        public int? OptionalConstantSerializedLength { get { return _subParser.OptionalConstantSerializedValueLength * _count; } }

        private readonly int _count;
        private readonly IArrayParser<T> _subParser;

        public FixedRepeatParser(IArrayParser<T> arrayParser, int count) {
            this._count = count;
            _subParser = arrayParser;
        }

        public ParsedValue<T[]> Parse(ArraySegment<byte> data) {
            return _subParser.Parse(data, _count);
        }
        public Expression TryMakeParseFromDataExpression(Expression array, Expression offset, Expression count) {
            return null;
        }
        public Expression TryMakeGetValueFromParsedExpression(Expression parsed) {
            return null;
        }
        public Expression TryMakeGetCountFromParsedExpression(Expression parsed) {
            return null;
        }
    }
}
