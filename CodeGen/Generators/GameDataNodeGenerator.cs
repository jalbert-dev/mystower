using System;
using System.Threading;
using System.Threading.Tasks;
using CodeGeneration.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace CodeGen
{
    public class GameDataNodeGenerator : ICodeGenerator
    {
        public GameDataNodeGenerator(AttributeData attributeData) 
        {
        }

        public Task<SyntaxList<MemberDeclarationSyntax>> GenerateAsync(TransformationContext context, IProgress<Diagnostic> progress, CancellationToken cancellationToken)
        {
            var classDeclaration = ClassDeclaration("HelloWorld");
            var list = SingletonList((MemberDeclarationSyntax)classDeclaration);
            return Task.FromResult(list);
        }
    }
}
