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
using static CodeGen.Util;

namespace CodeGen
{
    public class DatabaseTypeGenerator : ICodeGenerator
    {
        public DatabaseTypeGenerator(AttributeData attributeData) 
        {
        }

        public Task<SyntaxList<MemberDeclarationSyntax>> GenerateAsync(TransformationContext context, IProgress<Diagnostic> progress, CancellationToken cancellationToken)
        {
            var classType = (ClassDeclarationSyntax)context.ProcessingNode;

            progress.EnforceIsPartialClass(classType);
            progress.EnforcePrivateFieldsAndNames(classType);
            progress.EnforceAutogenPropNamesNotDeclared(classType);

            classType = classType
                .WithMembers(new SyntaxList<MemberDeclarationSyntax>())
                .WithAttributeLists(List<AttributeListSyntax>())
                .AddMembers(
                    (from decl in GetFieldVariableDeclarations(classType)
                    select BuildReadOnlyAccessorProp(decl.type, FieldToPropName(decl.var.Identifier), decl.var.Identifier))
                    .ToArray())
                .AddMembers(
                    BuildConstructorForRecord(classType))
                .AddMembers(
                    ParseMemberDeclaration($"public bool Equals({classType.Identifier} other) => object.ReferenceEquals(this, other);"),
                    ParseMemberDeclaration($"public {classType.Identifier} DeepClone() => this;"));

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

            return Task.FromResult(SingletonList<MemberDeclarationSyntax>(classType));
        }
    }
}