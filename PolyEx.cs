using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace Flexpressions;

/// <summary>
/// <b>A PolyEx (polynomial expression) is a series of MonoEx objects chained together by operators (+/-).</b> <para/>
/// 
/// Any group of monomial expressions can be combined into a polynomial expression,
/// and those can be combined into bigger polynomial expressions. 
/// The monomials are ordered by degree.<br/>
/// 
/// Polynomial expressions can be used in functions.<br/>
/// 
/// Supports indexed accessing for terms. <br/>
/// 
/// </summary>
/// 
// todo: ICollection
public class PolyEx : IReadOnlyCollection<MonoEx>, IEquatable<PolyEx> {

	#region Static Helpers

	private const char MINUS = '-';
	private const char PLUS = '+';

	#endregion

	#region State and Constructors

	// pseudoalias wrapper
	private class MonoSeries : List<MonoEx> {

		public MonoSeries() : base() { }
		public MonoSeries(MonoSeries other) : base(other) { }
		public MonoSeries(int cap) : base(cap) { }

	}

	private readonly MonoSeries termSeries;

	private PolyEx(MonoSeries termSeries) => this.termSeries = termSeries;

	/// <summary>
	/// Creates a single-term polynomial
	/// </summary>
	/// <param name="term">monomial term to become polynomial</param>
	public PolyEx(MonoEx term) {

		MonoSeries termSeries = new MonoSeries();

		termSeries.Add(term);

		this.termSeries = termSeries;

	}

	/// <summary>
	/// Instantiates the polynomial 0
	/// </summary>
	public PolyEx() => termSeries = new MonoSeries();

	/// <summary>
	/// Create a copy of another PolyEx
	/// </summary>
	/// <param name="other">PolyEx to deep copy</param>
	public PolyEx(PolyEx other) {

		MonoSeries termSeries = new MonoSeries();

		foreach (MonoEx mono in other.termSeries)
			termSeries.Add(new MonoEx(mono));

		this.termSeries = termSeries;

	}

	#region MonoEx delegates

	/// <summary>
	/// Creates a single-term polynomial with a number
	/// </summary>
	/// <param name="coefficient">coefficient to become MonoEx term</param>
	public PolyEx(double coefficient) : this(new MonoEx(coefficient));

	/// <summary>
	/// Creates a single-term polynomial with a number, a variable, and its degree
	/// </summary>
	/// <param name="coefficient">coefficient of term</param>
	/// <param name="independent">independent variable of term</param>
	/// <param name="degree">degree of term's independent variable</param>
	public PolyEx(double coefficient, Flex independent, int degree) : this(new MonoEx(coefficient, independent, degree)) { }

	/// <summary>
	/// Creates a single-term polynomial with a number and a variable
	/// </summary>
	/// <param name="coefficient">coefficient of term</param>
	/// <param name="independent">independent variable of term, will have degree 1</param>
	public PolyEx(double coefficient, Flex independent) : this(new MonoEx(coefficient, independent)) { }

	/// <summary>
	/// Creates a single-term polynomial with a variable, and its degree
	/// </summary>
	/// <param name="independent">independent variable of term</param>
	/// <param name="degree">degree of term's independent variable</param>
	public PolyEx(Flex independent, int degree) : this(new MonoEc(independent, degree)) { }

	/// <summary>
	/// Creates a single-term polynomial with a variable
	/// </summary>
	/// <param name="independent">independent variable of term, will have degree 1</param>
	public PolyEx(Flex independent) : this(new MonoEx(independent)) { }

	#endregion

	#endregion

	#region Operators/Modifiers

	#region Master Functions

	// All polynomial-producing operators delegate to the below master functions
	// (or construct their resultants directly)

