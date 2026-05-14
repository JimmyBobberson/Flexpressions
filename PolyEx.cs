using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;

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
/// <typeparam name="NumType"></typeparam>
/// 
// todo: ICollection
// todo: IEquatable
public readonly struct PolyEx : IEnumerable<MonoEx> {

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

	internal PolyEx() => termSeries = new MonoSeries();

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

	/// <summary>
	/// performs mono1 (+/-) mono2 and returns the resulting polynomial expression
	/// </summary>
	/// <param name="mono1"></param>
	/// <param name="mono2"></param>
	/// <param name="subtract"></param>
	/// <returns></returns>
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

		return new PolyEx(termSeries);

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

		return new PolyEx(monoSeries);

	}

	public static implicit operator PolyEx(MonoEx mono) => new PolyEx(mono);
	public static implicit operator PolyEx(double num) => new PolyEx(new MonoEx(coefficient: num));
	public static implicit operator PolyEx(Flex idp) => new PolyEx(new MonoEx(independent: idp, degree: 1));

	public static PolyEx operator +(PolyEx poly, MonoEx mono) => InsertMonomialInto(poly, mono);
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

	public static PolyEx operator -(PolyEx poly, MonoEx mono) => InsertMonomialInto(poly, mono, true);
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

	public static PolyEx operator -(PolyEx poly) {

		MonoSeries monoSeries = new MonoSeries(poly.Count);

		foreach (var mono in poly.AsSpan())
			monoSeries.Add(-mono);

		return new PolyEx(monoSeries);

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
	/// <param name="index"></param>
	/// <returns></returns>
	public MonoEx this[int idx] {

		get {

			if (idx >= 0 && idx < termSeries.Count)
				return termSeries[idx];

			throw new IndexOutOfRangeException("Attempted to access out-of-range monomial within polynomial");

		}

	}

	#region IEnumerable

	public IEnumerator<MonoEx> GetEnumerator() {

		foreach (var mono in termSeries)
			yield return mono;

	}

	// not sure what this does
	System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

	#endregion

	#endregion

	#region Stringy

	public override string ToString() {

		if (termSeries.Count == 0)
			return "" + 0;

		string ret = "";

		bool firstNode = true;

		foreach (var mono in this.AsSpan()) {

			if (firstNode) {

				firstNode = false;
				ret += mono;

			}

			else {

				if (mono.Coefficient < 0)
					ret += " - " + mono.AbsToString();
				else
					ret += " + " + mono.AbsToString();

			}

		}

		return ret;

	}

	#endregion

}




