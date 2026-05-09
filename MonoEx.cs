using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Linq;
using System.Net;

namespace Flexpressions {

	public struct MonoEx<T> where T : INumber<T> {

		#region static

		// exponents
		static private readonly bool EXPONENTS_ARE_SUPERSCRIPTS = false;
		static private readonly Dictionary<T, string> SUPERSCRIPT_FOR_DIGIT = new Dictionary<T, string>() {

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
		static private readonly string SUPERSCRIPT_FOR_NEGATIVE = "\u207B";

		// convert a T degree to its string representation 
		static private string DegreeToString(T degree){

			if (degree == null)
				return "";

			// non-superscript representation of exponent
			if (!EXPONENTS_ARE_SUPERSCRIPTS)
				return "^" + degree;

			// convert exponent to superscript string

			string ret = (degree < T.Zero) ? SUPERSCRIPT_FOR_NEGATIVE : "";
			degree = T.Abs(degree);

			// TODO: support multidigit powers (currently only supports single digit powe

			/*Queue<T> digits = new Queue<T>();

			while(degree > T.Zero){

				T digit = degree % T.CreateChecked(10);

				digits.Enqueue(digit);

				degree /= T.CreateChecked(10);

			}

			while (digits.Count != 0)
				ret += SUPERSCRIPT_FOR_DIGIT[digits.Dequeue()]; */

			return ret;

		}

		#endregion

		// map of independent variables to their degree
		private Dictionary<char, T> independents;
		// coefficient of monomial
		private T coefficient;

		/// <summary>
		/// construct a monomial expression with the given coefficient, independent variable, and degree 
		/// </summary>
		public MonoEx(T coefficient, char independent, T degree){

			independents = new Dictionary<char, T>();
			independents.Add(independent, degree);

			this.coefficient = coefficient;

		}

		/// <summary>
		// construct a constant monomial expression (no independent variables)
		/// </summary>
		public MonoEx(T coefficient){

			independents = new Dictionary<char, T>();

			this.coefficient = coefficient;

		}

		/// <summary>
		// construct a constant monomial expression where the coefficient is one 
		/// </summary>
		public MonoEx(char independent, T degree){

			independents = new Dictionary<char, T>();
			independents.Add(independent, degree);

			this.coefficient = T.One;

		}



		/*public MonoEx(T coefficient = T.One, IEnumerable<char> independents = null, idDegree = null){

			this.coefficient = coefficient;
			this.independents = independents;
			this.idDegree = idDegree;

		}*/

		#region accessors

		public IReadOnlyCollection<char> IndependentVariables => independents.Keys;	
		public T Coefficient => this.coefficient;
		public string Expression => ToString();

		#endregion

		#region helpers

		override public string ToString(){

			string ret = "" + coefficient;

			foreach (var (independent, degree) in independents)
				ret += "(" + independent + DegreeToString(degree) + ")";

			return ret;

		}

		#endregion

	}

}


