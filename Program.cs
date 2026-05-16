using System;
using System.Diagnostics;

namespace Flexpressions;

// todo: use optimized version of power calculation

// todo: function calculus

// todo: advanced functions with composition and chaining

// todo: multiply polys
// todo: multiply polys with monos
// todo: take monos and polys to power

// todo: equality operators for polys
// todo: equality operators between polys and monos 

// todo: MonoEx.Zero ? and other common things?

// todo: implement "complex functions" which can have trig, log, exponent, rational, etc (but do not have derivative)

// todo: mutable behavior

// todo for legacy:
//		add updated CompareTo logic
//		add refactored Flex
//		

class Program {

	static void Main(string[] args) {

		Function cutiepie = new Function(( Flex.x ^ 2 ) + Flex.y + 6);
		Console.WriteLine(cutiepie);
		Console.WriteLine(cutiepie.ExpressOutput(3, 10));

		cutiepie.Expression = ( Flex.x ^ 2 ) + ( Flex.z ^ 3 ) - ( 6 * Flex.y ) + 12 + Flex.x + ( Flex.z ^ 2 );
		Console.WriteLine(cutiepie);
		Console.WriteLine(cutiepie.ExpressOutput(3, 5, 7));
		Console.WriteLine(( (MonoEx)3 ) == 3);


	}

}