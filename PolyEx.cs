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

	private MonoSeries termSeries;

	private PolyEx(MonoSeries termSeries) => this.termSeries = termSeries;

	/// <summary>
	/// Creates a single-term polynomial
	/// </summary>
	/// <param name="term">monomial term to become polynomial</param>
	public PolyEx(in MonoEx term) {

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
	public PolyEx(double coefficient) : this(new MonoEx(coefficient)) { }

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
	public PolyEx(Flex independent, int degree) : this(new MonoEx(independent, degree)) { }

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
	internal static PolyEx CombineMonomials(in MonoEx mono1, in MonoEx mono2, bool subtract = false) {

		MonoSeries termSeries = new MonoSeries(2);

		// if the monomials are like terms, just sum their coefficients
		// resulting polynomial will only have 1 term, but its best if we always return a polynomial when we do +/- 
		if (mono1.IsLike(mono2)) {

			// add first to second
			double coefficientSum = mono1.Coefficient + ( mono2.Coefficient * ( subtract ? -1 : 1 ) );

			termSeries.Add(new MonoEx(mono1, coefficientSum));

		}
		else {

			MonoEx toAdd = subtract ? mono2.Flipped() : mono2;

			termSeries.Add(mono1);
			termSeries.Add(toAdd);

		}

		return new PolyEx(termSeries);

	}

	private void InsertMonomial(in MonoEx toInsert, bool subtract = false) {

		// create series to be used for new PolyEx as a copy of poly's series

		// see if poly contains a term like toInsert 
		int? idxToRemove = null;
		bool foundLikeTerm = false;
		for (int i = termSeries.Count - 1; i >= 0; i--)
			if (termSeries[i].IsLike(toInsert)) {

				foundLikeTerm = true;

				double coefficientSum = termSeries[i].Coefficient + ( toInsert.Coefficient * ( subtract ? -1 : 1 ) );

				if (coefficientSum == 0)
					idxToRemove = i;
				else
					termSeries[i] = new MonoEx(toInsert, coefficientSum);

				break;

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
	public static PolyEx operator +(PolyEx poly, in MonoEx mono) {

		PolyEx temp = new PolyEx(poly);

		return temp.Add(mono);

	}
	public static PolyEx operator +(in MonoEx mono, PolyEx poly) => poly + mono;

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
	public static PolyEx operator -(PolyEx poly, in MonoEx mono) {

		PolyEx temp = new PolyEx(poly);

		return temp.Subtract(mono);

	}
	public static PolyEx operator -(in MonoEx mono, PolyEx poly) {

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
	public static PolyEx operator *(PolyEx poly, in MonoEx mono) {

		PolyEx temp = new PolyEx(poly);

		return temp.MultiplyWith(mono);

	}
	/// <summary>
	/// Multiply all terms in a polynomial by a monomial
	/// </summary>
	/// <returns>Product of expressions as a polynomial</returns>
	public static PolyEx operator *(in MonoEx mono, PolyEx poly) => poly * mono;

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

	public PolyEx Add(in MonoEx mono) {

		InsertMonomial(mono, false);

		return this;

	}

	public PolyEx Add(PolyEx other) {

		foreach (ref readonly var mono in other.AsSpan())
			InsertMonomial(mono, false);

		return this;

	}

	public PolyEx Subtract(in MonoEx mono) {

		InsertMonomial(mono, true);

		return this;

	}

	public PolyEx Subtract(PolyEx other) {

		foreach (ref readonly var mono in other.AsSpan())
			InsertMonomial(mono, true);

		return this;

	}

	#endregion

	#region Multiplication

	public PolyEx MultiplyWith(in MonoEx mono) {

		for (int i = 0; i < termSeries.Count; i++)
			termSeries[i] = termSeries[i] * mono;

		return this;

	}

	public PolyEx MultiplyWith(PolyEx other) {

		Queue<PolyEx> products = new Queue<PolyEx>();

		// go through each term in poly2 and multiply it with poly1
		foreach (ref readonly MonoEx toMultiplyWith in other.AsSpan())
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

	// todo: exponent by squaring
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

		// i was sorting in the old version, but moved to a good old n^2 brute force search
		//		to prevent mutation in ==

		foreach (ref readonly var monoToFind in poly1.AsSpan()) {

			bool foundMonoInOther = false;

			foreach (ref readonly var monoFound in poly2.AsSpan())
				if (monoFound == monoToFind)
					foundMonoInOther = true;

			if (!foundMonoInOther)
				return false;

		}

		return true;

	}
	public static bool operator !=(PolyEx poly1, PolyEx poly2) => !( poly1 == poly2 );

	public static bool operator ==(PolyEx poly, in MonoEx mono) => poly.termSeries.Count == 1 && poly.termSeries.Last() == mono;
	public static bool operator !=(PolyEx poly, in MonoEx mono) => !( poly == mono );
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
		foreach (ref readonly MonoEx mono in this.AsSpan())
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
	System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => termSeries.GetEnumerator();

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

		foreach (ref readonly var mono in this.AsSpan()) {

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