using System;
using System.Linq;
using System.Threading.Tasks;
using CodeGeneration.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static CodeGen.Util;

namespace CodeGen
{
    public static class ProgressDiagnosticErrorExtensions
    {
        public static async Task EnforceFieldTypesEquatableAndCloneable(this IProgress<Diagnostic> progress, TransformationContext context, ClassDeclarationSyntax classType)
        {
            var fieldvars = from f in classType.ChildNodes().OfType<FieldDeclarationSyntax>()
                            from v in f.Declaration.Variables
                            select (f, v);

            foreach (var (f, v) in fieldvars)
            {
                var varSymbol = (IFieldSymbol)context.SemanticModel.GetDeclaredSymbol(v);
                if (!await DeclaredEquatable(v, context.Compilation))
                {
                    progress?.ReportError(
                        v.Identifier,
                        "JGAME1004",
                        "Data class field must be IEquatable",
                        "Game data field identifier '{0}' must implement IEquatable.",
                        v.Identifier.Text);
                }
                if (!await IsDeepCloneable(v, context.Compilation))
                {
                    progress?.ReportError(
                        v.Identifier,
                        "JGAME1005",
                        "Data class field must be IDeepCloneable",
                        "Game data field identifier '{0}' must implement IDeepCloneable.",
                        v.Identifier.Text);
                }
            }
        }

        public static void EnforcePrivateFieldsAndNames(this IProgress<Diagnostic> progress, ClassDeclarationSyntax classType)
        {
            foreach (var (_, f, v) in GetFieldVariableDeclarations(classType))
            {
                if (!char.IsLower(v.Identifier.Text[0]))
                {
                    progress?.ReportError(
                        v.Identifier,
                        "JGAME1002",
                        "Invalid case in data class field",
                        "Game data field identifier '{0}' must start with lowercase letter.",
                        v.Identifier.Text);
                }

                if (HasPublicModifier(f.Modifiers))
                {
                    progress?.ReportError(
                        v.Identifier,
                        "JGAME1003",
                        "Data class field must be public",
                        "Game data field identifier '{0}' must be public.",
                        v.Identifier.Text);
                }
            }
        }

        public static void EnforceAutogenPropNamesNotDeclared(this IProgress<Diagnostic> progress, ClassDeclarationSyntax classType)
        {
            var fieldvars = from f in classType.ChildNodes().OfType<FieldDeclarationSyntax>()
                            from v in f.Declaration.Variables
                            select (f, v);

            foreach (var prop in classType.ChildNodes().OfType<PropertyDeclarationSyntax>())
            {
                if (fieldvars.Any(x => FieldToPropName(x.v.Identifier) == prop.Identifier.Text))
                {
                    progress?.ReportError(
                        prop.Identifier,
                        "JGAME1006",
                        "Property is autogenerated",
                        "Property '{0}' is generated and must not be predefined.",
                        prop.Identifier.Text);

                }
            }
        }

        public static void EnforceNoDeclaredConstructor(this IProgress<Diagnostic> progress, ClassDeclarationSyntax classType)
        {
            if (classType.ChildNodes().OfType<ConstructorDeclarationSyntax>().Any())
            {
                progress?.ReportError(
                    classType.Identifier,
                    "JGAME1007",
                    "Illegal constructor",
                    "Game data node class '{0}' may not define constructors.",
                    classType.Identifier);
            }
        }

        public static void EnforceIsPartialClass(this IProgress<Diagnostic> progress, ClassDeclarationSyntax classType)
        {
            if (!IsPartialClass(classType))
            {
                progress.ReportError(
                    classType.Identifier,
                    "JGAME1001",
                    "Non-partial data node",
                    "Game data node class must be partial.");
            }
        }
    }
}