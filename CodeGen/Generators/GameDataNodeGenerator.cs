using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CodeGeneration.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace CodeGen
{
    public static class SyntaxNodeExtensions
    {
        public static IFieldSymbol GetFieldSymbol(this VariableDeclaratorSyntax v, Compilation compilation)
            => (IFieldSymbol)compilation.GetSemanticModel(v.SyntaxTree).GetDeclaredSymbol(v);
    }

    public static class SymbolExtensions
    {
        public static string FullName(this ITypeSymbol x)
            => $"{x.ContainingNamespace}.{x.MetadataName}";
    }

    public class GameDataNodeGenerator : ICodeGenerator
    {
        public GameDataNodeGenerator(AttributeData attributeData) 
        {
        }

        private static bool IsPartialClass(ClassDeclarationSyntax classType)
            => classType.Modifiers.Any(x => x.Kind() == SyntaxKind.PartialKeyword);

        private static bool HasPublicModifier(SyntaxTokenList tokens)
            => tokens.Any(tok => tok.Kind() == SyntaxKind.PublicKeyword);

        private static IEnumerable<(TypeSyntax type, VariableDeclaratorSyntax var)> GetFieldVariableDeclarations(ClassDeclarationSyntax cls)
             => from f in cls.ChildNodes().OfType<FieldDeclarationSyntax>()
                from v in f.Declaration.Variables
                select (f.Declaration.Type, v);

        private static IEnumerable<ParameterSyntax> GetFieldsAsParameters(ClassDeclarationSyntax cls)
             => from fv in GetFieldVariableDeclarations(cls)
                select Parameter(fv.var.Identifier).WithType(fv.type);

        private static IEnumerable<ExpressionStatementSyntax> ParameterAssignmentToFields(ClassDeclarationSyntax cls)
            => from fv in GetFieldVariableDeclarations(cls)
               select ExpressionStatement(
                   AssignmentExpression(
                       SyntaxKind.SimpleAssignmentExpression,
                       MemberAccessExpression(
                           SyntaxKind.SimpleMemberAccessExpression,
                           ThisExpression(),
                           IdentifierName(fv.var.Identifier)),
                        IdentifierName(fv.var.Identifier)));

        private static ConstructorDeclarationSyntax BuildConstructorForRecord(ClassDeclarationSyntax cls)
            => ConstructorDeclaration(cls.Identifier)
                .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
                .WithParameterList(ParameterList(SeparatedList(GetFieldsAsParameters(cls))))
                .WithBody(Block(ParameterAssignmentToFields(cls)))
                .NormalizeWhitespace();
            
        private static PropertyDeclarationSyntax BuildDefaultAccessorProp(TypeSyntax type, string propName, SyntaxToken field)
             => PropertyDeclaration(type, propName)
                    .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
                    .WithAccessorList(
                        AccessorList(
                            List(new AccessorDeclarationSyntax[] {
                                AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                                    .WithExpressionBody(ArrowExpressionClause(IdentifierName(field)))
                                    .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)),
                                AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                                    .WithExpressionBody(ArrowExpressionClause(
                                        AssignmentExpression(
                                            SyntaxKind.SimpleAssignmentExpression,
                                            IdentifierName(field),
                                            IdentifierName("value"))))
                                    .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)),
                            })))
                .NormalizeWhitespace();

        private static string FieldToPropName(SyntaxToken identifier)
            => identifier.Text.First().ToString().ToUpper() + identifier.Text.Substring(1);

        private static IEnumerable<PropertyDeclarationSyntax> BuildPropsForFieldsInRecord(ClassDeclarationSyntax cls)
        {
            var existingProps = cls.ChildNodes()
                .OfType<PropertyDeclarationSyntax>()
                .Select(x => x.Identifier);

            return GetFieldVariableDeclarations(cls)
                .Select(fv => (fv.Item1, fv.Item2, FieldToPropName(fv.var.Identifier)))
                .Where(fv => existingProps.All(prop => fv.Item3 != prop.Text))
                .Select(fv => BuildDefaultAccessorProp(fv.Item1, fv.Item3, fv.Item2.Identifier));
        }

        private static MemberDeclarationSyntax GenerateValueEquals(ClassDeclarationSyntax cls)
             => MethodDeclaration(
                    PredefinedType(Token(SyntaxKind.BoolKeyword)),
                    Identifier("Equals"))
                .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
                .WithParameterList(ParameterList(SingletonSeparatedList<ParameterSyntax>(
                    Parameter(Identifier("other"))
                    .WithType(IdentifierName(cls.Identifier)))))
                .WithBody(
                    Block(
                        ParseStatement($@"if (object.ReferenceEquals(other, null)) return false;"),
                        ParseStatement($@"if (object.ReferenceEquals(this, other)) return true;"),
                        ParseStatement($@"return {string.Join("&&",
                                                    GetFieldVariableDeclarations(cls)
                                                        .Select(fv => fv.var.Identifier.Text)
                                                        .Select(n => $"{n}.Equals(other.{n})")
                                                        .ToArray())};")))
                .NormalizeWhitespace();

        private static IEnumerable<MemberDeclarationSyntax> ImplementIEquatable(ClassDeclarationSyntax cls)
        {
            yield return GenerateValueEquals(cls);

            yield return ParseMemberDeclaration($@"
                public override bool Equals(object other) => Equals(other as {cls.Identifier});
            ");
            yield return ParseMemberDeclaration($@"
                public override int GetHashCode() => 0;
            ");
            yield return ParseMemberDeclaration($@"
                public static bool operator ==({cls.Identifier} lhs, {cls.Identifier} rhs)
                {{
                    if (object.ReferenceEquals(lhs, null))
                    {{
                        if (object.ReferenceEquals(rhs, null))
                            return true;
                        return false;
                    }}
                    return lhs.Equals(rhs);
                }}
            ");
            yield return ParseMemberDeclaration($@"
                public static bool operator !=({cls.Identifier} lhs, {cls.Identifier} rhs)
                {{
                    return !(lhs == rhs);
                }}
            ");
        }

        private static async Task<string> BuildNamedConstructorArg(TypeSyntax type, VariableDeclaratorSyntax var, CSharpCompilation compilation)
            => $"{var.Identifier}: {var.Identifier}{(await DeclaredDeepCloneable(var, compilation) ? ".DeepClone()" : "")}";
        
        private static async Task<MemberDeclarationSyntax> ImplementIDeepCloneable(ClassDeclarationSyntax classType, CSharpCompilation compilation)
             => ParseMemberDeclaration($@"
                    public {classType.Identifier} DeepClone()
                        => new {classType.Identifier}(
                            {string.Join(",\n", await Task.WhenAll(GetFieldVariableDeclarations(classType).Select(async x => await BuildNamedConstructorArg(x.Item1, x.var, compilation)).ToArray()))});
                ");

        private static MemberDeclarationSyntax BuildToStringForRecord(ClassDeclarationSyntax cls)
             => ParseMemberDeclaration(@"
                    public override string ToString() => global::Util.Stringify.ToPrettyJson(this);
                ");

        private static async Task<bool> AnyAttributeByName(ITypeSymbol type, Compilation compilation, string name)
        {
            var declaring = await Task.WhenAll(type.DeclaringSyntaxReferences.Select(x => x.GetSyntaxAsync()));
            var attrs = declaring.SelectMany(x => 
                compilation
                    .GetSemanticModel(x.SyntaxTree)
                    .GetDeclaredSymbol(x)
                    .GetAttributes());
            return attrs.Any(x => $"{x.AttributeClass.ContainingNamespace}.{x.AttributeClass.Name}" == name);
        }

        private static async Task<bool> DeclaredEquatable(VariableDeclaratorSyntax v, Compilation compilation)
        {
            var sym = v.GetFieldSymbol(compilation);

            return sym.Type.AllInterfaces.Any(x => x.FullName() == typeof(IEquatable<>).FullName) ||
                (await AnyAttributeByName(sym.Type, compilation, "CodeGen.GameDataNodeAttribute"));
        }

        private static async Task<bool> DeclaredDeepCloneable(VariableDeclaratorSyntax v, Compilation compilation)
        {
            var sym = v.GetFieldSymbol(compilation);

            return sym.Type.AllInterfaces.Any(x => x.FullName() == typeof(Util.IDeepCloneable<>).FullName) ||
                (await AnyAttributeByName(sym.Type, compilation, "CodeGen.GameDataNodeAttribute"));
        }

        private static async Task<bool> IsDeepCloneable(VariableDeclaratorSyntax v, Compilation compilation)
        {
            var sym = v.GetFieldSymbol(compilation);
            return sym.Type.IsValueType || sym.Type.FullName() == typeof(string).FullName || await DeclaredDeepCloneable(v, compilation);
        }

        public async Task<SyntaxList<MemberDeclarationSyntax>> GenerateAsync(TransformationContext context, IProgress<Diagnostic> progress, CancellationToken cancellationToken)
        {
            var classType = (ClassDeclarationSyntax)context.ProcessingNode;

            if (!IsPartialClass(classType) && progress != null)
            {
                progress.Report(
                    Diagnostic.Create(
                        new DiagnosticDescriptor(
                            "JGAME1001", 
                            "Non-partial data node",
                            "Game data node class must be partial.", 
                            "Test.Category", 
                            DiagnosticSeverity.Error, 
                            true),
                        classType.Identifier.GetLocation(),
                        new object[] {}));
            }

            // must not declare a constructor?

            foreach (var f in classType.ChildNodes().OfType<FieldDeclarationSyntax>())
            {
                foreach (var v in f.Declaration.Variables)
                {
                    if (!char.IsLower(v.Identifier.Text[0]))
                    {
                        progress.Report(
                            Diagnostic.Create(
                                new DiagnosticDescriptor(
                                    "JGAME1002",
                                    "Invalid case in data class field",
                                    "Game data field identifier '{0}' must start with lowercase letter.",
                                    "Test.Category",
                                    DiagnosticSeverity.Error,
                                    true),
                                v.Identifier.GetLocation(),
                                new object[] { v.Identifier.Text }));
                    }

                    if (HasPublicModifier(f.Modifiers))
                    {
                        progress.Report(
                            Diagnostic.Create(
                                new DiagnosticDescriptor(
                                    "JGAME1003",
                                    "Data class field must be private",
                                    "Game data field identifier '{0}' must be private.",
                                    "Test.Category",
                                    DiagnosticSeverity.Error,
                                    true),
                                v.Identifier.GetLocation(),
                                new object[] { v.Identifier.Text }));
                    }

                    var varSymbol = (IFieldSymbol)context.SemanticModel.GetDeclaredSymbol(v);
                    if (!await DeclaredEquatable(v, context.Compilation))
                    {
                        progress.Report(
                            Diagnostic.Create(
                                new DiagnosticDescriptor(
                                    "JGAME1004",
                                    "Data class field must be IEquatable",
                                    "Game data field identifier '{0}' must implement IEquatable.",
                                    "Test.Category",
                                    DiagnosticSeverity.Error,
                                    true),
                                v.Identifier.GetLocation(),
                                new object[] { v.Identifier.Text }));
                    }
                    if (!await IsDeepCloneable(v, context.Compilation))
                    {
                        progress.Report(
                            Diagnostic.Create(
                                new DiagnosticDescriptor(
                                    "JGAME1005",
                                    "Data class field must be IDeepCloneable",
                                    "Game data field identifier '{0}' must implement IDeepCloneable.",
                                    "Test.Category",
                                    DiagnosticSeverity.Error,
                                    true),
                                v.Identifier.GetLocation(),
                                new object[] { v.Identifier.Text }));
                    }
                }
            }

            classType = classType
                .WithMembers(new SyntaxList<MemberDeclarationSyntax>())
                .WithAttributeLists(List<AttributeListSyntax>())
                .AddMembers(BuildConstructorForRecord(classType))
                .AddMembers(BuildPropsForFieldsInRecord(classType).ToArray())
                .AddMembers(ImplementIEquatable(classType).ToArray())
                .AddMembers(await ImplementIDeepCloneable(classType, context.Compilation))
                .AddMembers(BuildToStringForRecord(classType));

            classType = classType
                .AddBaseListTypes(
                    SimpleBaseType(
                        QualifiedName(
                            AliasQualifiedName(
                                IdentifierName(Token(SyntaxKind.GlobalKeyword)),
                                Token(SyntaxKind.ColonColonToken),   
                                IdentifierName("System")),
                            GenericName(Identifier("IEquatable"))
                                .WithTypeArgumentList(TypeArgumentList(SingletonSeparatedList<TypeSyntax>(
                                    IdentifierName(classType.Identifier)))))),
                    SimpleBaseType(
                        QualifiedName(
                            AliasQualifiedName(
                                IdentifierName(Token(SyntaxKind.GlobalKeyword)),
                                Token(SyntaxKind.ColonColonToken),   
                                IdentifierName("Util")),
                            GenericName(Identifier("IDeepCloneable"))
                            .WithTypeArgumentList(TypeArgumentList(SingletonSeparatedList<TypeSyntax>(
                                        IdentifierName(classType.Identifier)))))));
            
            return SingletonList<MemberDeclarationSyntax>(classType);
        }
    }
}
