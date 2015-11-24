using System;

namespace CodeAnalysis._TestStuff
{
    class A //TypeDeclarationSyntax
    {
        public const int C = 0; // VariableDeclarator
        public void F(int i1, int i2) //MethodDeclarationSyntax (ParameterSyntax) 
        {
            string s = "balh"; // VariableDeclarator
            Console.WriteLine(s + i1 + i2);
        }
    }
}
