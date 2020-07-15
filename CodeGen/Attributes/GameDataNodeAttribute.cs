using System;
using System.Diagnostics;
using CodeGeneration.Roslyn;

namespace CodeGen
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    [CodeGenerationAttribute("CodeGen.GameDataNodeGenerator, Generators")]
    [Conditional("CodeGeneration")]
    public class GameDataNodeAttribute : Attribute
    {
    }
}
