﻿using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection.Metadata;
using ErrorProne.NET.Common;
using ErrorProne.NET.Core;
using ErrorProne.NET.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ErrorProne.NET.OtherRules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class PropertyAssignmentAnalyser : DiagnosticAnalyzer
    {
        private static readonly string Title = "Readonly property was never assigned.";
        private static readonly string Message = "Readonly property '{0} is never assigned to, and will always have its default value '{1}'.";
        private static readonly string Description = "Readonly property was never assigned.";

        private const string Category = "CodeSmell";

        private static readonly DiagnosticDescriptor ReadonlyPropertyWasNeverAssignedRule =
            new DiagnosticDescriptor(RuleIds.ReadonlyPropertyWasNeverAssignmed, Title, Message, Category,
                DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        private static readonly string TitlePrivateSetter = "Property with private setter was never assigned.";
        private static readonly string MessagePrivateSetter = "Property '{0} with private setter is never assigned to, and will always have its default value '{1}'.";
        private static readonly string DescriptionPrivateSetter = "Property with private setterwas never assigned.";

        private static readonly DiagnosticDescriptor PropertyWithPrivateSetterWasNeverAssigned =
            new DiagnosticDescriptor(RuleIds.PropertyWithPrivateSetterWasNeverAssigned, TitlePrivateSetter, MessagePrivateSetter, Category,
                DiagnosticSeverity.Warning, isEnabledByDefault: true, description: DescriptionPrivateSetter);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => 
            ImmutableArray.Create(ReadonlyPropertyWasNeverAssignedRule, PropertyWithPrivateSetterWasNeverAssigned);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzePropertyDeclaration, SyntaxKind.PropertyDeclaration);
        }

        private void AnalyzePropertyDeclaration(SyntaxNodeAnalysisContext context)
        {
            var propertyDeclaration = (PropertyDeclarationSyntax) context.Node;
            var propertySymbol = context.SemanticModel.GetDeclaredSymbol(propertyDeclaration);

            Contract.Assert(propertySymbol != null);

            var semanticModel = context.SemanticModel;
            // For readonly properties this stuff could be much more efficient, because only constructors
            // needs to be analyzed!
            var syntax = propertySymbol.ContainingType.DeclaringSyntaxReferences.FirstOrDefault()?.SyntaxTree;
            Contract.Assert(syntax != null);

            var writes = syntax.GetRoot()
                .DescendantNodes()
                .OfType<AssignmentExpressionSyntax>()
                .Select(id => new { Symbol = semanticModel.GetSymbolInfo(id.Left).Symbol, Id = id })
                .Where(x => x.Symbol != null && x.Symbol.Equals(propertySymbol))
                .ToList();

            var defaultValue = propertySymbol.Type.Defaultvalue();
            
            if (propertySymbol.IsReadOnly && writes.Count == 0)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(ReadonlyPropertyWasNeverAssignedRule, propertyDeclaration.Identifier.GetLocation(),
                    propertySymbol.Name, defaultValue));
            }
            else if (propertySymbol.SetMethod?.DeclaredAccessibility == Accessibility.Private && writes.Count == 0)
            {
                var setter = propertyDeclaration.DescendantNodes().OfType<AccessorDeclarationSyntax>().First(d => d.IsKind(SyntaxKind.SetAccessorDeclaration));
                context.ReportDiagnostic(
                    Diagnostic.Create(PropertyWithPrivateSetterWasNeverAssigned, setter.GetLocation(), propertySymbol.Name, defaultValue));
            }
        }
    }
}