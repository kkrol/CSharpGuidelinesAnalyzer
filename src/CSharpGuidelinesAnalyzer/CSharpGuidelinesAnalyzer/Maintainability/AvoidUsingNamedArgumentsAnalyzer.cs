using System.Collections.Immutable;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Semantics;

namespace CSharpGuidelinesAnalyzer.Maintainability
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class AvoidUsingNamedArgumentsAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "AV1555";

        private const string Title = "Avoid using non-(nullable-)boolean named arguments";
        private const string MessageFormat = "Parameter '{0}' in the call to '{1}' is invoked with a named argument.";
        private const string Description = "Avoid using named arguments.";
        private const string Category = "Maintainability";

        [NotNull]
        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat,
            Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description,
            helpLinkUri: HelpLinkUris.GetForCategory(Category));

        [ItemNotNull]
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize([NotNull] AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterCompilationStartAction(startContext =>
            {
                if (AnalysisUtilities.SupportsOperations(startContext.Compilation))
                {
                    startContext.RegisterOperationAction(AnalyzeArgument, OperationKind.Argument);
                }
            });
        }

        private void AnalyzeArgument(OperationAnalysisContext context)
        {
            var argument = (IArgument) context.Operation;

            if (argument.ArgumentKind == ArgumentKind.Named)
            {
                if (argument.Parameter.Type.SpecialType != SpecialType.System_Boolean &&
                    !AnalysisUtilities.IsNullableBoolean(argument.Parameter.Type))
                {
                    context.ReportDiagnostic(Diagnostic.Create(Rule, argument.Syntax.GetLocation(),
                        argument.Parameter.Name, FormatSymbol(argument.Parameter.ContainingSymbol)));
                }
            }
        }

        [NotNull]
        private string FormatSymbol([NotNull] ISymbol symbol)
        {
            return symbol.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat);
        }
    }
}