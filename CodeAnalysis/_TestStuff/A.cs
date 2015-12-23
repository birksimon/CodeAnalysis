using System;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace CodeAnalysis._TestStuff
{
    class A //TypeDeclarationSyntax
    {
        public const int C = 0; // VariableDeclarator
        public int Blub { get; set; }
        public void F(int i1, int i2) //MethodDeclarationSyntax (ParameterSyntax) 
        {
            string s = "balh"; // VariableDeclarator
            Console.WriteLine(s + i1 + i2);


            Datastrucutre ds = new Datastrucutre();
            ds.att2 = "asdd";       //SimpleMemberAccessExpression
            string blub = ds.att2;  //SimpleMemberAccessExpression

        }














        public int GetValue(int index, int[] arr)
        {
            if (index >= arr.Length + 1)
            {
                throw new IndexOutOfRangeException();
            }
            return arr[index];
        }

       
    }
}
