namespace Flexpressions;

/// <summary>
/// <b>A MonoEx (monomial expression) is the product of a coefficient and a set of independent variables (each with a degree).</b> <para/>
/// MonoEx objects are the fundamental building blocks of Flexpressions.<para/>
/// All powers are doubles (rounded) and all powers are integers <para/>
/// MonoEx objects are immutable and all operations return a new object.<para/>
/// </summary>

// todo: IParsable
// todo: INumber
public readonly struct MonoEx : IComparable, IEquatable<MonoEx> {

	#region Static Config and Helper Stuff

	// exponent config
	static private readonly bool EXPONENTS_ARE_SUPERSCRIPTS = false;

	// smth in here is broken
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

	internal static double ForcePrecision(double val) => double.Round(val, DECIMAL_PRECISION, MidpointRounding.AwayFromZero);

	#endregion

	#region State and Constructors

	// maps independent variables to their degree
	// this is being used as a optimized, gc-free array
	// it does restrict each monomial to only having up to 8 vars (one per byte in a ulong)
	//		and each var can only have a degree of up to 8 (biggest int that can be stored in a byte),
	//		but this covers 99% of use cases!
	internal record struct DegreeList : IReadOnlyCollection<int> {

		// notes to self:
		// &: bit lines up with 0 in mask, bit becomes 0.
		// |: bit lines up with 1 in mask, bit becomes 1.

		#region Static Helpers

		internal static DegreeList MakeDegreeList(Flex[] independent, params int[] degree) {

			if (independent.Length != degree.Length)
				throw new ArgumentException("MakeDegreeList requires the same number of independent variables and degrees!");

			DegreeList ret = new DegreeList();

			for (int i = 0; i < independent.Length; i++)
				ret[independent[i].Id] = degree[i];

			return ret;

		}

		#endregion

		#region Constants, State, and Constructors

		private const int BITS_PER_ELEM = 8; // every 8 bits is its own int
		private const ulong MASK = 0xFF; // hex for 255, aka max value per 8 bits. 11111111

		private ulong packedInts = 0UL;

		public DegreeList(ulong packedInts) => this.packedInts = packedInts;
		public DegreeList() : this(0UL) { }

		#endregion

		#region Accessors

		public int this[int index] {
			get {

				if (index >= Flex.NumVars || index < 0)
					throw new ArgumentOutOfRangeException("Attempted to access a monomial variable out of bounds!");

				// we need to move "index" positions
				int shift = index * BITS_PER_ELEM;

				// take packedInts and shift it to the right "shift" bytes
				//		the "shift" bytes of data on the right of the element at the index get thrown away
				//		now we just have a bunch of trailing zeroes, then the bit data we care about in the last
				//		BITS_PER_ELEM bits of the shifted ulong.
				// finally, apply the mask to the shifted ulong. now, everything on the left
				//		of the rightmost BITS_PER_ELEM bits get erased. Now we have the bit representation
				//		of the value at the index. Cast to int and return.
				return (int)( ( packedInts >> shift ) & MASK );

			}
			set {

				if (index >= Flex.NumVars || index < 0)
					throw new ArgumentOutOfRangeException("Attempted to access a monomial variable out of bounds!");
				if (value > (int)MASK)
					throw new ArgumentException("Variables only support degrees of up to 255!");

				int shift = index * BITS_PER_ELEM;

				// Shift the mask "shift" bits to the left, then invert it (11111111 becomes 00000000)
				// Apply the mask, which clears only the data that was aligned with the left-shifted mask
				// Remember that left shifting adds trailing zeroes to the right which then become
				//		ones with the inversion.
				// Erasing this data lets us write to it properly in the next step.
				packedInts &= ~( MASK << shift );

				// apply the mask to the passed in value first. anything left of the right 8 bits
				//		gets erased as a safety net for bad input.
				// then, shift the corrected input to the left so it lines up with the element at the idx.
				// finally, write the 1s from the new to the empty bits in the ulong.
				packedInts |= ( ( (ulong)value & MASK ) << shift );
			}
		}

		public int Count => Flex.NumVars;

		#endregion

		#region Operators

		public static bool operator ==(DegreeList list, ulong other) => list.packedInts == other;
		public static bool operator !=(DegreeList list, ulong other) => list.packedInts != other;

		public static explicit operator DegreeList(ulong bits) => new DegreeList(bits);
		public static explicit operator ulong(DegreeList degList) => degList.packedInts;

		#endregion

		#region IEnumerable

		// custom enum for performance
		public struct Enumerator : IEnumerator<int> {

			private readonly DegreeList data; // wrapped into a DegreeList when needed
			private int idx;

			public Enumerator(DegreeList data) {

				this.data = data;

				idx = -1;

			}

			public bool MoveNext() {

				idx++;

				return idx < Flex.NumVars; // stop point

			}

			public int Current => (int)( data[idx] );

			object System.Collections.IEnumerator.Current => Current;

			public void Reset() => idx = -1;

			public void Dispose() { }

		}

		// this is the one used by foreach loops and it is 0 gc vs the default enumerable
		public Enumerator GetEnumerator() => new Enumerator(this);

		// these r just for the compiler, but it should use the above one
		IEnumerator<int> IEnumerable<int>.GetEnumerator() => GetEnumerator();
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

		#endregion

	}

	// monomial data
	private readonly double coefficient; // scalar multiplier of the monomial
	private readonly DegreeList idpDegrees; // maps flex (as int) to degree

	// persistent aux data
	private readonly int totalDegree; // sum of all degrees of all variables in this mono
	private readonly int independentVariableCount; // number of variables this mono actually contains
	private readonly int variableScore; // used for CompareTo 

	internal MonoEx(double coefficient, DegreeList idpDegrees) {

		this.coefficient = ForcePrecision(coefficient);

		this.idpDegrees = idpDegrees;

		// calculate aux data
		independentVariableCount = 0;
		totalDegree = 0;
		variableScore = 0;

		for (int i = 0; i < idpDegrees.Count; i++) {

			totalDegree += idpDegrees[i];

			if (idpDegrees[i] != 0) {

				independentVariableCount++;
				variableScore += i << i; // ensure each idp has a unique val

			}

		}

		// for constant monos
		if (independentVariableCount == 0)
			variableScore = int.MaxValue;

	}

	internal MonoEx(double coefficient) : this(coefficient, new DegreeList()) { }

	internal MonoEx(double coefficient, Flex independent, int degree) : this(coefficient, DegreeList.MakeDegreeList([independent], degree)) { }

	internal MonoEx(double coefficient, Flex independent) : this(coefficient, independent, 1) { }

	internal MonoEx(Flex independent, int degree) : this(1, independent, degree) { }
	internal MonoEx(Flex independent) : this(1, independent, 1) { }

	internal MonoEx(in MonoEx other) : this(other.coefficient, other.idpDegrees) { }

	internal MonoEx(in MonoEx other, double newCoefficient) : this(newCoefficient, other.idpDegrees) { }

	/// <summary>
	/// Instantiates the monomial 0
	/// </summary>
	public MonoEx() : this(0) { }

	// edit degrees
	internal MonoEx(double coefficient, Flex[] varsToEdit, params int[] newDegrees) : this(coefficient, DegreeList.MakeDegreeList(varsToEdit, newDegrees)) {

		if (varsToEdit.Length != newDegrees.Length)
			throw new ArgumentException("Constructor requires the same number of independent variables and degrees!");

	}

	internal MonoEx Flipped() => new MonoEx(this, -this.coefficient);

	#endregion

	#region Accessors

	///<summary>
	/// returns the degree of a given independent variable in the monomial
	///</summary>
	public readonly int DegreeOfVariable(Flex idpVar) => idpDegrees[idpVar.Id];

	/// <summary>
	/// check if the monomial contains a given independent variable
	/// </summary>
	/// <param name="idpVar">variable to check for</param>
	/// <returns>true if the variable is in the monomial (has a nonzero degree)</returns>
	public readonly bool HasVariable(Flex idpVar) => DegreeOfVariable(idpVar) != 0;

	/// <summary>
	/// returns the count of all independent variables in the monomial
	/// </summary>
	public readonly int IndependentVariableCount => this.independentVariableCount;

	///<summary>
	/// returns the coefficient of the expression
	///</summary>
	public readonly double Coefficient => this.coefficient;

	///<summary>
	/// returns the sum of all degrees of the independent variables in the monomial expression, which is the total degree of the monomial
	///</summary>
	public readonly int TotalDegree => this.totalDegree;

	internal readonly DegreeList DegreeData => this.idpDegrees;

	#endregion

	#region Operators (Public Interface for Construction)

	// see Equality, Ordering, and Hashing for equality operators

	// add monomials to make a polynomial
	/// <summary>
	/// Add monomials, which creates a polynomial of either 1 or 2 terms depending on if the monomials were like terms
	/// </summary>
	/// <returns>Sum of expressions as a new polynomial</returns>
	public static PolyEx operator +(in MonoEx mono1, in MonoEx mono2) => PolyEx.CombineMonomials(mono1, mono2, false);

	// subtract monomials to make a polynomial
	/// <summary>
	/// Subtract monomials, which creates a polynomial of either 1 or 2 terms depending on if the monomials were like terms
	/// </summary>
	/// <returns>Difference of expressions as a new polynomial</returns>
	public static PolyEx operator -(in MonoEx mono1, in MonoEx mono2) => PolyEx.CombineMonomials(mono1, mono2, true);

	// multiply monomials to get a monomial in return 
	/// <summary>
	/// Multiply a monomial with another, combining coefficient and variables
	/// </summary>
	/// <returns>Product of expressions as a new monomial</returns>
	public static MonoEx operator *(in MonoEx mono1, in MonoEx mono2) {

		double coefficientProduct = ForcePrecision(mono1.coefficient * mono2.coefficient);
		DegreeList combinedVars = new DegreeList();

		// if either monomial is zero, skip all this var work
		if (coefficientProduct != 0)
			// sum variable degrees
			for (int i = 0; i < combinedVars.Count; i++)
				combinedVars[i] = mono1.idpDegrees[i] + mono2.idpDegrees[i];

		//todo: constructor makes a new array which is a waste because this func makes a trustable new array anyways
		return new MonoEx(coefficientProduct, combinedVars);

	}

	// multiply monomial by a scalar to get a monomial in return with the same independent variables
	/// <summary>
	/// Multiply a monomial with a scalar
	/// </summary>
	/// <returns>Product of expressions as a new monomial</returns>
	public static MonoEx operator *(in MonoEx mono1, in double scalar) => new MonoEx(ForcePrecision(mono1.Coefficient * scalar), mono1.idpDegrees);
	/// <summary>
	/// Multiply a monomial with a scalar
	/// </summary>
	/// <returns>Product of expressions as a new monomial</returns>
	public static MonoEx operator *(double scalar, in MonoEx mono1) => new MonoEx(ForcePrecision(mono1.Coefficient * scalar), mono1.idpDegrees);
	/// <summary>
	/// Create the negative version of a monomial
	/// </summary>
	/// <returns>This monomial with the opposite sign coefficient as a new monomial</returns>
	public static MonoEx operator -(in MonoEx mono) => mono.Flipped();

	// divide monomial by a scalar to get a monomial in return with the same independent variables
	/// <summary>
	/// Divide monomial by a scalar
	/// </summary>
	/// <returns>Quotient of expressions as a new monomial</returns>
	public static MonoEx operator /(in MonoEx mono1, double scalar) => new MonoEx(ForcePrecision(mono1.Coefficient / scalar), mono1.idpDegrees);

	public static MonoEx operator ^(in MonoEx mono, int pow) {

		if (pow < 0)
			throw new InvalidOperationException("Cannot take a monomial to a negative power!");

		if (pow == 0)
			return 1;

		MonoEx result = 1;
		MonoEx currentProduct = mono;

		while (pow > 0) {

			if (( pow & 1 ) == 1)
				result *= currentProduct;

			currentProduct *= currentProduct;
			pow >>= 1;

		}

		return result;

	}

	/// <summary>
	/// Convert a lone coefficient into a monomial of that coefficient and no variables
	/// </summary>
	/// <param name="num">Number that is secretly a constant monomial</param>
	public static implicit operator MonoEx(double num) => new MonoEx(coefficient: num);
	/// <summary>
	/// Convert a lone variable into a monomial of that variable with degree 1 and coefficient 1
	/// </summary>
	/// <param name="idp">Independent variable</param>
	public static implicit operator MonoEx(Flex idp) => new MonoEx(independent: idp, degree: 1);

	#endregion

	#region Equality, Ordering, and Hashing

	#region Comparison 

	/// <summary>
	/// Monomials with variables come first, then constant terms <br/>
	/// if the monomials are like terms, order them by coefficient <br/>
	/// if the monomials are not like terms, order them by degree then by coefficient <br/>
	/// </summary>
	/// <param name="other"></param>
	/// <returns></returns>
	/// <exception cref="ArgumentException"></exception>
	public readonly int CompareTo(object? other) {

		// this assumes precision has been forced consistently and accurately ... 

		// less than 0: this comes before other
		// 0: this and other are same
		// greater than 0: this comes after other

		// note that variable priority is based on the int values of the Flex variables

		if (other == null)
			return -1;

		if (other is MonoEx otherMono) {

			// if they have different independent variable counts, or same count dif degree,
			// they cannot be alike.
			// we check this first because IsLike is expensive and if either is condition is true, IsLike cannot be true

			// calculate variable comparison
			int variableCompare = this.variableScore.CompareTo(otherMono.variableScore);
			if (variableCompare != 0)
				return variableCompare;

			if (this.IsLike(otherMono))
				return this.Coefficient.CompareTo(otherMono.coefficient);

			int degreeCompare = this.TotalDegree.CompareTo(otherMono.TotalDegree);
			if (degreeCompare != 0)
				return degreeCompare * -1;

			int coefCompare = this.Coefficient.CompareTo(otherMono.coefficient);
			//if (coefCompare != 0)
			return coefCompare * -1;



		}
		else
			throw new ArgumentException("Object is not a MonoEx, cannot compare to other MonoEx!");

	}

	/// <param name="mono1"></param>
	/// <param name="mono2"></param>
	/// <returns>true if mono1 would come before mono2 in sorted order</returns>
	public static bool GreaterOrder(in MonoEx mono1, in MonoEx mono2) => mono1.CompareTo(mono2) < 0;

	/// <param name="other"></param>
	/// <returns>true if this would come before other in sorted order</returns>
	public readonly bool HasGreaterOrderThan(in MonoEx other) => MonoEx.GreaterOrder(this, other);

	#endregion

	#region Equality and Hashing

	/// <returns>true if other is non-null and MonoEx, and monomials have the same variables, degrees, and coefficient</returns>
	public override readonly bool Equals(object? other) {

		if (other == null || other.GetType() != this.GetType())
			return false;

		// the first 

		var otherMono = (MonoEx)other;

		return otherMono == this;

	}

	// needed for IEquatable
	public readonly bool Equals(MonoEx other) => this.Equals((object)other);

	// this has to match the logic in .equals()
	// .equals() delegates to ==
	// ==  uses IsLike and compares coefficients, so hash must as well
	/// <inheritdoc cref="GetHashCode"/>
	/// <summary>
	/// Generates hash based on coefficient, all variables, and their degrees
	/// </summary>
	public override readonly int GetHashCode() {

		HashCode hash = new HashCode();
		hash.Add(ForcePrecision(coefficient));
		hash.Add((ulong)idpDegrees);

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
	/// <returns>true if both monomials have the same variables, degrees, and coefficient</returns>
	public static bool operator ==(in MonoEx mono1, in MonoEx mono2) => mono1.IsLike(mono2)
																	&& ForcePrecision(mono1.Coefficient) == ForcePrecision(mono2.Coefficient);


	/// <returns></returns>
	/// <returns>false if both monomials have the same variables, degrees, and coefficient</returns>
	public static bool operator !=(in MonoEx mono1, in MonoEx mono2) => !( mono1 == mono2 );

	/// <returns>true if the monomial has no variables and a coefficient equal to num (both values are rounded)</returns>
	public static bool operator ==(in MonoEx mono, double num) => mono.independentVariableCount == 0 && ForcePrecision(mono.Coefficient) == ForcePrecision(num);

	/// <returns>false if the monomial has no variables and a coefficient equal to num (both values are rounded)</returns>
	public static bool operator !=(in MonoEx mono, double num) => !( mono == num );

	#endregion

	#endregion

	#region Helpers

	/// <summary>
	/// Check if a monomial is a like term with this monomial
	/// </summary>
	/// <param name="otherMono">Monomial to compare this with</param>
	/// <returns>true if otherMono has same vars with same degrees</returns>
	public readonly bool IsLike(in MonoEx otherMono) => otherMono.idpDegrees == this.idpDegrees;

	#endregion

	#region Stringy 

	internal readonly StringBuilder AbsToStringBuilder() {

		StringBuilder sb = new StringBuilder();

		// if coef is 0, whole thing is 0
		if (coefficient == 0)
			return sb.Append(0);

		double absCoef = double.Abs(coefficient);

		// if the coefficient is one and there are vars, it will be left out 
		sb.Append(( absCoef == 1 && IndependentVariableCount != 0 ) ? "" : absCoef.ToString());

		// all vars with powers and wrapped in parenthesis
		for (int i = 0; i < idpDegrees.Count; i++) {

			int deg = idpDegrees[i];

			if (deg != 0) {

				bool wrapInParenthesis = deg != 1 && this.IndependentVariableCount != 1;

				sb.Append(wrapInParenthesis ? "(" : "")
					.Append(Flex.NumToFlexChar[i])
					.Append(DegreeToString(deg))
					.Append(( wrapInParenthesis ? ")" : "" ));

			}

		}

		return sb;

	}

	/// <summary>
	/// Use ToString for signed version
	/// </summary>
	/// <returns>Unsigned monomial as string</returns>
	public readonly string AbsToString() => AbsToStringBuilder().ToString();

	internal readonly StringBuilder ToStringBuilder() => new StringBuilder(coefficient < 0 ? "-" : "").Append(AbsToString());

	/// <returns>String representation of this monomial</returns>
	override readonly public string ToString() => ToStringBuilder().ToString();

	#endregion

}