using System;
using System.Diagnostics;

namespace Flexpressions;

// todo: all tostring use StringBuilder for optimization

// todo: function calculus

// todo: multiply polys
// todo: multiply polys with monos
// todo: take monos and polys to power

// todo: equality operators for polys
// todo: equality operators between polys and monos (using casts?)

// todo: MonoEx.Zero ? and other common things?

// todo: implement "complex functions" which can have trig, log, exponent, rational, etc (but do not have derivative)

// todo: mutable behavior

class Program {

	static void Main(string[] args) {


		Function cutiepie = new Function(( Flex.x ^ 2 ) + Flex.y + 6);
		Console.WriteLine(cutiepie);
		Console.WriteLine(cutiepie.ExpressOutput(3, 10));

		cutiepie.Expression = ( Flex.x ^ 2 ) + ( Flex.z ^ 3 ) - ( 6 * Flex.y ) + 12;
		Console.WriteLine(cutiepie);
		Console.WriteLine(cutiepie.ExpressOutput(3, 5, 7));

	}

}