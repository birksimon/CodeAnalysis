using System;

namespace CodeAnalysis._TestStuff
{
    class Base
    {
        public void Dummy()
        {
            var sub = new SubB();
            sub.Test();
            var sub2 = new SubA();
            Console.WriteLine("dummy");
            SubA.Bar();
        }
    }
}
