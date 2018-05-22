using System.Collections.Immutable;
using CSharpGuidelinesAnalyzer.Extensions;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace CSharpGuidelinesAnalyzer.Rules.Maintainability
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class AvoidMemberWithManyStatementsAnalyzer : GuidelineAnalyzer
    {
        public const string DiagnosticId = "AV1500";

        private const int MaxStatementCount = 7;
        private const string MaxStatementCountText = "7";

        private const string Title = "Member contains more than " + MaxStatementCountText + " statements";

        private const string MessageFormat = "{0} '{1}' contains {2} statements, which exceeds the maximum of " +
            MaxStatementCountText + " statements.";

        private const string Description = "Methods should not exceed " + MaxStatementCountText + " statements.";

        [NotNull]
        private static readonly AnalyzerCategory Category = AnalyzerCategory.Maintainability;

        [NotNull]
        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat,
            Category.Name, DiagnosticSeverity.Warning, true, Description, Category.GetHelpLinkUri(DiagnosticId));

        [ItemNotNull]
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize([NotNull] AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterOperationBlockAction(c => c.SkipInvalid(AnalyzeCodeBlock));
        }

        private void AnalyzeCodeBlock(OperationBlockAnalysisContext context)
        {
            var statementWalker = new StatementWalker();
            statementWalker.VisitBlocks(context.OperationBlocks);

            context.CancellationToken.ThrowIfCancellationRequested();

            if (statementWalker.StatementCount > MaxStatementCount)
            {
                ReportMember(context, statementWalker.StatementCount);
            }
        }

        private static void ReportMember(OperationBlockAnalysisContext context, int statementCount)
        {
            ISymbol containingMember = context.OwningSymbol.GetContainingMember();

            if (!containingMember.IsSynthesized())
            {
                string memberName = containingMember.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat);
                Location location = containingMember.Locations[0];

                context.ReportDiagnostic(Diagnostic.Create(Rule, location, containingMember.Kind, memberName, statementCount));
            }
        }

        private sealed class StatementWalker : OperationWalker
        {
            public int StatementCount { get; private set; }

            public void VisitBlocks([ItemNotNull] ImmutableArray<IOperation> blocks)
            {
                foreach (IOperation block in blocks)
                {
                    Visit(block);
                }
            }

            public override void VisitBlock([NotNull] IBlockOperation operation)
            {
                SyntaxNode parentSyntax = operation.Syntax?.Parent;

                if (parentSyntax is CheckedStatementSyntax || parentSyntax is UnsafeStatementSyntax)
                {
                    // IOperation API support for checked/unsafe statements is currently unavailable.
                    StatementCount++;
                }

                base.VisitBlock(operation);
            }

            public override void VisitVariableDeclaration([NotNull] IVariableDeclarationOperation operation)
            {
                if (operation.Syntax?.Parent is FixedStatementSyntax)
                {
                    // IOperation API support for fixed statements is currently unavailable.
                    StatementCount++;
                }

                base.VisitVariableDeclaration(operation);
            }

            public override void VisitBranch([NotNull] IBranchOperation operation)
            {
                IncrementStatementCount(operation);
            }

            public override void VisitEmpty([NotNull] IEmptyOperation operation)
            {
                IncrementStatementCount(operation);
            }

            public override void VisitExpressionStatement([NotNull] IExpressionStatementOperation operation)
            {
                IncrementStatementCount(operation);
                base.VisitExpressionStatement(operation);
            }

            public override void VisitForEachLoop([NotNull] IForEachLoopOperation operation)
            {
                IncrementStatementCount(operation);
                base.VisitForEachLoop(operation);
            }

            public override void VisitForLoop([NotNull] IForLoopOperation operation)
            {
                IncrementStatementCount(operation);
                base.VisitForLoop(operation);
            }

            public override void VisitConditional([NotNull] IConditionalOperation operation)
            {
                IncrementStatementCount(operation);
                base.VisitConditional(operation);
            }

            public override void VisitLock([NotNull] ILockOperation operation)
            {
                IncrementStatementCount(operation);
                base.VisitLock(operation);
            }

            public override void VisitReturn([NotNull] IReturnOperation operation)
            {
                IncrementStatementCount(operation);
                base.VisitReturn(operation);
            }

            public override void VisitSwitch([NotNull] ISwitchOperation operation)
            {
                IncrementStatementCount(operation);
                base.VisitSwitch(operation);
            }

            public override void VisitThrow([NotNull] IThrowOperation operation)
            {
                IncrementStatementCount(operation);
                base.VisitThrow(operation);
            }

            public override void VisitTry([NotNull] ITryOperation operation)
            {
                IncrementStatementCount(operation);
                base.VisitTry(operation);
            }

            public override void VisitUsing([NotNull] IUsingOperation operation)
            {
                IncrementStatementCount(operation);
                base.VisitUsing(operation);
            }

            public override void VisitVariableDeclarationGroup([NotNull] IVariableDeclarationGroupOperation operation)
            {
                IncrementStatementCount(operation);
                base.VisitVariableDeclarationGroup(operation);
            }

            public override void VisitWhileLoop([NotNull] IWhileLoopOperation operation)
            {
                IncrementStatementCount(operation);
                base.VisitWhileLoop(operation);
            }

            private void IncrementStatementCount([CanBeNull] IOperation operation)
            {
                if (operation != null && !operation.IsImplicit && operation.IsStatement())
                {
                    StatementCount++;
                }
            }
        }
    }
}