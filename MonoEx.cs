using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Linq;
using System.Net;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Immutable;

namespace Flexpressions;

/// <summary>
/// <b>A MonoEx (monomial expression) is the product of a coefficient and a set of independent variables (each with a degree).</b> <para/>
/// MonoEx objects are the fundamental building blocks of Flexpressions.<br/>
/// A MonoEx can use any type that implements INumber (int, float, double, etc). <br/>
/// MonoEx objects are immutable and all operations return a new object.<br/>
/// </summary>
public readonly struct MonoEx<NumType> : IComparable where NumType : INumber<NumType> {

	// return T.CreateChecked(IFloatingPoint<T>.Round(fp, decimals, MidpointRounding.AwayFromZero));

	#region Static Config and Helper Stuff

	// exponent config
	static private readonly bool INT_EXPONENTS_ARE_SUPERSCRIPTS = false; // curently broken

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
	private const string SUPERSCRIPT_FOR_NEGATIVE = "\u207B";

	// not implemented!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
	static private readonly HashSet<char> DISALLOWED_VARS = new HashSet<Char>()
		{ '(', ')', '[', ']', '{', '}', '^', '+', '-', '*', '/',
		'0', '1', '2', '3', '4', '5', '6', '7', '8', '9'};

	// convert a degree number to its string representation 
	static private string DegreeToString(NumType degree) {

		if (degree == null)
			return "";

		// non-superscript representation of exponent
		if (degree is not int || !INT_EXPONENTS_ARE_SUPERSCRIPTS) {

			if (degree == NumType.One)
				return "";

			return "^" + degree;

		}


		// convert exponent to superscript string

		string ret = ( degree < NumType.Zero ) ? SUPERSCRIPT_FOR_NEGATIVE : "";
		degree = NumType.Abs(degree);

		// TODO: support multidigit powers for non int types

		Queue<NumType> digits = new Queue<NumType>();

		while (degree > NumType.Zero) {

			NumType digit = degree % NumType.CreateChecked(10);

			digits.Enqueue(digit);

			degree /= NumType.CreateChecked(10);

		}

		while (digits.Count != 0)
			ret += SUPERSCRIPT_FOR_DIGIT[digits.Dequeue()];

		return ret;

	}

	// not total decimals to display but maxs
	private const int DECIMAL_PRECISION = 3;

	// i despise how this is written
	// i was trying to cast INumber down to I
	public static NumType ForcePrecision(NumType value) {
		return value switch {

			double d => (NumType)(object)Math.Round(d, DECIMAL_PRECISION, MidpointRounding.AwayFromZero),
			float f => (NumType)(object)MathF.Round(f, DECIMAL_PRECISION, MidpointRounding.AwayFromZero),
			decimal dec => (NumType)(object)Math.Round(dec, DECIMAL_PRECISION, MidpointRounding.AwayFromZero),
			Half h => (NumType)(object)Half.Round(h, DECIMAL_PRECISION, MidpointRounding.AwayFromZero),

			// Non-floating point types - return unchanged
			_ => value
		};
	}

	#endregion

	#region State and Constructors

	// maps independent variables to their degree
	// todo: is this an imperfect choice of data structure? i dont need to modify it much after construction

	// pseudo-alias ... 
	private class IdpList : Dictionary<char, NumType> {

		public IdpList() : base() { }
		public IdpList(IdpList other) : base(other) { }

	}

	private readonly IdpList independents;

	// coefficient of monomial
	private readonly NumType coefficient;

	// construct a monomial expression with the given coefficient and idp map
	// trying to ensure that precision is forced on IdpList in its lifetime without having to iterate through it here
	private MonoEx(NumType coefficient, IdpList independents) {

		this.coefficient = ForcePrecision(coefficient);
		this.independents = new IdpList(independents);

	}

	/// <summary>
	/// construct a monomial expression with the given coefficient, independent variable, and degree 
	/// </summary>
	public MonoEx(NumType coefficient, char independent, NumType degree) :
		this(coefficient, new IdpList() { { independent, ForcePrecision(degree) } }) { }

	/// <summary>
	/// construct a constant monomial expression (no independent variables)
	/// </summary>
	public MonoEx(NumType coefficient) :
		this(coefficient, new IdpList()) { }

	/// <summary>
	/// construct a constant monomial expression where the coefficient is one 
	/// </summary>
	public MonoEx(char independent, NumType degree) :
		this(NumType.One, new IdpList() { { independent, ForcePrecision(degree) } }) { }

	public MonoEx(MonoEx<NumType> other) :
		this(other.coefficient, new IdpList(other.independents)) { }

	public MonoEx(MonoEx<NumType> other, NumType newCoefficient) :
		this(newCoefficient, new IdpList(other.independents)) { }

	public MonoEx() : this(NumType.Zero) { }

	/// <summary>
	/// Returns a monomial with the same coefficient and variables but opposite sign
	/// </summary>
	/// <returns></returns>
	public MonoEx<NumType> Flipped() => new MonoEx<NumType>(this, this.coefficient * NumType.CreateChecked(-1));

	#endregion

	#region Accessors

	///<summary>
	/// returns the degree of a given independent variable in the monomial (zero if the variable is not explicitly present)
	///</summary>
	public NumType DegreeOfVariable(char idpVar) => independents.TryGetValue(idpVar, out var degree) ? ForcePrecision(degree) : NumType.Zero;

	///<summary>
	/// returns a readonly set of all the independent variables the monomial contains
	///</summary>
	public IReadOnlyCollection<char> IndependentVariables => independents.Keys;

	/// <summary>
	/// returns the count of all independent variables in the monomial
	/// </summary>
	public int IndependentVariableCount => independents.Count;

	///<summary>
	/// returns the coefficient of the expression
	///</summary>
	public NumType Coefficient => ForcePrecision(this.coefficient);

	///<summary>
	/// returns the sum of all degrees of the independent variables in the monomial expression, which is the total degree of the monomial
	///</summary>
	public NumType Degree => ForcePrecision(independents.Values.Aggregate(NumType.Zero, (current, next) => current + next));

	///<summary>
	/// returns the monomial in its written out form
	///</summary>
	public string Expression => ToString();

	/// <summary>
	/// Returns the sum of the int values of each independent variable as a tiebreaker for CompareTo
	/// </summary>
	/// <returns></returns>
	private int VariableLexicalScore() => independents.Keys.Sum(idpVar => (int)idpVar);

	#endregion

	#region Operators ( + Public Interface for Construction)

	// add monomials to make a polynomial
	public static PolyEx<NumType> operator +(MonoEx<NumType> mono1, MonoEx<NumType> mono2) => PolyEx<NumType>.CombineMonomials(mono1, mono2, false);

	// subtract monomials to make a polynomial
	public static PolyEx<NumType> operator -(MonoEx<NumType> mono1, MonoEx<NumType> mono2) => PolyEx<NumType>.CombineMonomials(mono1, mono2, true);

	// multiply monomials to get a monomial in return 
	public static MonoEx<NumType> operator *(MonoEx<NumType> mono1, MonoEx<NumType> mono2) {

		NumType coefficientProduct = ForcePrecision(mono1.coefficient * mono2.coefficient);
		IdpList combinedVars = new IdpList();

		// if either monomial is zero, skip all this var work and turn the monomial into "canonical zero"
		if (coefficientProduct != NumType.Zero) {

			// get each variable in mono1
			foreach (char idpVar in mono1.IndependentVariables) {

				// save the degree
				NumType degree = mono1.DegreeOfVariable(idpVar);

				// check if mono2 has the variable, read its degree
				if (mono2.independents.TryGetValue(idpVar, out var otherDegree))
					//if mono2 has the variable, combine the degrees
					degree += otherDegree;

				// add the new degree variable to the new dictionary
				combinedVars.Add(idpVar, ForcePrecision(degree));

			}

			//repeat the exact process for vars in mono2 but not in mono1

			// get each variable in mono2
			foreach (char idpVar in mono2.IndependentVariables) {

				// skip if already added from mono1 pass
				if (combinedVars.ContainsKey(idpVar))
					continue;

				// var found that is in mono2 but not mono1

				// add the variable to the new dictionary
				combinedVars.Add(idpVar, ForcePrecision(mono2.DegreeOfVariable(idpVar)));

			}

		}

		return new MonoEx<NumType>(coefficientProduct, combinedVars);

	}

	// multiply monomial by a scalar to get a monomial in return with the same independent variables
	public static MonoEx<NumType> operator *(MonoEx<NumType> mono1, NumType scalar) => new MonoEx<NumType>(ForcePrecision(mono1.Coefficient * scalar), mono1.independents);
	public static MonoEx<NumType> operator *(NumType scalar, MonoEx<NumType> mono1) => new MonoEx<NumType>(ForcePrecision(mono1.Coefficient * scalar), mono1.independents);

	// divide monomial by a scalar to get a monomial in return with the same independent variables
	public static MonoEx<NumType> operator /(MonoEx<NumType> mono1, NumType scalar) => new MonoEx<NumType>(ForcePrecision(mono1.Coefficient / scalar), mono1.independents);

	#region Equality, Ordering, and Hashing

	//
	public static bool GreaterOrder(MonoEx<NumType> mono1, MonoEx<NumType> mono2) => mono1.CompareTo(mono2) < 0;

	// nonstatic delegate for GreaterOrder
	public bool HasGreaterOrderThan(MonoEx<NumType> other) => MonoEx<NumType>.GreaterOrder(this, other);

	/// <summary>
	/// if the monomials are like terms, order them by coefficient <br/>
	/// if the monomials are not like terms, order them by degree then by coefficient <br/>
	/// if the monomials are not like terms but have the same degree and coefficient, order lexicographically-ish
	/// </summary>
	/// <param name="other"></param>
	/// <returns></returns>
	/// <exception cref="ArgumentException"></exception>
	public int CompareTo(object? other) {

		// this assumes precision has been forced consistently and accurately ... 

		// less than 0: this comes before other
		// 0: this and other are same
		// greater than 0: this comes after other

		if (other == null)
			return -1;

		if (other is MonoEx<NumType> otherMono) {

			// if they have different independent variable counts, or same count dif degree,
			// they cannot be alike.
			// we check this first because IsLike is expensive and if either is condition is true, IsLike cannot be true
			if (this.IsLike(otherMono))
				return this.Coefficient.CompareTo(otherMono.coefficient);

			int degreeCompare = this.Degree.CompareTo(otherMono.Degree);
			if (degreeCompare != 0)
				return degreeCompare * -1;

			int coefCompare = this.Coefficient.CompareTo(otherMono.coefficient);
			if (coefCompare != 0)
				return coefCompare * -1;

			int scoreCompare = this.VariableLexicalScore().CompareTo(otherMono.VariableLexicalScore());

			return scoreCompare * -1;

		}
		else
			throw new ArgumentException("Object is not a MonoEx, cannot compare to other MonoEx!");

	}


	/// <summary>
	/// returns true if same vars with same degree (deep check on dict)
	/// </summary>
	/// <param name="other"></param>
	/// <returns></returns>
	public bool IsLike(MonoEx<NumType> otherMono) {

		if (otherMono.IndependentVariables.Count != this.IndependentVariables.Count
			|| ( this.IndependentVariableCount == otherMono.IndependentVariableCount && this.Degree != otherMono.Degree ))
			return false;

		foreach (var (independent, degree) in this.independents)
			if (!otherMono.independents.TryGetValue(independent, out var otherDegree) || ForcePrecision(degree) != ForcePrecision(otherDegree))
				return false;

		foreach (var (independent, degree) in otherMono.independents)
			if (!this.independents.TryGetValue(independent, out var otherDegree) || ForcePrecision(degree) != ForcePrecision(otherDegree))
				return false;

		return true;

	}

	public override bool Equals(object? other) {

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
	public static bool operator ==(MonoEx<NumType> mono1, MonoEx<NumType> mono2) => ReferenceEquals(mono1.independents, mono2.independents)
																						|| ( mono1.IsLike(mono2)
																						&& ForcePrecision(mono1.Coefficient) == ForcePrecision(mono2.Coefficient) );
	public static bool operator !=(MonoEx<NumType> mono1, MonoEx<NumType> mono2) => !( mono1 == mono2 );

	// a monomial is equal to a scalar if it has no independent variables and its coefficient is equal to the scalar
	public static bool operator ==(MonoEx<NumType> mono1, NumType num) => ( mono1.independents == null || mono1.independents.Count == 0 )
																			&& ForcePrecision(mono1.Coefficient) == ForcePrecision(num);
	public static bool operator !=(MonoEx<NumType> mono1, NumType num) => !( mono1 == num );

	// this has to match the logic in .equals()
	// .equals() delegates to ==
	// ==  uses IsLike and compares coefficients, so hash must as well
	/// <inheritdoc cref="GetHashCode"/>
	/// <summary>
	/// Generates hash based on coefficient, all variables, and their degrees. 
	/// Hash matches if the terms divided by each other would equal one. 
	/// </summary>
	public override int GetHashCode() {

		HashCode hash = new HashCode();
		hash.Add(ForcePrecision(coefficient));

		if (independents != null) {

			foreach (var key in independents.Keys.OrderBy(k => k)) { // todo: this is too slow for a hash function , but it is necessary to ensure that the order of the variables does not affect the hash (e.g. 2xy and 2yx should have the same hash). If this becomes a bottleneck, we can consider caching the hash code or using a different data structure for independents that maintains a consistent order.
				hash.Add(key);
				hash.Add(ForcePrecision(independents[key]));
			}

		}

		return hash.ToHashCode();

	}

	#endregion

	#endregion

	override public string ToString() {

		// if coef is 0, whole thing is 0
		if (coefficient == NumType.Zero)
			return NumType.Zero.ToString();

		// if the coefficient is one, it will be left out 
		string ret = ( coefficient == NumType.One && independents.Count != 0 ) ? "" : coefficient.ToString();

		// all vars with powers and wrapped in parenthesis
		foreach (var (independent, degree) in independents) {

			if (degree == NumType.Zero)
				ret += "1";
			else
				ret += "(" + independent + DegreeToString(degree) + ")";

		}


		return ret;

	}

}





