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

		Function cutiepie = ( Flex.z ^ 3 );
		Function cutiepop = new Function(( Flex.z + 1 ), "g");

		cutiepie.Expression = cutiepie.Expression ^ 2;
		Console.WriteLine(cutiepie);

		cutiepie.Expression = 2;
		Console.WriteLine(cutiepie);

		Function ringle = Flex.x;
		Console.WriteLine(ringle.ExpressOutput(1));

	}

}