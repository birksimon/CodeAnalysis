using System;
using CodeAnalysis.Domain;
using static CodeAnalysis.Domain.InheritanceInspector;

namespace CodeAnalysis._TestStuff
{
    class SubA : Base
    {
        public void Foo(string bar)
        {
            Console.WriteLine(bar);
        }

        public static void Bar()
        {
            Console.WriteLine("bar");
        }
    }
}
