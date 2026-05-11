using System;
using System.Diagnostics;

namespace Flexpressions;

// todo: all tostring use StringBuilder for optimization
// todo: monomial dict turn into list for optimization 
// todo: monomial evaluation by passing in param NumType[] args
// todo: multiply polys
// todo: multiply polys with monos
// todo: cast monos to polys
// todo: equality operators for polys
// todo: equality operators between polys and monos (using casts?)
// todo: MonoEx<NumType>.Zero ? and other common things?
// todo: allow casting between different MonoEx types (e.g. MonoEx<int> to MonoEx<double>)
// todo: allow casting from NumType to MonoEx<NumType> for "chalkboard expressions" with specific syntax like (2 * (x ^ 2 ))
// todo: fix exponent subscripts 
// todo: implement functions which use some sort of bracket operator to pass in params
//			takes every independent variable from the polynomial as input somehow ... 
//			derivatives and integrals'

// todo: implement "complex functions" which can have trig, log, exponent, rational, etc (but do not have derivative)
// todo: disallowed chars list for monomial independent vars, including numbers and operators (perhaps throw an exception)
// todo: polyex list capaicty optimization
// TODO: make everything mutable

class Program {

	static void Main(string[] args) {

		//FlexVar.x;

		//PolyEx<int> p = 'x' + 2 + 3 * 'y';

		MonoEx<float> funny = new MonoEx<float>(3, FlexVar.x, 3);
		MonoEx<float> funny2 = new MonoEx<float>(3);

		Console.WriteLine(funny);
		Console.WriteLine(funny2);
		Console.WriteLine(funny2 + funny);
		Console.WriteLine(funny + funny2 + funny);

	}

}