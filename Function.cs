using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Numerics;

namespace Flexpressions;

public class Function {

	private const string DEFAULT_NAME = "f";

	private PolyEx poly;
	private SortedSet<int> vars;
	string functionName;
	string? cachedFunctionString; // todo: use memoization/caching in other classes

	public Function(PolyEx poly, string functionName) {

		this.poly = poly;
		cachedFunctionString = null;
		this.functionName = functionName;

		vars = new SortedSet<int>();

		foreach (var mono in poly)
			for (int i = 0; i < Flex.NUM_VARS; i++)
				if (mono.DegreeOfVariable(new Flex(i)) != 0)
					vars.Add(i);


	}

	public Function(PolyEx poly) : this(poly: poly, functionName: DEFAULT_NAME) { }

	public Function(MonoEx mono, string functionName) : this(poly: mono, functionName: functionName) { }

	public Function(MonoEx mono) : this(poly: mono, functionName: DEFAULT_NAME) { }

	public double this[params double[] inputs] {

		get {

			// this is a rough check and does not account for the fact that a function of y and z has the same dimension of x and y and x and z
			// (this will be caught later
			if (inputs.Length != vars.Count)
				throw new ArgumentException("Invalid Input: Inputted " + inputs.Length
										+ " values into function which has " + vars.Count + " vars");

			double result = 0;

			// iterate through each term
			foreach (var mono in poly) {

				// if 0, skip
				if (mono.Coefficient == 0)
					continue;

				// product of coef and variables
				double evaluatedTerm = mono.Coefficient;

				// check every var
				// essentially: say the first element of the input is 2 and the "first" element in vars is 0
				// we then know to input 2 into the variable associated with 0 (x)
				// if the second element of input is 1 and the "second" element in vars is 2
				// we then know to input 1 into the variable associated with 2 (z)
				int i = 0;
				foreach (int var in vars) {

					// todo: replace with optimized power calculation leveraging int as deg

					// input for variable
					double input = inputs[i];
					// degree of varbiable
					double deg = double.CreateChecked(mono.DegreeOfVariable(new Flex(var)));

					evaluatedTerm *= double.CreateChecked(double.Pow(input, deg));

					i++;

				}

				result += evaluatedTerm;

			}

			return result;

		}

	}

	public string ExpressOutput(params double[] inputs) {

		string ret = functionName + "(";

		int counter = vars.Count;

		foreach (double input in inputs) {

			ret += input + ( ( counter != 1 ) ? ", " : "" );
			counter--;

		}

		ret += ") = " + this[inputs];

		return ret;

	}

	public PolyEx Expression {

		get => this.Expression;

		set => this.Expression = value;

	}

	public string FunctionString {

		get {

			if (cachedFunctionString != null)
				return cachedFunctionString;

			string ret = functionName + "(";

			int counter = vars.Count;

			foreach (int idpVar in vars) {

				ret += Flex.VAR_TO_CHAR[idpVar] + ( ( counter != 1 ) ? ", " : "" );
				counter--;

			}

			ret += ")";

			cachedFunctionString = ret;

			return cachedFunctionString;

		}

	}


	public override string ToString() => this.FunctionString + " = " + poly;

}




