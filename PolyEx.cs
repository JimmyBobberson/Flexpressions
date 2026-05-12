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
/// A PolyEx can use any type that implements INumber (int, float, double, etc)
/// and all MonoEx objects within it will use the same type. <br/>
/// 
/// Polynomial expressions can be used in functions.<br/>
/// 
/// Supports indexed accessing for terms. <br/>
/// 
/// PolyEx objects are immutable and all operations return a new object.<br/>
/// </summary>
/// <typeparam name="NumType"></typeparam>
public readonly struct PolyEx<NumType> : IEnumerable<MonoEx<NumType>> where NumType : INumber<NumType> {

	#region Static Helpers

	private const char MINUS = '-';
	private const char PLUS = '+';

	#endregion

	#region State and Constructors

	// pseudoalias wrapper
	private class MonoSeries : List<MonoEx<NumType>> {

		public MonoSeries() : base() { }
		public MonoSeries(MonoSeries other) : base(other) { }
		public MonoSeries(int cap) : base(cap) { }

	}

	private readonly MonoSeries termSeries;

	private PolyEx(MonoSeries termSeries) => this.termSeries = termSeries;

	private PolyEx(MonoEx<NumType> term) {

		MonoSeries termSeries = new MonoSeries();

		termSeries.Add(term);

		this.termSeries = termSeries;

	}

	public PolyEx() => termSeries = new MonoSeries();

	#endregion

	#region Operators

	// mono to poly
	public static implicit operator PolyEx<NumType>(MonoEx<NumType> mono) => new PolyEx<NumType>(mono);
	public static implicit operator PolyEx<NumType>(NumType num) => new PolyEx<NumType>(new MonoEx<NumType>(coefficient: num));
	public static implicit operator PolyEx<NumType>(Flex<NumType> idp) => new PolyEx<NumType>(new MonoEx<NumType>(independent: idp, degree: NumType.One));

	/// <summary>
	/// performs mono1 (+/-) mono2 and returns the resulting polynomial expression
	/// </summary>
	/// <param name="mono1"></param>
	/// <param name="mono2"></param>
	/// <param name="subtract"></param>
	/// <returns></returns>
	public static PolyEx<NumType> CombineMonomials(MonoEx<NumType> mono1, MonoEx<NumType> mono2, bool subtract = false) {

		MonoSeries termSeries = new MonoSeries(2);

		// if the monomials are like terms, just sum their coefficients
		// resulting polynomial will only have 1 term, but its best if we always return a polynomial when we do +/- 
		if (mono1.IsLike(mono2)) {

			// add first to second
			NumType coefficientSum = mono1.Coefficient + ( mono2.Coefficient * ( subtract ? NumType.CreateChecked(-1) : NumType.One ) );

			termSeries.Add(new MonoEx<NumType>(mono1, coefficientSum));

		}
		else {

			mono2 = subtract ? new MonoEx<NumType>(mono2, mono2.Coefficient * NumType.CreateChecked(-1)) : mono2;

			termSeries.Add(mono1);
			termSeries.Add(mono2);

		}

		return new PolyEx<NumType>(termSeries);

	}

	private static PolyEx<NumType> InsertMonomialInto(PolyEx<NumType> poly, MonoEx<NumType> toInsert, bool subtract = false) {

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

				NumType coefficientSum = mono.Coefficient + ( toInsert.Coefficient * ( subtract ? NumType.CreateChecked(-1) : NumType.One ) );

				if (coefficientSum == NumType.Zero)
					idxToRemove = i;
				else
					monoSeries[i] = new MonoEx<NumType>(toInsert, coefficientSum);

				break;

			}
		}

		if (idxToRemove != null)
			monoSeries.RemoveAt((int)idxToRemove);
		// toInsert is not a like term of any element in the polynomial so we will insert it normally
		else if (!foundLikeTerm)
			monoSeries.Add(toInsert);

		return new PolyEx<NumType>(monoSeries);

	}

	public static PolyEx<NumType> operator +(PolyEx<NumType> poly, MonoEx<NumType> mono) => InsertMonomialInto(poly, mono);
	public static PolyEx<NumType> operator +(PolyEx<NumType> poly1, PolyEx<NumType> poly2) {

		PolyEx<NumType> polySum = new PolyEx<NumType>();

		// make a copy of poly1 
		foreach (var mono in poly1.AsSpan())
			polySum.termSeries.Add(mono);

		// add each mono from poly2 into the copy of poly1 
		foreach (var mono in poly2.AsSpan())
			polySum = InsertMonomialInto(polySum, mono);

		return polySum;

	}

	public static PolyEx<NumType> operator -(PolyEx<NumType> poly, MonoEx<NumType> mono) => InsertMonomialInto(poly, mono, true);
	public static PolyEx<NumType> operator -(PolyEx<NumType> poly1, PolyEx<NumType> poly2) {

		PolyEx<NumType> polySum = new PolyEx<NumType>();

		// make a copy of poly1 
		foreach (var mono in poly1.AsSpan())
			polySum.termSeries.Add(mono);

		// subtract each mono from poly2 into the copy of poly1 
		foreach (var mono in poly2.AsSpan())
			polySum = InsertMonomialInto(polySum, mono, true);

		return polySum;

	}

	public static PolyEx<NumType> operator -(PolyEx<NumType> poly) {

		MonoSeries monoSeries = new MonoSeries(poly.Count);

		foreach (var mono in poly.AsSpan())
			monoSeries.Add(-mono);

		return new PolyEx<NumType>(monoSeries);

	}

	/// <summary>
	/// access an monomial of the polynomial expression
	/// elements are ordered from highest to lowest degree
	/// </summary>
	/// <param name="index"></param>
	/// <returns></returns>
	public MonoEx<NumType> this[int idx] {

		get {

			if (idx >= 0 && idx < termSeries.Count)
				return termSeries[idx];

			throw new IndexOutOfRangeException("Attempted to access out-of-range monomial within polynomial");

		}

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
	public List<MonoEx<NumType>> MonomialList => new List<MonoEx<NumType>>(termSeries).ToList();

	/// <summary>
	/// returns the highest degree monomial in the polynomial expression.
	/// the degree of a monomial is defined as the sum of the degrees of each of its independent variables
	/// </summary>
	//public NumType Degree => termSeries is not null && termSeries.Count > 0 ? termSeries.Min.Degree : NumType.Zero;

	// allows memory to be read directly 
	internal ReadOnlySpan<MonoEx<NumType>> AsSpan() => CollectionsMarshal.AsSpan(termSeries);

	public IEnumerator<MonoEx<NumType>> GetEnumerator() {

		foreach (var mono in termSeries)
			yield return mono;

	}

	// not sure what this does
	System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

	#endregion

	public override string ToString() {

		if (termSeries.Count == 0)
			return "" + NumType.Zero;

		string ret = "";

		bool firstNode = true;

		foreach (var mono in this.AsSpan()) {

			if (firstNode) {

				firstNode = false;
				ret += mono;

			}

			else {

				if (mono.Coefficient < NumType.Zero)
					ret += " - " + mono.AbsToString();
				else
					ret += " + " + mono.AbsToString();

			}

		}

		return ret;

	}

}




