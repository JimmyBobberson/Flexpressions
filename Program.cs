using System;
using System.Diagnostics;

namespace Flexpressions;

// todo: function calculus

// todo: implement "complex functions" which can have trig, log, exponent, rational, etc (but do not have derivative)
// todo: advanced functions with composition and chaining

// todo for legacy:
//		add updated CompareTo logic
//		add refactored Flex
//		add function variable caching

class Program {

	static void Main(string[] args) {

		/*Function cutiepie = ( Flex.z ^ 3 );
		Function cutiepop = new Function(( Flex.z + 1 ), "g");


		cutiepie.Expression = cutiepie.Expression ^ 2;
		Console.WriteLine(cutiepie);

		cutiepie.Expression = 2;
		Console.WriteLine(cutiepie);

		Function ringle = Flex.x;
		Console.WriteLine(ringle.ExpressOutput(1));*/

		/*Function heinousPoly = ( 13 * ( Flex.x ^ 7 ) * ( Flex.y ^ 4 ) )
					 - ( 8 * ( Flex.z ^ 5 ) * ( Flex.t ^ 9 ) )
					 + ( 42 * ( Flex.x ^ 2 ) * Flex.y * ( Flex.z ^ 3 ) * ( Flex.t ^ 2 ) )
					 - ( 256 * ( Flex.y ^ 12 ) )
					 + ( ( Flex.x ^ 5 ) * ( Flex.t ^ 6 ) )
					 + 666;

		Console.WriteLine(heinousPoly);
		Console.WriteLine(heinousPoly.ExpressOutput(1, -1, 1, 2));
		Console.WriteLine(heinousPoly.ExpressOutput(2, 1, -1, 1));*/


		Function f = ( Flex.x ^ 3 ) + 7;
		Console.WriteLine(f);
		Console.WriteLine(f.Expression);

		f.Expression = Flex.x * ( Flex.z ^ 2 );
		Console.WriteLine(f);
		Console.WriteLine(f.ExpressOutput(3.5, 2));
		Console.WriteLine(f[3.5, 2]);



	}

}