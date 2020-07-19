﻿using System;
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
            if (!IsPartialClass(classType) && progress != null)
            {
                progress.ReportError(
                    classType.Identifier,
                    "JGAME1001", 
                    "Non-partial data node",
                    "Game data node class must be partial.");
            }

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

            if (classType.ChildNodes().OfType<ConstructorDeclarationSyntax>().Any())
            {
                progress?.ReportError(
                    classType.Identifier,
                    "JGAME1007", 
                    "Illegal constructor",
                    "Game data node class '{0}' may not define constructors.", 
                    classType.Identifier);
            }

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

            foreach (var (f, v) in fieldvars)
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
                        "Data class field must be private",
                        "Game data field identifier '{0}' must be private.",
                        v.Identifier.Text);
                }

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
