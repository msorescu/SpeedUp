using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
namespace SelectLambda
{
    class Program
    {
        static void Main(string[] args)
        {
            var numbers = new int[] { 1, 2, 3, 4, 5, 6 };
            foreach (var result in numbers.Select(n => new { Number = n, IsEven = n % 2 == 0 }))
                Console.WriteLine(result);
            Console.ReadLine();
}
}
}