	// this is used in MonoEx
	internal static PolyEx CombineMonomials(MonoEx mono1, MonoEx mono2, bool subtract = false) {

		MonoSeries termSeries = new MonoSeries(2);

		// if the monomials are like terms, just sum their coefficients
		// resulting polynomial will only have 1 term, but its best if we always return a polynomial when we do +/- 
		if (mono1.IsLike(mono2)) {

			// add first to second
			double coefficientSum = mono1.Coefficient + ( mono2.Coefficient * ( subtract ? -1 : 1 ) );

			termSeries.Add(new MonoEx(mono1, coefficientSum));

		}
		else {

			mono2 = subtract ? mono2.Flipped() : mono2;

			termSeries.Add(mono1);
			termSeries.Add(mono2);

		}

		return new PolyEx(termSeries); // runs sort

	}

	private void InsertMonomial(MonoEx toInsert, bool subtract = false) {

		// create series to be used for new PolyEx as a copy of poly's series

		// see if poly contains a term like toInsert 
		int? idxToRemove = null;
		bool foundLikeTerm = false;
		for (int i = 0; i < termSeries.Count; i++) {

			var mono = termSeries[i];

			if (mono.IsLike(toInsert)) {

				foundLikeTerm = true;

				double coefficientSum = mono.Coefficient + ( toInsert.Coefficient * ( subtract ? -1 : 1 ) );

				if (coefficientSum == 0)
					idxToRemove = i;
				else
					termSeries[i] = new MonoEx(toInsert, coefficientSum);

				break;

			}
		}

		if (idxToRemove != null)
			termSeries.RemoveAt((int)idxToRemove);
		// toInsert is not a like term of any element in the polynomial so we will insert it normally
		else if (!foundLikeTerm)
			termSeries.Add(subtract ? toInsert.Flipped() : toInsert);

	}

	#endregion

	#region	Immutable (Public Interface for Construction)

	#region Typecasts

	/// <summary>
	/// Convert a monomial into a single-term polynomial
	/// </summary>
	/// <param name="mono"></param>
	public static implicit operator PolyEx(MonoEx mono) => new PolyEx(mono);
	/// <summary>
	/// Convert a coefficient into a monomial and then into into a single-term polynomial
	/// </summary>
	/// <param name="num"></param>
	public static implicit operator PolyEx(double num) => new PolyEx(new MonoEx(coefficient: num));
	/// <summary>
	/// Convert a variable into a monomial and then into into a single-term polynomial
	/// </summary>
	/// <param name="idp"></param>
	public static implicit operator PolyEx(Flex idp) => new PolyEx(new MonoEx(independent: idp, degree: 1));

	#endregion

	#region Addition and Subtraction

	/// <summary>
	/// Add a monomial to a polynomial
	/// </summary>
	/// <returns>Sum of expressions as polynomial</returns>
	public static PolyEx operator +(PolyEx poly, MonoEx mono) {

		PolyEx temp = new PolyEx(poly);

		return temp.Add(mono);

	}
	public static PolyEx operator +(MonoEx mono, PolyEx poly) => poly + mono;

	/// <summary>
	/// Adds all monomials from poly2 to poly1
	/// </summary>
	/// <returns>Sum of expressions as polynomial</returns>
	public static PolyEx operator +(PolyEx poly1, PolyEx poly2) {

		PolyEx temp = new PolyEx(poly1);

		return temp.Add(poly2);

	}

	/// <summary>
	/// Subtracts a monomial from a polynomial
	/// </summary>
	/// <returns>Difference of expressions as polynomial</returns>
	public static PolyEx operator -(PolyEx poly, MonoEx mono) {

		PolyEx temp = new PolyEx(poly);

		return temp.Subtract(mono);

	}
	public static PolyEx operator -(MonoEx mono, PolyEx poly) {

		// mono - poly == -poly + mono
		PolyEx temp = poly.Negative();

		return temp.Add(mono);

	}

	/// <summary>
	/// Subtracts all monomials within poly2 from poly1
	/// </summary>
	/// <returns>Difference of expressions as polynomial</returns>
	public static PolyEx operator -(PolyEx poly1, PolyEx poly2) {

		PolyEx temp = new PolyEx(poly1);

		return temp.Subtract(poly2);

	}

