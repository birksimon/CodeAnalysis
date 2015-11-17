using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
