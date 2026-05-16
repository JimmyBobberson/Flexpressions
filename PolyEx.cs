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
/// PolyEx objects are immutable and all operations return a new object.<br/>
/// </summary>
/// 
// todo: ICollection
public struct PolyEx : IReadOnlyCollection<MonoEx>, IEquatable<PolyEx> {

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

	internal PolyEx(MonoEx term) {

		MonoSeries termSeries = new MonoSeries();

		termSeries.Add(term);

		this.termSeries = termSeries;

	}

	/// <summary>
	/// Instantiates the polynomial 0
	/// </summary>
	public PolyEx() => termSeries = new MonoSeries();

	internal PolyEx(PolyEx other) {

		MonoSeries termSeries = new MonoSeries();

		foreach (MonoEx mono in other.termSeries)
			termSeries.Add(new MonoEx(mono));

		this.termSeries = termSeries;

	}
	#endregion

	#region Operators (Public Interface for Construction)

	// All polynomial operators delegate to the below two master functions
	// (or construct their resultants directly) 

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

	private static PolyEx InsertMonomialInto(PolyEx poly, MonoEx toInsert, bool subtract = false) {

		// create series to be used for new PolyEx as a copy of poly's series
		MonoSeries monoSeries = new(poly.Count + 1);
		foreach (var node in poly.AsSpan())
			monoSeries.Add(node);

		// see if poly contains a term like toInsert 
		int? idxToRemove = null;
		bool foundLikeTerm = false;
		for (int i = 0; i < monoSeries.Count; i++) {

			var mono = monoSeries[i];

			if (mono.IsLike(toInsert)) {

				foundLikeTerm = true;

				double coefficientSum = mono.Coefficient + ( toInsert.Coefficient * ( subtract ? -1 : 1 ) );

				if (coefficientSum == 0)
					idxToRemove = i;
				else
					monoSeries[i] = new MonoEx(toInsert, coefficientSum);

				break;

			}
		}

		if (idxToRemove != null)
			monoSeries.RemoveAt((int)idxToRemove);
		// toInsert is not a like term of any element in the polynomial so we will insert it normally
		else if (!foundLikeTerm)
			monoSeries.Add(subtract ? toInsert.Flipped() : toInsert);

		return new PolyEx(monoSeries); // runs sort

	}

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

	/// <summary>
	/// Add a monomial to a polynomial
	/// </summary>
	/// <returns>Sum of expressions as polynomial</returns>
	public static PolyEx operator +(PolyEx poly, MonoEx mono) => InsertMonomialInto(poly, mono);
	/// <summary>
	/// Adds all monomials from poly2 to poly1
	/// </summary>
	/// <returns>Sum of expressions as polynomial</returns>
	public static PolyEx operator +(PolyEx poly1, PolyEx poly2) {

		PolyEx polySum = new PolyEx();

		// make a copy of poly1 
		foreach (var mono in poly1.AsSpan())
			polySum.termSeries.Add(mono);

		// add each mono from poly2 into the copy of poly1 
		foreach (var mono in poly2.AsSpan())
			polySum = InsertMonomialInto(polySum, mono);

		return polySum;

	}

	/// <summary>
	/// Subtracts a monomial from a polynomial
	/// </summary>
	/// <returns>Difference of expressions as polynomial</returns>
	public static PolyEx operator -(PolyEx poly, MonoEx mono) => InsertMonomialInto(poly, mono, true);
	/// <summary>
	/// Subtracts all monomials within poly2 from poly1
	/// </summary>
	/// <returns>Difference of expressions as polynomial</returns>
	public static PolyEx operator -(PolyEx poly1, PolyEx poly2) {

		PolyEx polySum = new PolyEx();

		// make a copy of poly1 
		foreach (var mono in poly1.AsSpan())
			polySum.termSeries.Add(mono);

		// subtract each mono from poly2 into the copy of poly1 
		foreach (var mono in poly2.AsSpan())
			polySum = InsertMonomialInto(polySum, mono, true);

		return polySum;

	}

	/// <summary>
	/// Create the negative version of a polynomial
	/// </summary>
	/// <returns>This polynomial with flipped-sign coefficients</returns>
	public static PolyEx operator -(PolyEx poly) {

		MonoSeries monoSeries = new MonoSeries(poly.Count);

		foreach (var mono in poly.AsSpan())
			monoSeries.Add(-mono);

		return new PolyEx(monoSeries);

	}

	/// <summary>
	/// Multiply all terms in a polynomial by a monomial
	/// </summary>
	/// <returns>Product of expressions as a polynomial</returns>
	public static PolyEx operator *(PolyEx poly, MonoEx mono) {

		MonoSeries series = new MonoSeries();

		// multiply each monomial in poly with mono
		foreach (MonoEx toMultiplyWith in poly.AsSpan())
			series.Add(toMultiplyWith * mono);

		return new PolyEx(series);

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

		Queue<PolyEx> products = new Queue<PolyEx>();

		// go through each term in poly2 and multiply it with poly1
		foreach (MonoEx toMultiplyWith in poly2.AsSpan())
			products.Enqueue(poly1 * toMultiplyWith);



		PolyEx finalProduct = products.Dequeue();

		while (products.Count > 0)
			finalProduct += products.Dequeue();

		return finalProduct;

	}

	public static PolyEx operator ^(PolyEx poly, int pow) {

		if (pow <= 0)
			return 1;

		PolyEx result = poly;

		for (int i = 1; i < pow; i++)
			result = result * poly;

		return result;

	}

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
	public bool Equals(PolyEx other) => this.Equals(other);

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




