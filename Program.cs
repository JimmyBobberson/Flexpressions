using System;
using System.Diagnostics;

namespace Flexpressions;

// todo: all tostring use StringBuilder for optimization

// todo: monomial evaluation by passing in param NumType[] args
// todo: implement functions which use some sort of bracket operator to pass in params
//			takes every independent variable from the polynomial as input somehow ... 
//			derivatives and integrals'

// todo: multiply polys
// todo: multiply polys with monos

// todo: equality operators for polys
// todo: equality operators between polys and monos (using casts?)

// todo: MonoEx<NumType>.Zero ? and other common things?

// todo: allow casting between different MonoEx types (e.g. MonoEx<int> to MonoEx<double>)

// todo: allow casting from NumType to MonoEx<NumType> for "chalkboard expressions" with specific syntax like (2 * (x ^ 2 ))

// todo: fix exponent subscripts 

// todo: implement "complex functions" which can have trig, log, exponent, rational, etc (but do not have derivative)

// todo: make everything mutable

class Program {

	static void Main(string[] args) {


		Function<int> cutiepie = new Function<int>(( Flex<int>.x ^ 2 ) + Flex<int>.y + 6);

		Function<int> lol = new Function<int>
			(( Flex<int>.x ^ 2 ) + Flex<int>.z + 6);

		Function<decimal> each = new Function<decimal>
			(Flex<decimal>.x + Flex<decimal>.y + Flex<decimal>.z);

		Function<decimal> each2 = new Function<decimal>
			(Flex<decimal>.y + Flex<decimal>.z);

		// todo: display f(x, y, z)

		Console.WriteLine(cutiepie);
		Console.WriteLine(cutiepie.ExpressOutput(3, 10));

		/*Console.WriteLine(cutiepie[3, -10]);
		Console.WriteLine(cutiepie[2, 4]);
		//Console.WriteLine(lol[3, 0, 2]);
		//Console.WriteLine(lol[3, 1, 2]);
		Console.WriteLine(lol[3, 1]);
		Console.WriteLine(each[1, 1, 1]);*/


	}

}