	#endregion

	#region Multiplication

	/// <summary>
	/// Multiply all terms in a polynomial by a monomial
	/// </summary>
	/// <returns>Product of expressions as a polynomial</returns>
	public static PolyEx operator *(PolyEx poly, MonoEx mono) {

		PolyEx temp = new PolyEx(poly);

		return temp.MultiplyWith(mono);

	}
	/// <summary>
	/// Multiply all terms in a polynomial by a monomial
	/// </summary>
	/// <returns>Product of expressions as a polynomial</returns>
	public static PolyEx operator *(MonoEx mono, PolyEx poly) => poly * mono;

	// TODO: OPTIMIZE A LOT (Delegate to mutable behavior)
	/// <summary>
	/// Multiply two polynomials together
	/// </summary>
	/// <returns>Product of expressions as a polynomial</returns>
	public static PolyEx operator *(PolyEx poly1, PolyEx poly2) {

		PolyEx temp = new PolyEx(poly1);

		return temp.MultiplyWith(poly2);

	}

	/// <summary>
	/// Create the negative version of a polynomial
	/// </summary>
	/// <returns>This polynomial with flipped-sign coefficients</returns>
	public static PolyEx operator -(PolyEx poly) {

		PolyEx temp = new PolyEx(poly);

		return temp.Negative();

	}

	#endregion

	#region Division and Pow

	public static PolyEx operator /(PolyEx poly, double num) {

		PolyEx temp = new PolyEx(poly);

		return temp.DivideBy(num);

	}

	public static PolyEx operator ^(PolyEx poly, int pow) {

		PolyEx temp = new PolyEx(poly);

		return temp.Pow(pow);

	}

	#endregion

	#endregion

	#region Mutable

	#region Addition and Subtraction

	public PolyEx Add(MonoEx mono) {

		InsertMonomial(mono, false);

		return this;

	}

	public PolyEx Add(PolyEx other) {

		foreach (var mono in other.AsSpan())
			InsertMonomial(mono, false);

		return this;

	}

	public PolyEx Subtract(MonoEx mono) {

		InsertMonomial(mono, true);

		return this;

	}

	public PolyEx Subtract(PolyEx other) {

		foreach (var mono in other.AsSpan())
			InsertMonomial(mono, true);

		return this;

	}

	#endregion

	#region Multiplication

	public PolyEx MultiplyWith(MonoEx mono) {

		for (int i = 0; i < termSeries.Count; i++)
			termSeries[i] = termSeries[i] * mono;

		return this;

	}

	public PolyEx MultiplyWith(PolyEx other) {

		Queue<PolyEx> products = new Queue<PolyEx>();

		// go through each term in poly2 and multiply it with poly1
		foreach (MonoEx toMultiplyWith in other.AsSpan())
			products.Enqueue(this * toMultiplyWith);

		this.termSeries.Clear();

		while (products.Count > 0)
			this.Add(products.Dequeue());

		return this;

	}

	public PolyEx Negative() {

		for (int i = 0; i < termSeries.Count; i++)
			termSeries[i] = -termSeries[i];

		return this;

	}

	#endregion

	#region Division and Pow

	public PolyEx DivideBy(double num) {

		for (int i = 0; i < termSeries.Count; i++)
			termSeries[i] = termSeries[i] / num;

		return this;

	}

	public PolyEx Pow(int pow) {

		if (pow <= 0)
			return 1;

		for (int i = 1; i < pow; i++)
			this.MultiplyWith(this);

		return this;

	}

	#endregion

	#endregion

	#endregion

	#region Equality

	public static bool operator ==(PolyEx poly1, PolyEx poly2) {

		if (poly2.Count != poly1.Count)
			return false;

		if (ReferenceEquals(poly1.termSeries, poly2.termSeries))
			return true;

		// we have guaranteed that these polynomials have the same size

		poly1.Sort();
		poly2.Sort();

		var monoSpan1 = poly1.AsSpan();
		var monoSpan2 = poly2.AsSpan();

		for (int i = 0; i < monoSpan1.Length; i++)
			if (monoSpan1[i] != monoSpan2[i])
				return false;

		return true;

	}
	public static bool operator !=(PolyEx poly1, PolyEx poly2) => !( poly1 == poly2 );

