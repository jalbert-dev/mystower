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
using Util;

namespace CodeGen
{
    public class DatabaseTypeGenerator : ICodeGenerator
    {
        public DatabaseTypeGenerator(AttributeData attributeData) 
        {
        }

        public async Task<SyntaxList<MemberDeclarationSyntax>> GenerateAsync(TransformationContext context, IProgress<Diagnostic> progress, CancellationToken cancellationToken)
        {
            var classType = (ClassDeclarationSyntax)context.ProcessingNode;
            return SingletonList<MemberDeclarationSyntax>(classType);
        }
    }
}