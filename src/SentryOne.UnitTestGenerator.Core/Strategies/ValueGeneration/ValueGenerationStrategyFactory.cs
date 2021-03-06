﻿namespace SentryOne.UnitTestGenerator.Core.Strategies.ValueGeneration
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using SentryOne.UnitTestGenerator.Core.Frameworks;
    using SentryOne.UnitTestGenerator.Core.Helpers;

    public static class ValueGenerationStrategyFactory
    {
        internal static readonly Random Random = new Random();

        private static IEnumerable<IValueGenerationStrategy> Strategies =>
            new IValueGenerationStrategy[]
            {
                new SimpleValueGenerationStrategy(() => Generate.Literal("TestValue" + Random.Next(int.MaxValue)), "string"),
                new SimpleValueGenerationStrategy(() => Generate.Literal(Random.Next(int.MaxValue)), "int", "int?"),
                new SimpleValueGenerationStrategy(() => Generate.Literal((long)Random.Next(int.MaxValue)), "long", "long?"),
                new SimpleValueGenerationStrategy(() => CastedLiteral(Random.Next(int.MaxValue), SyntaxKind.UIntKeyword), "uint", "uint?"),
                new SimpleValueGenerationStrategy(() => CastedLiteral(Random.Next(int.MaxValue), SyntaxKind.ULongKeyword), "ulong", "ulong?"),
                new SimpleValueGenerationStrategy(() => Generate.Literal((decimal)((Random.NextDouble() * int.MaxValue) * 0.99d)), "decimal", "decimal?"),
                new SimpleValueGenerationStrategy(() => CastedLiteral(Random.Next(short.MaxValue), SyntaxKind.ShortKeyword), "short", "short?"),
                new SimpleValueGenerationStrategy(() => CastedLiteral(Random.Next(ushort.MaxValue), SyntaxKind.UShortKeyword), "ushort", "ushort?"),
                new SimpleValueGenerationStrategy(() => CastedLiteral(Random.Next(byte.MaxValue), SyntaxKind.ByteKeyword), "byte", "byte?"),
                new SimpleValueGenerationStrategy(() => CastedLiteral(Random.Next(sbyte.MaxValue), SyntaxKind.SByteKeyword), "sbyte", "sbyte?"),
                new SimpleValueGenerationStrategy(() => Generate.ObjectCreation(SyntaxFactory.IdentifierName("Guid"), Generate.Literal(Guid.NewGuid().ToString())), "System.Guid", "System.Guid?"),
                new SimpleValueGenerationStrategy(() => Generate.ObjectCreation(SyntaxFactory.IdentifierName("DateTime"), Generate.Literal(Random.Next(int.MaxValue))), "System.DateTime", "System.DateTime?"),
                new SimpleValueGenerationStrategy(() => Generate.Literal((Random.NextDouble() * int.MaxValue) * 0.99d), "double", "double?"),
                new SimpleValueGenerationStrategy(() => Generate.Literal((float)(Random.NextDouble() * short.MaxValue)), "float", "float?"),
                new SimpleValueGenerationStrategy(() => (Random.Next(int.MaxValue) % 2) > 0 ? Generate.Literal(true) : Generate.Literal(false), "bool", "bool?"),
                new SimpleValueGenerationStrategy(() => SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.IdentifierName("CultureInfo"), SyntaxFactory.IdentifierName((Random.Next(int.MaxValue) % 2) > 0 ? "CurrentCulture" : "InvariantCulture")), "System.Globalization.CultureInfo"),
                new SimpleValueGenerationStrategy(() => SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.IdentifierName("CancellationToken"), SyntaxFactory.IdentifierName("None")), "System.Threading.CancellationToken"),
                new SimpleValueGenerationStrategy(ArrayFactory.Byte, "byte[]"),
                new TypedValueGenerationStrategy(EnumFactory.Random, "System.Enum"),
                new SimpleValueGenerationStrategy(() => Generate.ObjectCreation(SyntaxFactory.IdentifierName("MemoryStream")), "System.IO.Stream"),
                new TypedValueGenerationStrategy(ArrayFactory.ImplicitlyTyped, "System.Collections.Generic.IEnumerable"),
                new TypedValueGenerationStrategy(ArrayFactory.ImplicitlyTyped, "System.Collections.Generic.IList"),
                new TypedValueGenerationStrategy(ArrayFactory.ImplicitlyTypedArray, "System.Array"),
                new SimpleValueGenerationStrategy(BrushFactory.Brushes, "System.Drawing.Brush"),
                new SimpleValueGenerationStrategy(BrushFactory.Brushes, "System.Windows.Media.Brush"),
                new SimpleValueGenerationStrategy(BrushFactory.Color, "System.Drawing.Color"),
                new SimpleValueGenerationStrategy(BrushFactory.Colors, "System.Windows.Media.Color"),
            };

        public static ExpressionSyntax GenerateFor(ITypeSymbol symbol, SemanticModel model, HashSet<string> visitedTypes, IFrameworkSet frameworkSet)
        {
            return GenerateFor(symbol.ToFullName(), symbol, model, visitedTypes, frameworkSet);
        }

        public static ExpressionSyntax GenerateFor(string typeName, ITypeSymbol symbol, SemanticModel model, HashSet<string> visitedTypes, IFrameworkSet frameworkSet)
        {
            if (symbol == null)
            {
                throw new ArgumentNullException(nameof(symbol));
            }

            var strategy = Strategies.FirstOrDefault(x => x.SupportedTypeNames.Any(t => string.Equals(t, typeName, StringComparison.OrdinalIgnoreCase)));
            if (strategy != null)
            {
                return strategy.CreateValueExpression(symbol, model, visitedTypes, frameworkSet);
            }

            var baseType = symbol.BaseType;
            while (baseType != null)
            {
                var name = baseType.ToFullName();
                strategy = Strategies.FirstOrDefault(x => x.SupportedTypeNames.Any(t => string.Equals(t, name, StringComparison.OrdinalIgnoreCase)));
                if (strategy != null)
                {
                    return strategy.CreateValueExpression(symbol, model, visitedTypes, frameworkSet);
                }

                baseType = baseType.BaseType;
            }

            return null;
        }

        public static bool IsSupported(ITypeSymbol symbol)
        {
            var current = symbol;
            while (current != null)
            {
                var fullName = current.ToFullName();
                var isSupported = Strategies.Any(x => x.SupportedTypeNames.Any(t => string.Equals(t, fullName, StringComparison.OrdinalIgnoreCase)));
                if (isSupported)
                {
                    return true;
                }

                current = current.BaseType;
            }

            return false;
        }

        private static ExpressionSyntax CastedLiteral(object o, SyntaxKind typeKeyword)
        {
            return SyntaxFactory.CastExpression(SyntaxFactory.PredefinedType(SyntaxFactory.Token(typeKeyword)), Generate.Literal(o));
        }
    }
}