	public static bool operator ==(PolyEx poly, MonoEx mono) => poly.termSeries.Count == 1 && poly.termSeries.Last() == mono;
	public static bool operator !=(PolyEx poly, MonoEx mono) => !( poly == mono );
	public static bool operator ==(MonoEx mono, PolyEx poly) => poly == mono;
	public static bool operator !=(MonoEx mono, PolyEx poly) => poly != mono;

	/// <returns>true if other is non-null and MonoEx, and monomials have the same variables, degrees, and coefficient</returns>
	public override bool Equals(object? other) {

		if (other == null || other.GetType() != this.GetType())
			return false;

		// the first 

		var otherMono = (PolyEx)other;

		return otherMono == this;

	}

	// needed for IEquatable
	public bool Equals(PolyEx other) => this.Equals((object)other);

	/// <inheritdoc cref="GetHashCode"/>
	/// <summary>
	/// Generates hash based on all monomials (which is based on their coefficient, all their variables, and their degrees)
	/// </summary>
	public override int GetHashCode() {

		HashCode hash = new HashCode();
		foreach (MonoEx mono in this.AsSpan())
			hash.Add(mono.GetHashCode());

		return hash.ToHashCode();

	}


	#endregion

	#region Accessors

	/// <summary>
	/// total number of monmials that make up this polynomial expression
	/// </summary>
	public int Count => termSeries.Count;

	/// <summary>
	/// return the ordered List of monomials in this polynomial expression (creates a new List)
	/// </summary>
	public List<MonoEx> MonomialList => new List<MonoEx>(termSeries).ToList();

	/// <summary>
	/// returns the highest degree monomial in the polynomial expression.
	/// the degree of a monomial is defined as the sum of the degrees of each of its independent variables
	/// </summary>
	//public NumType Degree => termSeries is not null && termSeries.Count > 0 ? termSeries.Min.Degree : NumType.Zero;

	// allows monos to be read directly without copying
	internal ReadOnlySpan<MonoEx> AsSpan() => CollectionsMarshal.AsSpan(termSeries);

	/// <summary>
	/// access a monomial of the polynomial expression
	/// elements are ordered from highest to lowest degree
	/// </summary>
	/// <param name="idx"></param>
	/// <returns></returns>
	public MonoEx this[int idx] {

		get {

			if (idx >= 0 && idx < termSeries.Count)
				return termSeries[idx];

			throw new IndexOutOfRangeException("Attempted to access out-of-range monomial within polynomial");

		}

	}

	#region IEnumerable

	/// <summary>
	/// Get monomial enumerator for polynomial
	/// </summary>
	/// <returns></returns>
	public IEnumerator<MonoEx> GetEnumerator() {

		foreach (var mono in termSeries)
			yield return mono;

	}

	// not sure what this does
	System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

	#endregion

	#endregion

	#region Helpers

	internal void Sort() => termSeries.Sort();

	#endregion

	#region Stringy

	internal StringBuilder ToStringBuilder() {

		StringBuilder sb = new StringBuilder("");

		if (termSeries.Count == 0)
			return sb.Append(0);

		Sort();

		bool firstNode = true;

		foreach (var mono in this.AsSpan()) {

			if (firstNode) {

				firstNode = false;
				sb.Append(mono.ToStringBuilder());

			}

			else {

				if (mono.Coefficient < 0)
					sb.Append(" - ").Append(mono.AbsToStringBuilder());
				else
					sb.Append(" + ").Append(mono.AbsToStringBuilder());

			}

		}

		return sb;

	}

	/// <returns>String representation of the polynomial</returns>
	public override string ToString() => ToStringBuilder().ToString();

	#endregion

}




