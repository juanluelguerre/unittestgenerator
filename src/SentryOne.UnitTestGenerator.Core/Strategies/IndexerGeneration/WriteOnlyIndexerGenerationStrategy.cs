﻿namespace SentryOne.UnitTestGenerator.Core.Strategies.IndexerGeneration
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using SentryOne.UnitTestGenerator.Core.Frameworks;
    using SentryOne.UnitTestGenerator.Core.Helpers;
    using SentryOne.UnitTestGenerator.Core.Models;
    using SentryOne.UnitTestGenerator.Core.Resources;

    public class WriteOnlyIndexerGenerationStrategy : IGenerationStrategy<IIndexerModel>
    {
        private readonly IFrameworkSet _frameworkSet;

        public WriteOnlyIndexerGenerationStrategy(IFrameworkSet frameworkSet)
        {
            _frameworkSet = frameworkSet ?? throw new ArgumentNullException(nameof(frameworkSet));
        }

        public bool IsExclusive => false;

        public int Priority => 2;

        public bool CanHandle(IIndexerModel indexer, ClassModel model)
        {
            if (indexer == null)
            {
                throw new ArgumentNullException(nameof(indexer));
            }

            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            return !indexer.HasGet && indexer.HasSet;
        }

        public IEnumerable<MethodDeclarationSyntax> Create(IIndexerModel indexer, ClassModel model)
        {
            if (indexer == null)
            {
                throw new ArgumentNullException(nameof(indexer));
            }

            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            var methodName = string.Format(CultureInfo.InvariantCulture, "CanSet{0}", model.GetIndexerName(indexer));
            var method = _frameworkSet.TestFramework.CreateTestMethod(methodName, false, model.IsStatic)
                .AddBodyStatements(GetPropertyAssertionBodyStatements(indexer, model).ToArray());

            yield return method;
        }

        private IEnumerable<StatementSyntax> GetPropertyAssertionBodyStatements(IIndexerModel indexer, ClassModel sourceModel)
        {
            var paramExpressions = indexer.Parameters.Select(param => AssignmentValueHelper.GetDefaultAssignmentValue(param.TypeInfo, sourceModel.SemanticModel, _frameworkSet)).ToArray();

            var defaultAssignmentValue = AssignmentValueHelper.GetDefaultAssignmentValue(indexer.TypeInfo, sourceModel.SemanticModel, _frameworkSet);
            yield return SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, Generate.IndexerAccess(sourceModel.TargetInstance, paramExpressions), defaultAssignmentValue));
            yield return _frameworkSet.TestFramework.AssertFail(Strings.PlaceholderAssertionMessage);
        }
    }
}