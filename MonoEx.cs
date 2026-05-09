using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Linq;
using System.Net;
using System.Diagnostics.CodeAnalysis;

namespace Flexpressions
{

	public class MonoEx<NumType> where NumType : INumber<NumType>
	{

		#region static

		// exponents
		static private readonly bool EXPONENTS_ARE_SUPERSCRIPTS = false;
		static private readonly Dictionary<NumType, string> SUPERSCRIPT_FOR_DIGIT = new Dictionary<NumType, string>() {

			{NumType.Zero, "\u2070"},
			{NumType.One, "\u00B9"},
			{NumType.CreateChecked(2), "\u00B2"},
			{NumType.CreateChecked(3), "\u00B3"},
			{NumType.CreateChecked(4), "\u2074"},
			{NumType.CreateChecked(5), "\u2075"},
			{NumType.CreateChecked(6), "\u2076"},
			{NumType.CreateChecked(7), "\u2077"},
			{NumType.CreateChecked(8), "\u2078"},
			{NumType.CreateChecked(9), "\u2079"}

		};
		static private readonly string SUPERSCRIPT_FOR_NEGATIVE = "\u207B";

		// convert a T degree to its string representation 
		static private string DegreeToString(NumType degree)
		{

			if (degree == null)
				return "";

			// non-superscript representation of exponent
			if (!EXPONENTS_ARE_SUPERSCRIPTS)
				return "^" + degree;

			// convert exponent to superscript string

			string ret = (degree < NumType.Zero) ? SUPERSCRIPT_FOR_NEGATIVE : "";
			degree = NumType.Abs(degree);

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
		// todo: use ImmutableDictionary 
		private Dictionary<char, NumType> independents;
		// coefficient of monomial
		private NumType coefficient;

		#region Constructors

		public MonoEx(NumType coefficient, Dictionary<char, NumType> independents)
		{

			this.coefficient = coefficient;
			this.independents = new Dictionary<char, NumType>(independents);

		}

		/// <summary>
		/// construct a monomial expression with the given coefficient, independent variable, and degree 
		/// </summary>
		public MonoEx(NumType coefficient, char independent, NumType degree) :
			this(coefficient, new Dictionary<char, NumType>() { { independent, degree } })
		{ }

		/// <summary>
		// construct a constant monomial expression (no independent variables)
		/// </summary>
		public MonoEx(NumType coefficient) :
			this(coefficient, new Dictionary<char, NumType>())
		{ }

		/// <summary>
		// construct a constant monomial expression where the coefficient is one 
		/// </summary>
		public MonoEx(char independent, NumType degree) :
			this(NumType.One, new Dictionary<char, NumType>() { { independent, degree } })
		{ }

		public MonoEx(MonoEx<NumType> other) :
			this(other.coefficient, new Dictionary<char, NumType>(other.independents))
		{ }

		public MonoEx(MonoEx<NumType> other, NumType newCoefficient) :
			this(newCoefficient, new Dictionary<char, NumType>(other.independents))
		{ }

		// returns true if vars are the same (deep check on dict)
		public bool IsLike(MonoEx<NumType> other)
		{

			if (other.IndependentVariables.Count != this.IndependentVariables.Count)
				return false;

			foreach (var (independent, degree) in this.independents)
				if (!other.independents.TryGetValue(independent, out var otherDegree) || degree != otherDegree)
					return false;

			return true;

		}

		#endregion

		#region Equality and Hashing

		public override bool Equals(object? other)
		{

			if (other == null || other.GetType() != this.GetType())
				return false;

			// the first 

			var otherMono = (MonoEx<NumType>)other;

			return otherMono == this;

		}

		// if the two objects have the same reference, they are equal
		// if either object is null and the references are not equal, the objects are not equal
		//		(null check for safety on next check, and uses "is null" to prevent loop)
		// if the independent variable references are equal, the objects are equal
		// finally, if all else fails, we have to check to see if mono1 and mono2 have the same independent variables and coefficients (slow).
		//		if so, they are equal
		public static bool operator ==(MonoEx<NumType> mono1, MonoEx<NumType> mono2) => (ReferenceEquals(mono1, mono2))
																								|| mono1 is null || mono2 is null
																								|| ReferenceEquals(mono1.independents, mono2.independents)
																								|| (mono1.IsLike(mono2) && mono1.Coefficient == mono2.Coefficient);
		public static bool operator !=(MonoEx<NumType> mono1, MonoEx<NumType> mono2) => !(mono1 == mono2);

		// this has to match the logic in .equals()
		// .equals() delegates to ==
		// ==  uses IsLike and compares coefficients, so hash must as well
		public override int GetHashCode()
		{

			HashCode hash = new HashCode();
			hash.Add(coefficient);

			if (independents != null)
			{

				foreach (var key in independents.Keys.OrderBy(k => k))
				{
					hash.Add(key);
					hash.Add(independents[key]);
				}

			}

			return hash.ToHashCode();

		}

		#endregion

		#region accessors

		public NumType DegreeOfVariable(char idpVar) => independents.TryGetValue(idpVar, out var degree) ? degree : NumType.Zero;

		public IReadOnlyCollection<char> IndependentVariables => independents.Keys;

		public NumType Coefficient => this.coefficient;

		public NumType Degree => independents.Values.Aggregate(NumType.Zero, (current, next) => current + next);

		public string Expression => ToString();

		#endregion

		#region helpers

		override public string ToString()
		{
			// if the coefficient is one, it will be left out 
			string ret = (coefficient == NumType.One) ? "" : coefficient.ToString();

			foreach (var (independent, degree) in independents)
				ret += "(" + independent + DegreeToString(degree) + ")";

			return ret;

		}

		#endregion

		// order for polynomials:
		//by degree, higher degree first
		public static bool GreaterOrder(MonoEx<NumType> mono1, MonoEx<NumType> mono2) => mono1.Degree > mono2.Degree;

		// nonstatic delegate for GreaterOrder
		public bool GreaterOrder(MonoEx<NumType> other) => MonoEx<NumType>.GreaterOrder(this, other);

		public static PolyEx<NumType> operator +(MonoEx<NumType> mono1, MonoEx<NumType> mono2) => PolyEx<NumType>.CombineMonomials(mono1, mono2, false);

		public static PolyEx<NumType> operator -(MonoEx<NumType> mono1, MonoEx<NumType> mono2) => PolyEx<NumType>.CombineMonomials(mono1, mono2, true);

		public static MonoEx<NumType> operator *(MonoEx<NumType> mono1, MonoEx<NumType> mono2)
		{

			NumType coefficientProduct = mono1.coefficient * mono2.coefficient;
			Dictionary<char, NumType> combinedVars = new Dictionary<char, NumType>();

			// get each variable in mono1
			foreach (char idpVar in mono1.IndependentVariables)
			{

				// save the degree
				NumType degree = mono1.DegreeOfVariable(idpVar);

				// check if mono2 has the variable, read its degree
				if(mono2.independents.TryGetValue(idpVar, out var otherDegree))
					//if mono2 has the variable, combine the degrees
					degree += otherDegree;
				
				// add the new degree variable to the new dictionary
				combinedVars.Add(idpVar, degree);

			}

			//repeat the exact process for vars in mono2 but not in mono1

			// get each variable in mono2
			foreach (char idpVar in mono2.IndependentVariables)
			{

				// skip if already added from mono1 pass
				if (combinedVars.ContainsKey(idpVar))
					continue;

				// var found that is in mono2 but not mono1

				// add the variable to the new dictionary
				combinedVars.Add(idpVar, mono2.DegreeOfVariable(idpVar));

			}

			return new MonoEx<NumType>(coefficientProduct, combinedVars);

		}

	}

	public record struct MonomialNode<NumType>(char? symbol, MonoEx<NumType> mono) where NumType : INumber<NumType>
	{

		override public string ToString() => ((symbol != null) ? symbol + " " : "") 
												+ mono + " ";

	}
}


