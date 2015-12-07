using System;
using Microsoft.CodeAnalysis;

namespace CodeAnalysis._TestStuff
{
    class SubB : Base
    {
        public void Test()
        {
            Console.WriteLine("test");
        }

        

        /// <summary>
        /// sasdf
        /// asdfasdf
        /// 
        /// </summary>
        /// <param name="parameterA"></param>
        /// <param name="paramterB"></param>
        public void ThisIsAFunction(int parameterA, int paramterB)
        {
            /*


            */
            

            // doing some stuff
            for (int i = 0; i < 10; i++)
            {
                parameterA = parameterA + paramterB;
            }
        }
    }
}
