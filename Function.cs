using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Numerics;
using System.Text;

namespace Flexpressions;

/// <summary>
/// Functions allow the evaluation of polynomials at specific points
/// </summary>
public class Function {

	#region Static Helper thingies

	private const string DEFAULT_NAME = "f";

	#endregion

	#region State and Constructors

	// The polynomial this function uses
	private PolyEx poly;
	// The set of vars present in the polynomial
	private SortedSet<int> vars;
	// The name of the function used in printing, e.g. f 
	string functionName;
	// Used to optimize repeated ToString calls which are expensive here
	string? cachedFunctionString; // todo: use memoization/caching in other classes

	/// <summary>
	/// Construct a function using a polynomial and a defined name
	/// </summary>
	/// <param name="poly"></param>
	/// <param name="functionName"></param>
	public Function(PolyEx poly, string functionName) {

		this.poly = poly;
		cachedFunctionString = null;
		this.functionName = functionName;

		vars = new SortedSet<int>();

		UpdatePoly(poly);

	}

	/// <summary>
	/// Construct a function <i>f</i> using a polynomial
	/// </summary>
	/// <param name="poly"></param>
	public Function(PolyEx poly) : this(poly: poly, functionName: DEFAULT_NAME) { }

	/// <summary>
	/// Construct a function using a monomial (single-term polynomial) and a defined name
	/// </summary>
	/// <param name="mono"></param>
	/// <param name="functionName"></param>
	public Function(MonoEx mono, string functionName) : this(poly: mono, functionName: functionName) { }

	/// <summary>
	/// Construct a function <i>f</i> using a monomial (single-term polynomial)
	/// </summary>
	/// <param name="mono"></param>
	public Function(MonoEx mono) : this(poly: mono, functionName: DEFAULT_NAME) { }

	#endregion

	#region Evaluation

	/// <summary>
	/// Evaluate this function at a point.<br/>
	/// For example, if your function is dependent on x and y, this[1, 2] will evaluate the function at the point where x = 1 and y = 2 <br/>
	/// <i>Inputs must be ordered as follows: [x, y, z]. Omit variables not present in this function. </i>
	/// </summary>
	/// <param name="inputs"></param>
	/// <returns>string representation of the function value at a point as an equation</returns>
	public double this[params double[] inputs] {

		get {

			// this is a rough check and does not account for the fact that a function of y and z has the same dimension of x and y and x and z
			// (this will be caught later
			if (inputs.Length != vars.Count)
				throw new ArgumentException("Invalid Input: Inputted " + inputs.Length
										+ " values into function which has " + vars.Count + " vars");

			double result = 0;

			// iterate through each term
			foreach (var mono in poly.AsSpan()) {

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
					double deg = double.CreateChecked(mono.DegreeOfVariable(Flex.All[var]));

					evaluatedTerm *= double.CreateChecked(double.Pow(input, deg));

					i++;

				}

				result += evaluatedTerm;

			}

			return result;

		}

	}

	/// <summary>
	/// Evaluate the function and produce the result as an equation <br/>(e.g., "f(2) = 4")
	/// </summary>
	/// <param name="inputs"></param>
	/// <returns>string representation of the function value at a point as an equation</returns>
	public string ExpressOutput(params double[] inputs) {

		StringBuilder sb = new StringBuilder(functionName).Append("(");

		int counter = vars.Count;

		foreach (double input in inputs) {

			sb.Append(input).Append(( ( counter != 1 ) ? ", " : "" ));
			counter--;

		}

		sb.Append(") = ").Append(this[inputs]);

		return sb.ToString();

	}

	#endregion

	#region Accessors

	/// <summary>
	/// Read or modify to the polynomial this function is associated with
	/// </summary>
	public PolyEx Expression {

		get => this.poly;

		set => UpdatePoly(value);

	}

	/// <summary>
	/// Read the number of independent variables in this function
	/// </summary>
	public int NumVariables => vars.Count();

	/// <summary>
	/// Check if the function is dependent on a certain independent variable
	/// </summary>
	/// <param name="variable"></param>
	/// <returns>true if variable is within this function's polynomial</returns>
	public bool ContainsVariable(Flex variable) => vars.Contains(variable.Id);

	#endregion

	#region Helpers

	private void UpdatePoly(PolyEx poly) {

		poly.Sort();

		this.poly = poly;

		cachedFunctionString = null;

		vars.Clear();

		foreach (var mono in poly)
			for (int i = 0; i < Flex.NumVars; i++)
				if (mono.DegreeOfVariable(Flex.All[i]) != 0)
					vars.Add(i);

	}

	#endregion

	#region Stringy wingy

	/// <returns>string representation of this function as an equality with its polynomial</returns>
	public override string ToString() => new StringBuilder(this.Signature).Append(" = ").Append(poly.ToStringBuilder()).ToString();

	/// <summary>
	/// Return the function and its args represented as a string without the polynomial <br/>
	/// (e.g., "f(x, y)") <br/>
	/// Use ToString to express the function as an equality alongside the polynomial <br/>
	/// (e.g., "f(x, y) = xy")
	/// </summary>
	public string Signature {

		get {

			if (cachedFunctionString != null)
				return cachedFunctionString;

			StringBuilder sb = new StringBuilder(functionName).Append("(");

			int counter = vars.Count;

			foreach (int idpVar in vars) {

				sb.Append(Flex.NumToFlexChar[idpVar]).Append(( ( counter != 1 ) ? ", " : "" ));
				counter--;

			}

			sb.Append(")");

			cachedFunctionString = sb.ToString();

			return cachedFunctionString;

		}

	}

	#endregion

}




