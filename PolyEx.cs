using System;
using System.Diagnostics;
using System.Numerics;

namespace Flexpressions;

public record struct MonomialNode<NumType>(char? symbol, MonoEx<NumType> mono) where NumType : INumber<NumType> {

	override public string ToString() => ( ( symbol != null ) ? symbol + " " : "" )
											+ mono + " ";
}

public class PolyEx<NumType> where NumType : INumber<NumType> {

	private static readonly char MINUS = '-';
	private static readonly char PLUS = '+';

	private List<MonomialNode<NumType>> termSeries;

	private PolyEx(List<MonomialNode<NumType>> termSeries) => this.termSeries = termSeries;

	private PolyEx(MonomialNode<NumType> term) {

		List<MonomialNode<NumType>> termSeries = new List<MonomialNode<NumType>>();

		termSeries.Add(term);

		this.termSeries = termSeries;

	}

	// performs mono1 (+/-) mono2 and returns the resulting polynomial expression
	public static PolyEx<NumType> CombineMonomials(MonoEx<NumType> mono1, MonoEx<NumType> mono2, bool subtract) {

		List<MonomialNode<NumType>> termSeries = new List<MonomialNode<NumType>>();

		// if the monomials are like terms, just sum their coefficients
		// resulting polynomial will only have 1 term, but its best if we always return a polynomial when we do +/- 
		if (mono1.IsLike(mono2)) {

			// add first to second
			NumType coefficientSum = mono1.Coefficient + ( mono2.Coefficient * ( subtract ? NumType.CreateChecked(-1) : NumType.One ) );

			termSeries.Add(new MonomialNode<NumType>(null, new MonoEx<NumType>(mono1, coefficientSum)));

		}
		else {

			// figure out which one comes first in the order
			var first = ( mono1.GreaterOrder(mono2) ? mono1 : mono2 );
			var second = ( first.Equals(mono1) ? mono2 : mono1 );

			// determine the operator char
			char op = ( subtract ? MINUS : PLUS );

			// subtracting a negative is addition,  simplify expression
			if (op == MINUS && second.Coefficient < NumType.Zero) {

				// remove the negative sign from the term
				second = new MonoEx<NumType>(second, second.Coefficient * NumType.CreateChecked(-1));
				// swap the operator
				op = PLUS;

			}

			// adding a negative is subtraction
			else if (op == PLUS && second.Coefficient < NumType.Zero) {

				// remove the negative sign from the term
				second = new MonoEx<NumType>(second, second.Coefficient * NumType.CreateChecked(-1));
				// swap the operator
				op = MINUS;

			}

			// add to mono list in order
			termSeries.Add(new MonomialNode<NumType>(null, first));
			termSeries.Add(new MonomialNode<NumType>(op, second));

		}

		return new PolyEx<NumType>(termSeries);

	}

	public override string ToString() {

		string ret = "";

		foreach (var MonoNode in termSeries)
			ret += MonoNode;

		return ret;

	}

}




