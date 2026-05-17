using System;
using System.Diagnostics;

namespace Flexpressions;

// todo: function calculus

// todo: implement "complex functions" which can have trig, log, exponent, rational, etc (but do not have derivative)
// todo: advanced functions with composition and chaining
class Program {

	static void Main(string[] args) {

		Function q = ( new PolyEx(Flex.x, 3) ).Add(new MonoEx(7)).Add(Flex.t ^ 7);
		Function f = ( Flex.x ^ 3 ) + 7 + ( Flex.t ^ 7 );

		Console.WriteLine(q);
		Console.WriteLine(f);
	}

}