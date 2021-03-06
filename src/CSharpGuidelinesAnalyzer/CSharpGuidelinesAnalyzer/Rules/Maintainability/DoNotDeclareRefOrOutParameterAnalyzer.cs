using System;
using System.Collections.Immutable;
using CSharpGuidelinesAnalyzer.Extensions;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CSharpGuidelinesAnalyzer.Rules.Maintainability
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class DoNotDeclareRefParameterAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CSS1562";

        private const string Title = "Do not declare a parameter as ref";
        private const string MessageFormat = "Parameter '{0}' is declared as ref.";
        private const string Description = "Don't use ref parameters.";

        [NotNull]
        private static readonly AnalyzerCategory Category = AnalyzerCategory.Maintainability;

        [NotNull]
        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat,
            Category.DisplayName, DiagnosticSeverity.Warning, true, Description, Category.GetHelpLinkUri(DiagnosticId));

        [ItemNotNull]
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        [NotNull]
        private static readonly Action<SyntaxNodeAnalysisContext> AnalyzeParameterAction =
            context => context.SkipEmptyName(AnalyzeParameter);

        public override void Initialize([NotNull] AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterSyntaxNodeAction(AnalyzeParameterAction, SyntaxKind.Parameter);
        }

        private static void AnalyzeParameter(SymbolAnalysisContext context)
        {
            var parameter = (IParameterSymbol)context.Symbol;

            if (parameter.ContainingSymbol.IsDeconstructor() || parameter.IsSynthesized())
            {
                return;
            }

            if (!IsRefParameter(parameter))
            {
                return;
            }

            AnalyzeRefParameter(parameter, context);
        }

        private static bool IsRefParameter([NotNull] IParameterSymbol parameter)
        {
            return parameter.RefKind == RefKind.Ref;
        }
         
        private static void AnalyzeRefParameter([NotNull] IParameterSymbol parameter, SymbolAnalysisContext context)
        {
            ISymbol containingMember = parameter.ContainingSymbol;

            if (!containingMember.IsOverride && !containingMember.HidesBaseMember(context.CancellationToken) &&
                !parameter.IsInterfaceImplementation())
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, parameter.Locations[0], parameter.Name));
            }
        }
    }
}
