using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Numerics;

namespace Flexpressions;

public class Function<NumType> where NumType : INumber<NumType> {

	private const string DEFAULT_NAME = "f";

	private PolyEx<NumType> poly;
	private SortedSet<int> vars;
	string functionName;
	string? cachedFunctionString; // todo: use memoization/caching in other classes

	public Function(PolyEx<NumType> poly, string functionName) {

		cachedFunctionString = null;
		this.functionName = functionName;

		vars = new SortedSet<int>();

		UpdatePoly(poly);


	}

	public Function(PolyEx<NumType> poly) : this(poly: poly, functionName: DEFAULT_NAME) { }

	public Function(MonoEx<NumType> mono, string functionName) : this(poly: mono, functionName: functionName) { }

	public Function(MonoEx<NumType> mono) : this(poly: mono, functionName: DEFAULT_NAME) { }

	public NumType this[params NumType[] inputs] {

		get {

			// this is a rough check and does not account for the fact that a function of y and z has the same dimension of x and y and x and z
			// (this will be caught later
			if (inputs.Length != vars.Count)
				throw new ArgumentException("Invalid Input: Inputted " + inputs.Length
										+ " values into function which has " + vars.Count + " vars");

			NumType result = NumType.Zero;

			// iterate through each term
			foreach (MonoEx<NumType> mono in poly) {

				// if 0, skip
				if (mono.Coefficient == NumType.Zero)
					continue;

				// product of coef and variables
				NumType evaluatedTerm = mono.Coefficient;

				// check every var
				// essentially: say the first element of the input is 2 and the "first" element in vars is 0
				// we then know to input 2 into the variable associated with 0 (x)
				// if the second element of input is 1 and the "second" element in vars is 2
				// we then know to input 1 into the variable associated with 2 (z)
				int i = 0;
				foreach (int var in vars) {

					// input for variable
					double input = double.CreateChecked(inputs[i]);
					// degree of varbiable
					double deg = double.CreateChecked(mono.DegreeOfVariable(new Flex<NumType>(var)));

					evaluatedTerm *= NumType.CreateChecked(double.Pow(input, deg));

					i++;

				}

				result += evaluatedTerm;

			}

			return result;

		}

	}

	public string ExpressOutput(params NumType[] inputs) {

		string ret = functionName + "(";

		int counter = vars.Count;

		foreach (NumType input in inputs) {

			ret += input + ( ( counter != 1 ) ? ", " : "" );
			counter--;

		}

		ret += ") = " + this[inputs];

		return ret;

	}

	private void UpdatePoly(PolyEx<NumType> poly) {

		this.poly = poly;

		cachedFunctionString = null;

		vars.Clear();

		foreach (var mono in poly)
			for (int i = 0; i < Flex<NumType>.NUM_VARS; i++)
				if (mono.DegreeOfVariable(Flex<NumType>.NUM_TO_FLEX[i]) != NumType.Zero)
					vars.Add(i);

	}

	public PolyEx<NumType> Expression {

		get => this.poly;

		set => UpdatePoly(value);

	}

	public string FunctionString {

		get {

			if (cachedFunctionString != null)
				return cachedFunctionString;

			string ret = functionName + "(";

			int counter = vars.Count;

			foreach (int idpVar in vars) {

				ret += Flex<NumType>.VAR_TO_CHAR[idpVar] + ( ( counter != 1 ) ? ", " : "" );
				counter--;

			}

			ret += ")";

			cachedFunctionString = ret;

			return cachedFunctionString;

		}

	}


	public override string ToString() => this.FunctionString + " = " + poly;

}




