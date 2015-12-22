using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeAnalysis.Enums;

namespace CodeAnalysis._TestStuff
{
    class FlagArgumentTester
    {
        public void EnumFlagArgumentFunction(ClassType args)
        {
            switch (args)
            {
                case ClassType.DataStructure:
                    Console.WriteLine("1");
                    break;
                case ClassType.Hybrid:
                    Console.WriteLine("2");
                    break;
                default:
                    Console.WriteLine("3");
                    break;
            }
        }
    }
}
