using System.Runtime.InteropServices;

namespace Flexpressions;

/// <summary>
/// <b>A PolyEx (polynomial expression) is a series of MonoEx objects chained together by operators (+/-).</b> <para/>
/// 
/// Polynomials are created by combining expressions.<para/>
/// 
/// Polynomials can be constructed immutably with Whiteboard operators (+, -, *, ^),
/// <br/> or mutably with the functions Add, Subtract, MultiplyWith, DivideBy, and Pow. <para/>
/// 
/// Polynomial expressions can be used in Functions to be evaluated.
/// 
/// </summary>
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

		if (other is not null)
			foreach (ref readonly MonoEx mono in other.AsSpan())
				termSeries.Add(new MonoEx(mono));

		this.termSeries = termSeries;

	}

	#region MonoEx delegates

	/// <summary>
	/// Creates a single-term polynomial with a number (coefficient)
	/// </summary>
	/// <param name="coefficient">coefficient to become MonoEx term</param>
	public PolyEx(double coefficient) : this(new MonoEx(coefficient)) { }

	/// <summary>
	/// Creates a single-term polynomial with a coefficient, a variable, and its degree
	/// </summary>
	/// <param name="coefficient">coefficient of term</param>
	/// <param name="independent">independent variable of term</param>
	/// <param name="degree">degree of term's independent variable</param>
	public PolyEx(double coefficient, Flex independent, int degree = 1) : this(new MonoEx(coefficient, independent, degree)) { }

	/// <summary>
	/// Creates a single-term polynomial with a coefficient 1, a variable, and its degree
	/// </summary>
	/// <param name="independent">independent variable of term</param>
	/// <param name="degree">degree of term's independent variable</param>
	public PolyEx(Flex independent, int degree = 1) : this(new MonoEx(1, independent, degree)) { }

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
	/// <param name="mono">Term to convert to polynomial</param>
	public static implicit operator PolyEx(MonoEx mono) => new PolyEx(mono);
	/// <summary>
	/// Convert a coefficient into a monomial and then into into a single-term polynomial
	/// </summary>
	/// <param name="num">Term to convert to polynomial</param>
	public static implicit operator PolyEx(double num) => new PolyEx(new MonoEx(coefficient: num));
	/// <summary>
	/// Convert a variable into a monomial and then into into a single-term polynomial
	/// </summary>
	/// <param name="idp">Term to convert to polynomial</param>
	public static implicit operator PolyEx(Flex idp) => new PolyEx(new MonoEx(independent: idp, degree: 1));

	#endregion

	#region Addition and Subtraction

	/// <summary>
	/// Add a monomial to a polynomial
	/// </summary>
	/// <returns>Sum of expressions as a new polynomial</returns>
	public static PolyEx operator +(PolyEx poly, in MonoEx mono) {

		PolyEx temp = new PolyEx(poly);

		return temp.Add(mono);

	}
	/// <summary>
	/// Add a monomial to a polynomial
	/// </summary>
	/// <returns>Sum of expressions as a new polynomial</returns>
	public static PolyEx operator +(in MonoEx mono, PolyEx poly) => poly + mono;

	/// <summary>
	/// Adds all monomials from poly2 to poly1
	/// </summary>
	/// <returns>Sum of expressions as a new polynomial</returns>
	public static PolyEx operator +(PolyEx poly1, PolyEx poly2) {

		PolyEx temp = new PolyEx(poly1);

		return temp.Add(poly2);

	}

	/// <summary>
	/// Subtracts a monomial from a polynomial
	/// </summary>
	/// <returns>Difference of expressions as a new polynomial</returns>
	public static PolyEx operator -(PolyEx poly, in MonoEx mono) {

		PolyEx temp = new PolyEx(poly);

		return temp.Subtract(mono);

	}
	/// <summary>
	/// Subtracts a polynomial from a monomial
	/// </summary>
	/// <returns>Difference of expressions as a new polynomial</returns>
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
	/// <returns>Product of expressions as a new polynomial</returns>
	public static PolyEx operator *(PolyEx poly, in MonoEx mono) {

		PolyEx temp = new PolyEx(poly);

		return temp.MultiplyWith(mono);

	}
	/// <summary>
	/// Multiply all terms in a polynomial by a monomial
	/// </summary>
	/// <returns>Product of expressions as a new polynomial</returns>
	public static PolyEx operator *(in MonoEx mono, PolyEx poly) => poly * mono;

	// TODO: OPTIMIZE A LOT (Delegate to mutable behavior)
	/// <summary>
	/// Multiply two polynomials together
	/// </summary>
	/// <returns>Product of expressions as a new polynomial</returns>
	public static PolyEx operator *(PolyEx poly1, PolyEx poly2) {

		PolyEx temp = new PolyEx(poly1);

		return temp.MultiplyWith(poly2);

	}

	/// <summary>
	/// Create the negative version of a polynomial
	/// </summary>
	/// <returns>This polynomial with flipped-sign coefficients as a new polynomial</returns>
	public static PolyEx operator -(PolyEx poly) {

		PolyEx temp = new PolyEx(poly);

		return temp.Negative();

	}

	#endregion

	#region Division and Pow

	/// <summary>
	/// Divide a polynomial by a constant 
	/// </summary>
	/// /// <param name="poly">Polynomial dividend</param>
	/// <param name="num">Constant expression divisor</param>
	/// <returns>Quotient of operation as new polynomial</returns>
	public static PolyEx operator /(PolyEx poly, double num) {

		PolyEx temp = new PolyEx(poly);

		return temp.DivideBy(num);

	}

	/// <summary>
	/// Take polynomial to a power
	/// </summary>
	/// <param name="poly">Polynomial expression</param>
	/// <param name="pow">Exponent to take poynomial to</param>
	/// <returns>Power of polynomial as new polynomial</returns>
	public static PolyEx operator ^(PolyEx poly, int pow) {

		PolyEx temp = new PolyEx(poly);

		return temp.Pow(pow);

	}

	#endregion

	#endregion

	#region Mutable

	#region Addition and Subtraction

	/// <summary>
	/// Add a monomial to this polyonomial (mutates this)
	/// </summary>
	/// <param name="mono">Monomial to add to this</param>
	/// <returns>this</returns>
	public PolyEx Add(in MonoEx mono) {

		InsertMonomial(mono, false);

		return this;

	}

	/// <summary>
	/// Add a polynomial to this polyonomial (mutates this)
	/// </summary>
	/// <param name="other">Polynomial to add to this</param>
	/// <returns>this</returns>
	public PolyEx Add(PolyEx other) {

		if (other is not null)
			foreach (ref readonly var mono in other.AsSpan())
				InsertMonomial(mono, false);

		return this;

	}

	/// <summary>
	/// Subtract a monomial from this polynomial (mutates this)
	/// </summary>
	/// <param name="mono">Monomial to subtract from this</param>
	/// <returns>this</returns>
	public PolyEx Subtract(in MonoEx mono) {

		InsertMonomial(mono, true);

		return this;

	}

	/// <summary>
	/// Subtract a polynomial from this polynomial (mutates this)
	/// </summary>
	/// <param name="other">Polynomial to subtract from this</param>
	/// <returns>this</returns>
	public PolyEx Subtract(PolyEx other) {

		if (other is not null)
			foreach (ref readonly var mono in other.AsSpan())
				InsertMonomial(mono, true);

		return this;

	}

	#endregion

	#region Multiplication

	/// <summary>
	/// Multiply this polynomial with a monomial (mutates this)
	/// </summary>
	/// <param name="mono">Monomial to multiply with</param>
	/// <returns>this</returns>
	public PolyEx MultiplyWith(in MonoEx mono) {

		for (int i = 0; i < termSeries.Count; i++)
			termSeries[i] = termSeries[i] * mono;

		return this;

	}

	/// <summary>
	/// Multiply this polynomial with another polynomial (mutates this)
	/// </summary>
	/// <param name="other">Polynomial to multiply with</param>
	/// <returns>this</returns>
	public PolyEx MultiplyWith(PolyEx other) { //todo: optimize to have no aux data 

		MonoEx[] temp = termSeries.ToArray();

		termSeries.Clear();

		foreach (MonoEx mono in temp)
			foreach (ref readonly MonoEx toMultiplyWith in other.AsSpan())
				InsertMonomial(mono * toMultiplyWith);

		return this;

	}

	/// <summary>
	/// Turn this polynomial into the negative version of itself (mutates this)
	/// </summary>
	/// <returns>this</returns>
	public PolyEx Negative() {

		for (int i = 0; i < termSeries.Count; i++)
			termSeries[i] = -termSeries[i];

		return this;

	}

	#endregion

	#region Division and Pow

	/// <summary>
	/// Divide this polynomial by a constant number (mutates this)
	/// </summary>
	/// <param name="num">Constant to divide by</param>
	/// <returns>this</returns>
	public PolyEx DivideBy(double num) {

		for (int i = 0; i < termSeries.Count; i++)
			termSeries[i] = termSeries[i] / num;

		return this;

	}

	/// <summary>
	/// Take this polynomial to a power (mutates this)
	/// </summary>
	/// <param name="pow">Exponent to take polynomial to</param>
	/// <returns>this</returns>
	public PolyEx Pow(int pow) {

		if (pow < 0)
			throw new InvalidOperationException("Cannot take a polynomial to a negative power!");

		if (pow == 0) {

			termSeries.Clear();
			termSeries.Add(1);

			return this;

		}

		PolyEx result = 1;
		PolyEx currentProduct = this;

		while (pow > 0) {

			if (( pow & 1 ) == 1)
				result *= currentProduct;

			currentProduct *= currentProduct;
			pow >>= 1;

		}

		return result;
	}

	#endregion

	#endregion

	#endregion

	#region Equality

	/// <returns>true if both polynomials are the same object or have the same terms</returns>
	public static bool operator ==(PolyEx poly1, PolyEx poly2) {

		if (ReferenceEquals(poly1, poly2))
			return true;

		if (poly1 is null || poly2 is null || poly1.Count != poly2.Count)
			return false;

		if (ReferenceEquals(poly1.termSeries, poly2.termSeries)) // should never be true tbh
			return true;

		// we have guaranteed that these polynomials have the same size

		// i was sorting in the old version, but moved to a good old n^2 brute force search
		//		to prevent mutation in ==

		var poly2Span = poly2.AsSpan();

		foreach (ref readonly var monoToFind in poly1.AsSpan())
			// apparently span.contains has low level optimizations with IEquatable which make it better than doing a normal loop
			if (!poly2Span.Contains(monoToFind))
				return false;

		return true;

	}
	/// <returns>false if both polynomials are the same object or have the same terms</returns>
	public static bool operator !=(PolyEx poly1, PolyEx poly2) => !( poly1 == poly2 );

	/// <returns>true if the polynomial has one term, and that term is equal to mono</returns>
	public static bool operator ==(PolyEx poly, in MonoEx mono) => poly is not null && poly.termSeries.Count == 1 && poly.termSeries[0] == mono;
	/// <returns>false if the polynomial has one term, and that term is equal to mono</returns>
	public static bool operator !=(PolyEx poly, in MonoEx mono) => !( poly == mono );

	/// <returns>true if the polynomial has one term, and that term is equal to mono</returns>
	public static bool operator ==(MonoEx mono, PolyEx poly) => poly == mono;
	/// <returns>false if the polynomial has one term, and that term is equal to mono</returns>
	public static bool operator !=(MonoEx mono, PolyEx poly) => poly != mono;

	/// <returns>true if other is non-null, is PolyEx, and the PolyEx has all the same terms as this</returns>
	public override bool Equals(object? other) {

		if (other is null || other.GetType() != this.GetType())
			return false;

		// the first 

		var otherMono = (PolyEx)other;

		return otherMono == this;

	}

	// needed for IEquatable
	/// <returns>true if other is non-null, is PolyEx, and the PolyEx has all the same terms as this</returns>
	public bool Equals(PolyEx? other) => this.Equals((object?)other);

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
	/// total number of monomials that make up this polynomial expression
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