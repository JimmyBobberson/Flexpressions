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
/// All powers are doubles (rounded) and all powers are integers
/// MonoEx objects are immutable and all operations return a new object.<br/>
/// </summary>

// todo: IEquatable
// todo: IEnumerable
// todo: IReadOnlyDictionary
// todo: IParsable
// todo: INumber
public readonly struct MonoEx : IComparable {

	#region Static Config and Helper Stuff

	// exponent config
	static private readonly bool EXPONENTS_ARE_SUPERSCRIPTS = true; // curently broken

	static private readonly string[] SUPERSCRIPT_FOR_DIGIT = new string[] {
			"\u2070",
			"\u00B9",
			"\u00B2",
			"\u00B3",
			"\u2074",
			"\u2075",
			"\u2076",
			"\u2077",
			"\u2078",
			"\u2079"
	};

	private const string SUPERSCRIPT_FOR_NEGATIVE = "\u207B";

	// convert a degree number to its string representation 
	static private string DegreeToString(int degree) {

		// Math.Pow(degree, degree);

		if (degree == 1)
			return "";

		if (!EXPONENTS_ARE_SUPERSCRIPTS)
			return "^" + degree;

		// convert exponent to superscript string

		string ret = ( degree < 0 ) ? SUPERSCRIPT_FOR_NEGATIVE : "";
		degree = int.Abs(degree);

		// TODO: support multidigit powers for non int types

		Queue<int> digits = new Queue<int>();

		while (degree > 0) {

			int digit = degree % 10;

			digits.Enqueue(digit);

			degree /= 10;

		}

		while (digits.Count != 0)
			ret += SUPERSCRIPT_FOR_DIGIT[digits.Dequeue()];

		return ret;

	}

	// not total decimals to display but maxs
	private const int DECIMAL_PRECISION = 3;

	public static double ForcePrecision(double val) => double.Round(val, DECIMAL_PRECISION, MidpointRounding.AwayFromZero);

	#endregion

	#region State and Constructors

	// maps independent variables to their degree
	// var is represented by enum which is an int
	private readonly int[] idpDegrees;

	// coefficient of monomial
	private readonly double coefficient;

	internal MonoEx(double coefficient, int[] idpDegrees) {

		this.coefficient = ForcePrecision(coefficient);

		this.idpDegrees = new int[Flex.NUM_VARS];
		for (int i = 0; i < idpDegrees.Length; i++)
			this.idpDegrees[i] = idpDegrees[i];

	}

	internal MonoEx(double coefficient) {

		this.coefficient = ForcePrecision(coefficient);
		idpDegrees = new int[Flex.NUM_VARS];

	}

	internal MonoEx(double coefficient, Flex independent, int degree) : this(coefficient) => idpDegrees[independent.Id] = degree;

	internal MonoEx(Flex independent, int degree) : this(1, independent, degree) { }

	internal MonoEx(MonoEx other) : this(other.coefficient, other.idpDegrees) { }

	internal MonoEx(MonoEx other, double newCoefficient) : this(newCoefficient, other.idpDegrees) { }

	internal MonoEx() : this(0) { }

	internal MonoEx Flipped() => new MonoEx(this, -this.coefficient);

	#endregion

	#region Accessors

	///<summary>
	/// returns the degree of a given independent variable in the monomial (zero if the variable is not explicitly present)
	///</summary>
	public int DegreeOfVariable(Flex idpVar) => idpDegrees[idpVar.Id];

	/// <summary>
	/// returns the count of all independent variables in the monomial
	/// </summary>
	public int IndependentVariableCount {

		get {

			int count = 0;

			foreach (int deg in idpDegrees)
				if (deg != 0)
					count++;

			return count;

		}

	}

	///<summary>
	/// returns the coefficient of the expression
	///</summary>
	public double Coefficient => this.coefficient;

	///<summary>
	/// returns the sum of all degrees of the independent variables in the monomial expression, which is the total degree of the monomial
	///</summary>
	public int Degree => idpDegrees.Aggregate(0, (current, next) => current + next);

	/// <summary>
	/// Used as a tiebreaker for CompareTo by seeing which vars are present in the expression
	/// </summary>
	/// <returns></returns>
	private int VariableLexicalScore() {

		int score = 0;

		for (int i = 0; i < idpDegrees.Length; i++)
			if (idpDegrees[i] != 0)
				score += i;

		return score;

	}

	#endregion

	#region Operators (Public Interface for Construction)

	// see Equality, Ordering, and Hashing for equality operators

	// add monomials to make a polynomial
	public static PolyEx operator +(MonoEx mono1, MonoEx mono2) => PolyEx.CombineMonomials(mono1, mono2, false);

	// subtract monomials to make a polynomial
	public static PolyEx operator -(MonoEx mono1, MonoEx mono2) => PolyEx.CombineMonomials(mono1, mono2, true);

	// multiply monomials to get a monomial in return 
	public static MonoEx operator *(MonoEx mono1, MonoEx mono2) {

		double coefficientProduct = ForcePrecision(mono1.coefficient * mono2.coefficient);
		int[] combinedVars = new int[Flex.NUM_VARS];

		// if either monomial is zero, skip all this var work
		if (coefficientProduct != 0)
			// sum variable degrees
			for (int i = 0; i < combinedVars.Length; i++)
				combinedVars[0] = mono1.idpDegrees[i] + mono2.idpDegrees[i];

		//todo: constructor makes a new array which is a waste because this func makes a trustable new array anyways
		return new MonoEx(coefficientProduct, combinedVars);

	}

	// multiply monomial by a scalar to get a monomial in return with the same independent variables
	public static MonoEx operator *(MonoEx mono1, double scalar) => new MonoEx(ForcePrecision(mono1.Coefficient * scalar), mono1.idpDegrees);
	public static MonoEx operator *(double scalar, MonoEx mono1) => new MonoEx(ForcePrecision(mono1.Coefficient * scalar), mono1.idpDegrees);
	public static MonoEx operator -(MonoEx mono) => mono.Flipped();

	// divide monomial by a scalar to get a monomial in return with the same independent variables
	public static MonoEx operator /(MonoEx mono1, double scalar) => new MonoEx(ForcePrecision(mono1.Coefficient / scalar), mono1.idpDegrees);
	public static MonoEx operator /(double scalar, MonoEx mono1) {

		double coef = scalar / mono1.coefficient;
		int[] idpList = new int[Flex.NUM_VARS];

		// flip all signs cuz thats how this division works
		for (int i = 0; i < idpList.Length; i++)
			idpList[i] = -mono1.idpDegrees[i];

		return new MonoEx(coef, idpList);

	}

	public static implicit operator MonoEx(double num) => new MonoEx(coefficient: num);
	public static implicit operator MonoEx(Flex idp) => new MonoEx(independent: idp, degree: 1);


	#endregion

	#region Equality, Ordering, and Hashing

	#region Comparison 

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

		if (other is MonoEx otherMono) {

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

	public static bool GreaterOrder(MonoEx mono1, MonoEx mono2) => mono1.CompareTo(mono2) < 0;

	// nonstatic delegate for GreaterOrder
	public bool HasGreaterOrderThan(MonoEx other) => MonoEx.GreaterOrder(this, other);

	#endregion

	#region Equality and Hashing

	/// <summary>
	/// returns true if same vars with same degree (deep check on dict)
	/// </summary>
	/// <param name="other"></param>
	/// <returns></returns>
	public bool IsLike(MonoEx otherMono) {

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

		var otherMono = (MonoEx)other;

		return otherMono == this;

	}

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

		foreach (int deg in idpDegrees)
			hash.Add(deg);

		return hash.ToHashCode();

	}

	#endregion

	#region Operators

	// if the two objects have the same reference, they are equal
	// if either object is null and the references are not equal, the objects are not equal
	//		(null check for safety on next check, and uses "is null" to prevent loop)
	// if the independent variable references are equal, the objects are equal
	// finally, if all else fails, we have to check to see if mono1 and mono2 have the same independent variables and coefficients (slow).
	//		if so, they are equal
	public static bool operator ==(MonoEx mono1, MonoEx mono2) => ReferenceEquals(mono1.idpDegrees, mono2.idpDegrees)
																						|| ( mono1.IsLike(mono2)
																						&& ForcePrecision(mono1.Coefficient) == ForcePrecision(mono2.Coefficient) );
	public static bool operator !=(MonoEx mono1, MonoEx mono2) => !( mono1 == mono2 );

	// a monomial is equal to a scalar if it has no independent variables and its coefficient is equal to the scalar
	public static bool operator ==(MonoEx mono1, double num) => ( mono1.idpDegrees == null || mono1.idpDegrees.Length == 0 )
																			&& ForcePrecision(mono1.Coefficient) == ForcePrecision(num);
	public static bool operator !=(MonoEx mono1, double num) => !( mono1 == num );

	#endregion

	#endregion

	#region Stringy 

	public string AbsToString() {

		// if coef is 0, whole thing is 0
		if (coefficient == 0)
			return "0";

		double absCoef = double.Abs(coefficient);

		// if the coefficient is one, it will be left out 
		string ret = ( absCoef == 1 ) ? "" : absCoef.ToString();

		bool isAllAlone = ( this.IndependentVariableCount == 1 ); // solo variable has no ()

		// all vars with powers and wrapped in parenthesis
		for (int i = 0; i < idpDegrees.Length; i++) {

			int deg = idpDegrees[i];

			if (deg != 0)
				ret += ( !isAllAlone ? "(" : "" ) +
					Flex.NUM_TO_FLEX_CHAR[i] + DegreeToString(deg) +
					( !isAllAlone ? ")" : "" );

		}

		return ret;

	}

	override public string ToString() => coefficient < 0 ? "-" + AbsToString() : AbsToString();

	#endregion

}





