using System;
using System.Diagnostics;
using System.Numerics;

namespace Flexpressions
{

	public class PolyEx<NumType> where NumType : INumber<NumType>
	{

		private List<MonomialNode<NumType>> termSeries;

		private PolyEx(List<MonomialNode<NumType>> termSeries) => this.termSeries = termSeries;

		private PolyEx(MonomialNode<NumType> term)
		{

			List<MonomialNode<NumType>> termSeries = new List<MonomialNode<NumType>>();

			termSeries.Add(term);

			this.termSeries = termSeries;

		}

		// perfors mono1 (+/-) mono2 and returns the resulting polynomial expression
		// ex: mono1 - mono2
		// ex: mono1 + mono2
		public static PolyEx<NumType> CombineMonomials(MonoEx<NumType> mono1, MonoEx<NumType> mono2, bool subtract)
		{

			List<MonomialNode<NumType>> termSeries = new List<MonomialNode<NumType>>();

			if (mono1.IsLike(mono2))
			{

				// add first to second or s
				NumType coefficientSum = mono1.Coefficient + (mono2.Coefficient * (subtract ? NumType.CreateChecked(-1) : NumType.One));
				termSeries.Add(new MonomialNode<NumType>(null, new MonoEx<NumType>(mono1, coefficientSum)));
				//return new PolyEx<NumType>(termList());

			}
			else
			{

				var first = (mono1.GreaterOrder(mono2) ? mono1 : mono2);
				var second = (first.Equals(mono1) ? mono2 : mono1);

				char op = (subtract ? '-' : '+');

				termSeries.Add(new MonomialNode<NumType>(null, first));
				termSeries.Add(new MonomialNode<NumType>(op, second));

			}

			return new PolyEx<NumType>(termSeries);

		}

		public override string ToString()
		{

			string ret = "";

			foreach (var MonoNode in termSeries)
				ret += MonoNode;

			return ret;

		}

	}

}


