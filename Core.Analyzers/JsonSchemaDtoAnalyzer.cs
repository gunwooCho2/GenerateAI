using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Core.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class JsonSchemaDtoAnalyzer : DiagnosticAnalyzer
{
    private const string JsonSchemaDtoName = "Core.JsonSchema.JsonSchemaDto";
    private const string JsonSchemaFieldAttributeName = "Core.JsonSchema.JsonSchemaFieldAttribute";
    private const string JsonSchemaIgnoreAttributeName = "Core.JsonSchema.JsonSchemaIgnoreAttribute";
    private const string JsonSchemaOverrideAttributeName = "Core.JsonSchema.JsonSchemaOverrideAttribute";

    private static readonly DiagnosticDescriptor MissingSchemaMetadataRule = new(
        "GAIJS001",
        "JsonSchemaDto member must define schema metadata",
        "{0}.{1} must have JsonSchemaField or JsonSchemaIgnore",
        "JsonSchema",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor UnknownOverrideMemberRule = new(
        "GAIJS002",
        "JsonSchemaOverride target member was not found",
        "{0} overrides unknown JSON schema member '{1}'",
        "JsonSchema",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor ConflictingSchemaMetadataRule = new(
        "GAIJS003",
        "JsonSchemaDto member has conflicting schema metadata",
        "{0}.{1} cannot have both JsonSchemaField and JsonSchemaIgnore",
        "JsonSchema",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(
            MissingSchemaMetadataRule,
            UnknownOverrideMemberRule,
            ConflictingSchemaMetadataRule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(Register);
    }

    private static void Register(CompilationStartAnalysisContext context)
    {
        INamedTypeSymbol? jsonSchemaDto = context.Compilation.GetTypeByMetadataName(JsonSchemaDtoName);
        INamedTypeSymbol? fieldAttribute = context.Compilation.GetTypeByMetadataName(JsonSchemaFieldAttributeName);
        INamedTypeSymbol? ignoreAttribute = context.Compilation.GetTypeByMetadataName(JsonSchemaIgnoreAttributeName);
        INamedTypeSymbol? overrideAttribute = context.Compilation.GetTypeByMetadataName(JsonSchemaOverrideAttributeName);

        if (jsonSchemaDto == null || fieldAttribute == null || ignoreAttribute == null || overrideAttribute == null)
        {
            return;
        }

        context.RegisterSymbolAction(
            symbolContext => AnalyzeClass(
                symbolContext,
                jsonSchemaDto,
                fieldAttribute,
                ignoreAttribute,
                overrideAttribute),
            SymbolKind.NamedType);
    }

    private static void AnalyzeClass(
        SymbolAnalysisContext context,
        INamedTypeSymbol jsonSchemaDto,
        INamedTypeSymbol fieldAttribute,
        INamedTypeSymbol ignoreAttribute,
        INamedTypeSymbol overrideAttribute)
    {
        INamedTypeSymbol type = (INamedTypeSymbol)context.Symbol;

        if (type.TypeKind != TypeKind.Class
            || type.IsAbstract
            || SymbolEqualityComparer.Default.Equals(type, jsonSchemaDto)
            || !InheritsFrom(type, jsonSchemaDto))
        {
            return;
        }

        foreach (ISymbol member in GetDeclaredPublicSchemaMembers(type))
        {
            bool hasField = HasAttribute(member, fieldAttribute);
            bool hasIgnore = HasAttribute(member, ignoreAttribute);

            if (hasField && hasIgnore)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    ConflictingSchemaMetadataRule,
                    member.Locations.FirstOrDefault(),
                    type.Name,
                    member.Name));
                continue;
            }

            if (!hasField && !hasIgnore)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    MissingSchemaMetadataRule,
                    member.Locations.FirstOrDefault(),
                    type.Name,
                    member.Name));
            }
        }

        HashSet<string> availableMembers = GetAvailableSchemaMemberNames(type, jsonSchemaDto);

        foreach (AttributeData attribute in type.GetAttributes())
        {
            if (!SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, overrideAttribute))
            {
                continue;
            }

            string? memberName = attribute.ConstructorArguments.Length > 0
                ? attribute.ConstructorArguments[0].Value as string
                : null;

            if (memberName == null || memberName.Trim().Length == 0)
            {
                continue;
            }

            string overrideMemberName = memberName!;

            if (availableMembers.Contains(overrideMemberName))
            {
                continue;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                UnknownOverrideMemberRule,
                attribute.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken).GetLocation(),
                type.Name,
                overrideMemberName));
        }
    }

    private static bool InheritsFrom(INamedTypeSymbol type, INamedTypeSymbol baseType)
    {
        INamedTypeSymbol? current = type.BaseType;
        while (current != null)
        {
            if (SymbolEqualityComparer.Default.Equals(current, baseType))
            {
                return true;
            }

            current = current.BaseType;
        }

        return false;
    }

    private static IEnumerable<ISymbol> GetDeclaredPublicSchemaMembers(INamedTypeSymbol type)
    {
        foreach (ISymbol member in type.GetMembers())
        {
            if (member.IsStatic || member.DeclaredAccessibility != Accessibility.Public)
            {
                continue;
            }

            if (member is IPropertySymbol property)
            {
                if (property.IsIndexer)
                {
                    continue;
                }

                yield return property;
            }

            if (member is IFieldSymbol field && !field.IsConst)
            {
                yield return field;
            }
        }
    }

    private static HashSet<string> GetAvailableSchemaMemberNames(INamedTypeSymbol type, INamedTypeSymbol jsonSchemaDto)
    {
        HashSet<string> memberNames = new(StringComparer.Ordinal);
        INamedTypeSymbol? current = type;

        while (current != null && !SymbolEqualityComparer.Default.Equals(current, jsonSchemaDto))
        {
            foreach (ISymbol member in GetDeclaredPublicSchemaMembers(current))
            {
                memberNames.Add(member.Name);
            }

            current = current.BaseType;
        }

        return memberNames;
    }

    private static bool HasAttribute(ISymbol symbol, INamedTypeSymbol attributeType)
    {
        foreach (AttributeData attribute in symbol.GetAttributes())
        {
            if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, attributeType))
            {
                return true;
            }
        }

        return false;
    }
}
