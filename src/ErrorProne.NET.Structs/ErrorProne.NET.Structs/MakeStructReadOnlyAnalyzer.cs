﻿using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ErrorProne.NET.Structs
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class MakeStructReadOnlyAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = DiagnosticIds.MakeStructReadonlyDiagnosticId;

        private static readonly string Title = "Make struct readonly";
        private static readonly string MessageFormat = "Make struct '{0}' readonly";
        private static readonly string Description = "Readonly structs have better performance when passed/return by reference.";
        private const string Category = "Performance";
        private const DiagnosticSeverity Severity = DiagnosticSeverity.Warning;

        public static readonly DiagnosticDescriptor Rule = 
            new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, Severity, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSymbolAction(AnalyzeStructDeclaration, SymbolKind.NamedType);
        }

        private void AnalyzeStructDeclaration(SymbolAnalysisContext context)
        {
            var namedTypeSymbol = (INamedTypeSymbol)context.Symbol;

            if (!namedTypeSymbol.IsValueType)
            {
                return;
            }

            if (namedTypeSymbol.IsReadOnlyStruct())
            {
                return;
            }

            var members = namedTypeSymbol.GetMembers().Where(f => f is IFieldSymbol || f is IPropertySymbol).ToList();
            if (members.Count == 0 || members.All(m => IsReadonlyFieldOrProperty(m)))
            {
                var diagnostic = Diagnostic.Create(Rule, namedTypeSymbol.Locations[0], namedTypeSymbol.Name);

                context.ReportDiagnostic(diagnostic);
            }
        }

        private bool IsReadonlyFieldOrProperty(ISymbol member)
        {
            switch (member)
            {
                case IFieldSymbol fs:
                    return fs.IsReadOnly;
                case IPropertySymbol ps:
                    return ps.IsReadOnly;
                default:
                    throw new InvalidOperationException($"Unknown member type '{member.GetType()}'.");
            }
        }
    }
}