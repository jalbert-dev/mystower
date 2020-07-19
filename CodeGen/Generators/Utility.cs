using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Util;
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

    public static class ProgressDiagnosticExtensions
    {
        private static void ReportError(IProgress<Diagnostic> self,
                                       Location loc,
                                       string id,
                                       string title,
                                       string fmt,
                                       object[] fmtArgs)
        {
            self.Report(
                Diagnostic.Create(
                    new DiagnosticDescriptor(
                        id,
                        title,
                        fmt,
                        "Game.CodeGen",
                        DiagnosticSeverity.Error,
                        true),
                    loc,
                    fmtArgs));
        }
        public static void ReportError(this IProgress<Diagnostic> self,
                                       SyntaxToken offendingToken,
                                       string id,
                                       string title,
                                       string fmt,
                                       params object[] fmtArgs)
            => ReportError(self, offendingToken.GetLocation(), id, title, fmt, fmtArgs);
        public static void ReportError(this IProgress<Diagnostic> self,
                                       SyntaxNode offendingNode,
                                       string id,
                                       string title,
                                       string fmt,
                                       params object[] fmtArgs)
            => ReportError(self, offendingNode.GetLocation(), id, title, fmt, fmtArgs);
    }

    public static class Util
    {
        public static bool IsPartialClass(ClassDeclarationSyntax classType)
            => classType.Modifiers.Any(x => x.Kind() == SyntaxKind.PartialKeyword);

        public static bool HasPublicModifier(SyntaxTokenList tokens)
            => tokens.Any(tok => tok.Kind() == SyntaxKind.PublicKeyword);

        public static IEnumerable<(TypeSyntax type, VariableDeclaratorSyntax var)> GetFieldVariableDeclarations(ClassDeclarationSyntax cls)
             => from f in cls.ChildNodes().OfType<FieldDeclarationSyntax>()
                from v in f.Declaration.Variables
                select (f.Declaration.Type, v);

        public static IEnumerable<ParameterSyntax> GetFieldsAsParameters(ClassDeclarationSyntax cls)
             => from fv in GetFieldVariableDeclarations(cls)
                select Parameter(fv.var.Identifier).WithType(fv.type);

        public static IEnumerable<ExpressionStatementSyntax> ParameterAssignmentToFields(ClassDeclarationSyntax cls)
            => from fv in GetFieldVariableDeclarations(cls)
               select ExpressionStatement(
                   AssignmentExpression(
                       SyntaxKind.SimpleAssignmentExpression,
                       MemberAccessExpression(
                           SyntaxKind.SimpleMemberAccessExpression,
                           ThisExpression(),
                           IdentifierName(fv.var.Identifier)),
                        IdentifierName(fv.var.Identifier)));

        public static ConstructorDeclarationSyntax BuildConstructorForRecord(ClassDeclarationSyntax cls)
            => ConstructorDeclaration(cls.Identifier)
                .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
                .WithParameterList(ParameterList(SeparatedList(GetFieldsAsParameters(cls))))
                .WithBody(Block(ParameterAssignmentToFields(cls)))
                .NormalizeWhitespace();
            
        public static PropertyDeclarationSyntax BuildDefaultAccessorProp(TypeSyntax type, string propName, SyntaxToken field, MethodDeclarationSyntax? setter)
             => PropertyDeclaration(type, propName)
                    .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
                    .WithAccessorList(
                        AccessorList(
                            List(new AccessorDeclarationSyntax[] {
                                AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                                    .WithExpressionBody(ArrowExpressionClause(IdentifierName(field)))
                                    .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)),
                                AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                                    .WithBody(Block(
                                        setter == null ?
                                            ParseStatement(
                                                $"{field} = value;"
                                            ) :
                                            ParseStatement($@"
                                                {field} = {setter.Identifier.Text}(value);
                                            ")
                                    ))
                            })))
                .NormalizeWhitespace();

        public static string FieldToPropName(SyntaxToken identifier)
            => identifier.Text.First().ToString().ToUpper() + identifier.Text.Substring(1);
                
        public static IEnumerable<PropertyDeclarationSyntax> BuildPropsForFieldsInRecord(ClassDeclarationSyntax cls)
             => from decl in GetFieldDeclsWithSetterMethods(cls)
                select BuildDefaultAccessorProp(decl.type, FieldToPropName(decl.var.Identifier), decl.var.Identifier, decl.method);

        public static MemberDeclarationSyntax GenerateValueEquals(ClassDeclarationSyntax cls)
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

        public static IEnumerable<MemberDeclarationSyntax> ImplementIEquatable(ClassDeclarationSyntax cls)
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

        public static async Task<string> BuildNamedConstructorArg(TypeSyntax type, VariableDeclaratorSyntax var, CSharpCompilation compilation)
            => $"{var.Identifier}: {var.Identifier}{(await DeclaredDeepCloneable(var, compilation) ? ".DeepClone()" : "")}";
        
        public static async Task<MemberDeclarationSyntax> ImplementIDeepCloneable(ClassDeclarationSyntax classType, CSharpCompilation compilation)
             => ParseMemberDeclaration($@"
                    public {classType.Identifier} DeepClone()
                        => new {classType.Identifier}(
                            {string.Join(",\n", await Task.WhenAll(GetFieldVariableDeclarations(classType).Select(async x => await BuildNamedConstructorArg(x.Item1, x.var, compilation)).ToArray()))});
                ");

        public static MemberDeclarationSyntax BuildToStringForRecord(ClassDeclarationSyntax cls)
             => ParseMemberDeclaration(@"
                    public override string ToString() => global::Util.Stringify.ToPrettyJson(this);
                ");

        public static async Task<bool> AnyAttributeByName(ITypeSymbol type, Compilation compilation, string name)
        {
            var declaring = await Task.WhenAll(type.DeclaringSyntaxReferences.Select(x => x.GetSyntaxAsync()));
            var attrs = declaring.SelectMany(x => 
                compilation
                    .GetSemanticModel(x.SyntaxTree)
                    .GetDeclaredSymbol(x)
                    .GetAttributes());
            return attrs.Any(x => $"{x.AttributeClass.ContainingNamespace}.{x.AttributeClass.Name}" == name);
        }

        public static async Task<bool> DeclaredEquatable(VariableDeclaratorSyntax v, Compilation compilation)
        {
            var sym = v.GetFieldSymbol(compilation);

            // TODO!: This is an incorrect implementation! Must check for implementation of IEquatable<T>,
            //        rather than any IEquatable`1!
            return sym.Type.AllInterfaces.Any(x => x.FullName() == typeof(IEquatable<>).FullName) ||
                (await AnyAttributeByName(sym.Type, compilation, "CodeGen.GameDataNodeAttribute"));
        }

        public static async Task<bool> DeclaredDeepCloneable(VariableDeclaratorSyntax v, Compilation compilation)
        {
            var sym = v.GetFieldSymbol(compilation);

            return sym.Type.AllInterfaces.Any(x => x.FullName() == typeof(IDeepCloneable<>).FullName) ||
                (await AnyAttributeByName(sym.Type, compilation, "CodeGen.GameDataNodeAttribute"));
        }

        public static async Task<bool> IsDeepCloneable(VariableDeclaratorSyntax v, Compilation compilation)
        {
            var sym = v.GetFieldSymbol(compilation);
            return sym.Type.IsValueType || sym.Type.FullName() == typeof(string).FullName || await DeclaredDeepCloneable(v, compilation);
        }

        public static IEnumerable<(TypeSyntax type, VariableDeclaratorSyntax var, MethodDeclarationSyntax? method)> GetFieldDeclsWithSetterMethods(ClassDeclarationSyntax cls)
             => from fv in GetFieldVariableDeclarations(cls)
                from method in cls.ChildNodes()
                                  .OfType<MethodDeclarationSyntax>()
                                  .Where(m => m.Identifier.Text == $"set_{fv.var.Identifier.Text}")
                                  .FirstOrDefault()
                                  .AsSingleton()
                select (fv.type, fv.var, method);
    }
}