using System;
using System.Diagnostics;

namespace Flexpressions;

// todo: overload [] for polys
// todo: add/subtract/multiply polys
// todo: add/subtract/multiply polys with monos
// todo: cast monos to polys
// todo: allow casting between different MonoEx types (e.g. MonoEx<int> to MonoEx<double>)
// todo: allow casting from NumType to MonoEx<NumType> for "chalkboard expressions" with specific syntax like (2 * (x ^ 2 ))
// todo: fix exponent subscripts 
// todo: implement functions which use some sort of bracket operator to pass in params
//			takes every independent variable from the polynomial as input somehow ... 
// todo: look into optimizing internal data structures
// todo: implement "complex functions" which can have trig, log, exponent, rational, etc

class Program {

	static void Main(string[] args) {

		MonoEx<float> one = new MonoEx<float>(1);
		MonoEx<int> ntwo = new MonoEx<int>(-2);
		MonoEx<int> nthree = new MonoEx<int>(-3);
		MonoEx<int> quad = new MonoEx<int>('x', 2);
		MonoEx<int> nquad = new MonoEx<int>(-1, 'x', 2);
		MonoEx<double> flippy = new MonoEx<double>(7, 't', -2);
		MonoEx<double> cube = new MonoEx<double>('t', -3);
		MonoEx<double> quart = new MonoEx<double>('x', 4);

		Console.WriteLine(one);
		Console.WriteLine(ntwo);
		Console.WriteLine(quad);
		Console.WriteLine(flippy);
		Console.WriteLine();

		Console.WriteLine(ntwo * quad);
		Console.WriteLine(ntwo * nthree);
		Console.WriteLine(flippy * cube);
		Console.WriteLine(flippy * cube * quart);
		Console.WriteLine();

		PolyEx<int> poly = ntwo + quad;
		Console.WriteLine(poly);
		Console.WriteLine(cube + cube);
		Console.WriteLine(quad + nquad);


		//Console.WriteLine("\u207B");

	}

}