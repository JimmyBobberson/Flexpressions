using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Flexpressions {
	public struct MonoEx<T> where T : INumber<T> {

		static private readonly char DEFAULT_IV = 'x';
		static private readonly T DEFAULT_CO = T.One;
		static private readonly Dictionary<T, string> EXPONENT_FOR = new Dictionary<T, string>() {

			{T.Zero, "\u2070"},
			{T.One, "\u00B9"},
			{T.CreateChecked(2), "\u00B2"},
			{T.CreateChecked(3), "\u00B3"},
			{T.CreateChecked(4), "\u2074"},
			{T.CreateChecked(5), "\u2075"},
			{T.CreateChecked(6), "\u2076"},
			{T.CreateChecked(7), "\u2077"},
			{T.CreateChecked(8), "\u2078"},
			{T.CreateChecked(9), "\u2079"}

		};

		char independent;
		T coefficient;
		T degree; 

		public MonoEx(T degree){

			coefficient = DEFAULT_CO;
			independent = DEFAULT_IV;
			this.degree = degree;

		}

		public string Expression => "" + independent + coefficient + degree



	}

	class PolyEx<T> where T : INumber<T>{

		

	}

}
