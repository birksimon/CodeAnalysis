using System;

namespace CodeAnalysis._TestStuff
{
    class Base
    {
        public void Dummy()
        {
            var sub = new SubB();
            sub.Test();
            Console.WriteLine("dummy");
        }
    }
}
