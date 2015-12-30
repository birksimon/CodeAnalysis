using System;
using System.Runtime.InteropServices;
using CodeAnalysis.Enums;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace CodeAnalysis._TestStuff
{
    class A //TypeDeclarationSyntax
    {
        public const int C = 0; // VariableDeclarator
        public int Blub { get; set; }
        public A RecursiveTypeProperty;
        public void F(int i1, int i2) //MethodDeclarationSyntax (ParameterSyntax) 
        {
            string s = "balh"; // VariableDeclarator
            Console.WriteLine(s + i1 + i2);


            Datastrucutre ds = new Datastrucutre();
            ds.att2 = "asdd";       //SimpleMemberAccessExpression
            string blub = ds.att2;  //SimpleMemberAccessExpression
        }

        public void EnumFlagTester(ClassType enumFlagArg)
        {
            switch (enumFlagArg)
            {
                case ClassType.DataStructure:
                    Console.WriteLine("ds");
                    break;
                case ClassType.Hybrid:
                    Console.WriteLine("hyb");
                    break;
            }
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
