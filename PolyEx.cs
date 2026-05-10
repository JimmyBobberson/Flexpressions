using System;
using System.Diagnostics;
using System.Numerics;

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
public readonly struct PolyEx<NumType> where NumType : INumber<NumType> {

	#region Static Helpers

	private const char MINUS = '-';
	private const char PLUS = '+';

	#endregion

	#region State and Constructors

	/// <summary>
	/// PolyEx stores monomials as MonoNodes which know the operator that precedes them, null if first term in a given polynomial
	/// </summary>
	/// <typeparam name="NumType"></typeparam>
	private record struct MonoNode : IComparable {

		private MonoEx<NumType> mono;
		private char? symbol;

		public MonoNode(char? symbol, MonoEx<NumType> mono) {

			this.symbol = symbol;

			if (symbol is not null && ( ( symbol == MINUS && mono.Coefficient < NumType.Zero ) || ( symbol == PLUS && mono.Coefficient == NumType.Zero ) )) {

				this.mono = mono.Flipped();
				this.symbol = ( this.symbol == PLUS ) ? MINUS : PLUS;

			}
			else
				this.mono = mono;

		}

		override public string ToString() => ( ( symbol != null ) ? symbol + " " : "" )
											+ mono + " ";

		/// <summary>
		/// Set symbol to null. If symbol was minus, associated mono absorbs it, flipping its sign. <br/>
		/// Does nothing if symbol was already null
		/// </summary>
		public void RemoveSymbol() {

			if (symbol is not null && symbol == MINUS)
				mono = mono.Flipped();

			symbol = null;

		}

		public char? Symbol {

			get => symbol;

			set => symbol = value;

		}

		/// <summary>
		/// Access the monomial of this MonoNode, adjusted for its sign 
		/// (i.e. if this is a positive term that follows subtraction, you will recieve a negative monomial)
		/// (if you just need to access the degree of the monomial, use this.Degree for efficiency)
		/// </summary>
		public MonoEx<NumType> Mono {

			get {

				// if the symbol is null just return the mono as-is 
				if (symbol != null) {

					// if the symbol is not null, factor subtraction into the coefficient

					bool followsPlus = ( symbol is null || symbol == PLUS );

					// technically in CombineMonomials all subtractions of negatives become addition,
					//		and additions of negative become subtraction. that means no terms in a polynomial
					//		are negative and the sign is actually represented by the operator in the node.
					//		the first term can be negative, though 

					// term is positive and follows minus, return negative of term for proper representation
					if (mono.Coefficient > NumType.Zero && symbol == MINUS)
						return mono.Flipped();

					// term is negative and follows minus, flip sign to positive for proper representation
					// this should technically never happen based on the MonoNode constructor
					if (mono.Coefficient < NumType.Zero && !followsPlus)
						return mono.Flipped();

				}

				// default case
				return mono;

			}

			set => mono = value;

		}

		// more efficient and preferred over calling Mono.Degree
		public NumType Degree => mono.Degree;

		public int CompareTo(object? other) {

			if (other == null)
				return -1;

			MonoNode otherNode = (MonoNode)other;

			if (other == null)
				throw new ArgumentException("Object is not a MonoEx, cannot compare to other MonoEx!");

			return Mono.CompareTo(otherNode.Mono);

		}

	}

	private readonly SortedSet<MonoNode> termSeries;

	private PolyEx(SortedSet<MonoNode> termSeries) => this.termSeries = termSeries;

	private PolyEx(MonoNode term) {

		SortedSet<MonoNode> termSeries = new SortedSet<MonoNode>();

		termSeries.Add(term);

		this.termSeries = termSeries;

	}

	#endregion

	#region Operators

	// mono to poly
	public static implicit operator PolyEx<NumType>(MonoEx<NumType> mono) => new PolyEx<NumType>(new MonoNode(null, mono));

	/// <summary>
	/// performs mono1 (+/-) mono2 and returns the resulting polynomial expression
	/// </summary>
	/// <param name="mono1"></param>
	/// <param name="mono2"></param>
	/// <param name="subtract"></param>
	/// <returns></returns>
	public static PolyEx<NumType> CombineMonomials(MonoEx<NumType> mono1, MonoEx<NumType> mono2, bool subtract = false) {

		SortedSet<MonoNode> termSeries = new SortedSet<MonoNode>();

		// if the monomials are like terms, just sum their coefficients
		// resulting polynomial will only have 1 term, but its best if we always return a polynomial when we do +/- 
		if (mono1.IsLike(mono2)) {

			// add first to second
			NumType coefficientSum = mono1.Coefficient + ( mono2.Coefficient * ( subtract ? NumType.CreateChecked(-1) : NumType.One ) );

			termSeries.Add(new MonoNode(null, new MonoEx<NumType>(mono1, coefficientSum)));

		}
		else {

			MonoNode node1 = new MonoNode(PLUS, mono1);
			MonoNode node2 = new MonoNode(subtract ? MINUS : PLUS, mono2);

			termSeries.Add(node1);
			termSeries.Add(node2);

		}

		return new PolyEx<NumType>(termSeries);

	}

	private static PolyEx<NumType> InsertMonomialInto(PolyEx<NumType> poly, MonoEx<NumType> mono, bool subtract = false) {

		MonoNode toAdd = new MonoNode(subtract ? MINUS : PLUS, mono);

		// the add operation will fail if you try to add a monomial that is already in the polynomial,
		// in which case we remove that term, double it, then add it back
		if (!poly.termSeries.Add(toAdd)) {

			poly.termSeries.Remove(toAdd);

			poly.termSeries.Add(new MonoNode(subtract ? MINUS : PLUS, mono * NumType.CreateChecked(2)));

		}

		return poly;

	}

	public static PolyEx<NumType> operator +(PolyEx<NumType> poly, MonoEx<NumType> mono) => InsertMonomialInto(poly, mono);
	public static PolyEx<NumType> operator +(PolyEx<NumType> poly1, PolyEx<NumType> poly2) {

		foreach (MonoNode node in poly2.termSeries)
			InsertMonomialInto(poly1, node.Mono);

		return poly1;

	}

	public static PolyEx<NumType> operator -(PolyEx<NumType> poly, MonoEx<NumType> mono) => InsertMonomialInto(poly, mono, true);
	public static PolyEx<NumType> operator -(PolyEx<NumType> poly1, PolyEx<NumType> poly2) {

		foreach (MonoNode node in poly2.termSeries)
			InsertMonomialInto(poly1, node.Mono, true);

		return poly1;

	}


	/// <summary>
	/// access an monomial of the polynomial expression
	/// elements are ordered from highest to lowest degree
	/// </summary>
	/// <param name="index"></param>
	/// <returns></returns>
	/*public MonoEx<NumType> this[int idx] {

		get {

			if (idx >= 0 && idx < termSeries.Count)
				return termSeries.

			throw new IndexOutOfRangeException("Attempted to access out-of-range monomial within polynomial");

		}

	}*/

	#endregion

	#region Accessors

	/// <summary>
	/// total number of monmials that make up this polynomial expression
	/// </summary>
	public int Length => termSeries.Count;

	/// <summary>
	/// return the ordered SortedSet of monomials in this polynomial expression (creates a new SortedSet)
	/// </summary>
	public SortedSet<MonoEx<NumType>> MonomialSortedSet => new SortedSet<MonoEx<NumType>>(termSeries.Select(monoNode => monoNode.Mono));


	/// <summary>
	/// returns the highest degree monomial in the polynomial expression.
	/// the degree of a monomial is defined as the sum of the degrees of each of its independent variables
	/// </summary>
	public NumType Degree => termSeries is not null && termSeries.Count > 0 ? termSeries.Min.Degree : NumType.Zero;

	#endregion

	public override string ToString() {

		string ret = "";

		bool firstNode = true;
		foreach (var MonoNode in termSeries) {

			if (firstNode) {

				MonoNode.RemoveSymbol(); // this should be called somewhere better
				firstNode = false;

			}


			ret += MonoNode;

		}


		return ret;

	}

}




