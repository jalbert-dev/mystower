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

    public class GameDataNodeGenerator : ICodeGenerator
    {
        public GameDataNodeGenerator(AttributeData attributeData) 
        {
        }

        private async Task ValidateGameDataNodeTemplate(TransformationContext context, IProgress<Diagnostic> progress, ClassDeclarationSyntax classType)
        {
            progress.EnforceIsPartialClass(classType);

            foreach (var (type, var, method) in GetFieldDeclsWithSetterMethods(classType).Where(x => x.method != null))
            {
                var fieldType = context.SemanticModel.GetTypeInfo(type).Type;
                if (fieldType == null)
                    continue;

                if (!SymbolEqualityComparer.Default.Equals(
                        context.SemanticModel.GetTypeInfo(method.ReturnType).Type,
                        fieldType))
                {
                    progress?.ReportError(
                        method.ReturnType,
                        "JGAME1009",
                        "Setter type doesn't match field type",
                        "Return type of setter method '{0}' must match type of field '{1}' ('{2}').",
                        method.Identifier.Text, var.Identifier.Text, fieldType.FullName());
                }

                if (method.ParameterList.Parameters.Count != 1 ||
                    !SymbolEqualityComparer.Default.Equals(
                        context.SemanticModel.GetTypeInfo(method.ParameterList.Parameters.First().Type).Type,
                        fieldType))
                {
                    progress?.ReportError(
                        method.ParameterList,
                        "JGAME1010",
                        "Invalid setter method signature",
                        "Setter method '{0}' must have exactly 1 parameter of type '{1}'.",
                        method.Identifier.Text, fieldType.FullName());
                }
            }

            foreach (var method in classType.ChildNodes().OfType<MethodDeclarationSyntax>()
                    .Where(method => !method.Modifiers
                        .Any(modifier => modifier.Kind() == SyntaxKind.StaticKeyword))
                    .Where(method => method.Modifiers
                        .Any(modifier => modifier.Kind() == SyntaxKind.PublicKeyword)))
            {
                progress?.ReportError(
                    method.Identifier,
                    "JGAME1008",
                    "Public methods not allowed",
                    "Game data node class method '{0}.{1}' may not be public.",
                    classType.Identifier.Text, method.Identifier.Text);
            }

            progress.EnforceNoDeclaredConstructor(classType);

            progress.EnforceAutogenPropNamesNotDeclared(classType);

            progress.EnforcePrivateFieldsAndNames(classType);

            await progress.EnforceFieldTypesEquatableAndCloneable(context, classType);
        }

        public async Task<SyntaxList<MemberDeclarationSyntax>> GenerateAsync(TransformationContext context, IProgress<Diagnostic>? progress, CancellationToken cancellationToken)
        {
            var classType = (ClassDeclarationSyntax)context.ProcessingNode;

            if (progress != null)
                await ValidateGameDataNodeTemplate(context, progress, classType);

            classType = classType
                .WithMembers(new SyntaxList<MemberDeclarationSyntax>())
                .WithAttributeLists(SingletonList<AttributeListSyntax>(AttributeList(
                    SingletonSeparatedList<AttributeSyntax>(Attribute(
                        QualifiedName(
                            QualifiedName(
                                IdentifierName("Newtonsoft"),
                                IdentifierName("Json")),
                            IdentifierName("JsonObject")))
                        .WithArgumentList(AttributeArgumentList(SingletonSeparatedList<AttributeArgumentSyntax>(
                            AttributeArgument(
                                MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            IdentifierName("Newtonsoft"),
                                            IdentifierName("Json")),
                                        IdentifierName("MemberSerialization")),
                                    IdentifierName("Fields"))))))))))
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
