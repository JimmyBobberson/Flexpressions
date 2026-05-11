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
	// var is represented by enum which is an int
	private readonly NumType[] idpDegrees;

	// coefficient of monomial
	private readonly NumType coefficient;

	private MonoEx(NumType coefficient, NumType[] idpDegrees) {

		this.coefficient = ForcePrecision(coefficient);

		this.idpDegrees = new NumType[FlexVar.NUM_VARS];
		for (int i = 0; i < idpDegrees.Length; i++)
			this.idpDegrees[i] = ForcePrecision(idpDegrees[i]);

	}

	public MonoEx(NumType coefficient) {

		this.coefficient = ForcePrecision(coefficient);
		idpDegrees = new NumType[FlexVar.NUM_VARS];

	}

	public MonoEx(NumType coefficient, FlexVar independent, NumType degree) : this(coefficient) => idpDegrees[independent.Id] = ForcePrecision(degree);

	public MonoEx(FlexVar independent, NumType degree) : this(NumType.One, independent, degree) { }

	public MonoEx(MonoEx<NumType> other) : this(other.coefficient, other.idpDegrees) { }

	public MonoEx(MonoEx<NumType> other, NumType newCoefficient) : this(newCoefficient, other.idpDegrees) { }

	public MonoEx() : this(NumType.Zero) { }

	public MonoEx<NumType> Flipped() => new MonoEx<NumType>(this, this.coefficient * NumType.CreateChecked(-1));

	#endregion

	#region Accessors

	///<summary>
	/// returns the degree of a given independent variable in the monomial (zero if the variable is not explicitly present)
	///</summary>
	public NumType DegreeOfVariable(FlexVar idpVar) => idpDegrees[idpVar.Id];

	/// <summary>
	/// returns the count of all independent variables in the monomial
	/// </summary>
	public int IndependentVariableCount {

		get {

			int count = 0;

			foreach (NumType deg in idpDegrees)
				if (deg != NumType.Zero)
					count++;

			return count;

		}

	}

	///<summary>
	/// returns the coefficient of the expression
	///</summary>
	public NumType Coefficient => this.coefficient;

	///<summary>
	/// returns the sum of all degrees of the independent variables in the monomial expression, which is the total degree of the monomial
	///</summary>
	public NumType Degree => idpDegrees.Aggregate(NumType.Zero, (current, next) => current + next);

	///<summary>
	/// returns the monomial in its written out form
	///</summary>
	public string Expression => ToString();

	/// <summary>
	/// Used as a tiebreaker for CompareTo by seeing which vars are present in the expression
	/// </summary>
	/// <returns></returns>
	private int VariableLexicalScore() {

		int score = 0;

		for (int i = 0; i < idpDegrees.Length; i++)
			if (idpDegrees[i] != NumType.Zero)
				score += i;

		return score;

	}

	#endregion

	#region Operators ( + Public Interface for Construction)

	// add monomials to make a polynomial
	public static PolyEx<NumType> operator +(MonoEx<NumType> mono1, MonoEx<NumType> mono2) => PolyEx<NumType>.CombineMonomials(mono1, mono2, false);

	// subtract monomials to make a polynomial
	public static PolyEx<NumType> operator -(MonoEx<NumType> mono1, MonoEx<NumType> mono2) => PolyEx<NumType>.CombineMonomials(mono1, mono2, true);

	// multiply monomials to get a monomial in return 
	public static MonoEx<NumType> operator *(MonoEx<NumType> mono1, MonoEx<NumType> mono2) {

		NumType coefficientProduct = ForcePrecision(mono1.coefficient * mono2.coefficient);
		NumType[] combinedVars = new NumType[FlexVar.NUM_VARS];

		// if either monomial is zero, skip all this var work
		if (coefficientProduct != NumType.Zero)
			// sum variable degrees
			for (int i = 0; i < combinedVars.Length; i++)
				combinedVars[0] = mono1.idpDegrees[i] + mono2.idpDegrees[i];

		//todo: constructor makes a new array which is a waste because this func makes a trustable new array anyways
		return new MonoEx<NumType>(coefficientProduct, combinedVars);

	}

	// multiply monomial by a scalar to get a monomial in return with the same independent variables
	public static MonoEx<NumType> operator *(MonoEx<NumType> mono1, NumType scalar) => new MonoEx<NumType>(ForcePrecision(mono1.Coefficient * scalar), mono1.idpDegrees);
	public static MonoEx<NumType> operator *(NumType scalar, MonoEx<NumType> mono1) => new MonoEx<NumType>(ForcePrecision(mono1.Coefficient * scalar), mono1.idpDegrees);
	public static MonoEx<NumType> operator -(MonoEx<NumType> mono) => mono * NumType.CreateChecked(-1);

	// divide monomial by a scalar to get a monomial in return with the same independent variables
	public static MonoEx<NumType> operator /(MonoEx<NumType> mono1, NumType scalar) => new MonoEx<NumType>(ForcePrecision(mono1.Coefficient / scalar), mono1.idpDegrees);
	public static MonoEx<NumType> operator /(NumType scalar, MonoEx<NumType> mono1) {

		NumType coef = scalar / mono1.coefficient;
		NumType[] idpList = new NumType[FlexVar.NUM_VARS];

		// flip all signs cuz thats how this division works
		for (int i = 0; i < idpList.Length; i++)
			idpList[i] = -mono1.idpDegrees[i];

		return new MonoEx<NumType>(coef, idpList);

	}

	public static implicit operator MonoEx<NumType>(NumType num) => new MonoEx<NumType>(coefficient: num);

	public static implicit operator MonoEx<NumType>(FlexVar idp) => new MonoEx<NumType>(independent: idp, degree: NumType.One);

	#region Equality, Ordering, and Hashing

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

		if (otherMono.IndependentVariableCount != this.IndependentVariableCount
			|| ( this.IndependentVariableCount == otherMono.IndependentVariableCount && this.Degree != otherMono.Degree ))
			return false;

		for (int i = 0; i < otherMono.idpDegrees.Length; i++)
			if (idpDegrees[i] != otherMono.idpDegrees[i])
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
	public static bool operator ==(MonoEx<NumType> mono1, MonoEx<NumType> mono2) => ReferenceEquals(mono1.idpDegrees, mono2.idpDegrees)
																						|| ( mono1.IsLike(mono2)
																						&& ForcePrecision(mono1.Coefficient) == ForcePrecision(mono2.Coefficient) );
	public static bool operator !=(MonoEx<NumType> mono1, MonoEx<NumType> mono2) => !( mono1 == mono2 );

	// a monomial is equal to a scalar if it has no independent variables and its coefficient is equal to the scalar
	public static bool operator ==(MonoEx<NumType> mono1, NumType num) => ( mono1.idpDegrees == null || mono1.idpDegrees.Length == 0 )
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

		foreach (NumType deg in idpDegrees)
			hash.Add(deg);

		return hash.ToHashCode();

	}

	#endregion

	#endregion

	#region Stringy 

	public string AbsToString() {

		// if coef is 0, whole thing is 0
		if (coefficient == NumType.Zero)
			return NumType.Zero.ToString();

		NumType absCoef = NumType.Abs(coefficient);

		// if the coefficient is one, it will be left out 
		string ret = ( absCoef == NumType.One ) ? "" : absCoef.ToString();

		bool isAllAlone = ( this.IndependentVariableCount == 1 ); // solo variable has no ()

		// all vars with powers and wrapped in parenthesis
		for (int i = 0; i < idpDegrees.Length; i++) {

			NumType deg = idpDegrees[i];

			if (deg != NumType.Zero)
				ret += ( !isAllAlone ? "(" : "" ) +
					FlexVar.VAR_TO_CHAR[i] + DegreeToString(deg) +
					( !isAllAlone ? ")" : "" );

		}

		return ret;

	}

	override public string ToString() => coefficient < NumType.Zero ? "-" + AbsToString() : AbsToString();

	#endregion

}





