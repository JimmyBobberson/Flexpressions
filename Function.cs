namespace Flexpressions;

/// <summary>
/// <b>Functions enable the evaluation of polynomials at specific points as well as special operations like calculus.</b>  <para/>
/// Use [] for evaluation, and pass inputs in the following order: <i>x, y, z, t</i> (same as Flex.All). Omit variables not in your function. <para/>
/// If the function's polynomial contains no variables, it will
/// fall back to the last variables the function recognized, or the default variable "x".  <para/>
/// Functions have a string name (e.g., <i>f</i>) for when they are printed.
/// </summary>
public class Function {

	#region Static Helper thingies

	private const string DEFAULT_NAME = "f";
	private static readonly Flex DEFAULT_VAR = Flex.x;
	private const char PRIME = '\'';

	/// <summary> The function f(x) = 0 </summary>
	public static readonly Function Zero = new Function(0, DEFAULT_NAME);
	/// <summary> The function f(x) = num </summary>
	public static Function ConstantFunctionFor(double num) => new Function(num, DEFAULT_NAME);

	/// <summary> The function f(x) = x </summary>
	public static readonly Function Linear = new Function(DEFAULT_VAR, DEFAULT_NAME);
	/// <summary> The function f(x) = x^2 </summary>
	public static readonly Function Quadratic = new Function(DEFAULT_VAR ^ 2, DEFAULT_NAME);
	/// <summary> The function f(x) = x^3 </summary>
	public static readonly Function Cubic = new Function(DEFAULT_VAR ^ 3, DEFAULT_NAME);

	/// <summary> The function f(x,y) = x + y </summary>
	public static readonly Function Linear2D = new Function(Flex.x + Flex.y, DEFAULT_NAME);
	/// <summary> The function f(x,y,z) = x + y + z </summary>
	public static readonly Function Linear3D = new Function(Flex.x + Flex.y + Flex.z, DEFAULT_NAME);

	#endregion

	#region State and Constructors

	// The polynomial this function uses
	private PolyEx poly;
	// The set of vars present in the polynomial
	private SortedSet<int> vars;
	// The name of the function used in printing, e.g. f 
	private string functionName;
	// Used to optimize repeated ToString calls which are expensive here
	private string? cachedFunctionString; // todo: use memoization/caching in other classes

	/// <summary>
	/// Construct a function using a polynomial and a defined name
	/// </summary>
	/// <param name="poly">Polynomial this function will use</param>
	/// <param name="functionName">Function name used for printing</param>
	public Function(PolyEx poly, string functionName = DEFAULT_NAME) {

		this.cachedFunctionString = null;
		this.functionName = functionName;

		vars = new SortedSet<int>();

		// poly is actually set in within the function. the explicit assignment is just to remove a compiler warning
		this.poly = poly;
		UpdatePoly(poly);

		if (vars.Count == 0)
			vars.Add(DEFAULT_VAR.Id);

	}

	/// <summary>
	/// Construct a function using a monomial (single-term polynomial) and a defined name
	/// </summary>
	/// <param name="mono">[Poly]nomial this function will use</param>
	/// <param name="functionName">Function name used for printing</param>
	public Function(MonoEx mono, string functionName = DEFAULT_NAME) : this(poly: mono, functionName: functionName) { }

	/// <summary>
	/// Copy a function
	/// </summary>
	/// <param name="other">Function to take expression and name from</param>
	public Function(Function other) : this(new PolyEx(other.poly), other.functionName) { }

	#endregion

	#region Evaluation

	/// <summary>
	/// Evaluate this function at a point.<br/>
	/// For example, if your function is dependent on x and y, this[1, 2] will evaluate the function at the point where x = 1 and y = 2 <br/>
	/// <i>Inputs must be ordered as follows: [x, y, z]. Omit variables not present in this function. </i>
	/// </summary>
	/// <param name="inputs">Inputs to function following fixed ordering rules</param>
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

					evaluatedTerm *= DoublePow(inputs[i], mono.DegreeOfVariable(Flex.All[var]));

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
	/// <param name="inputs">Inputs to function following fixed ordering rules</param>
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
	/// This function's cosmetic identity (used in string representations of the function)
	/// </summary>
	public string Name {

		get => functionName;

		set {

			functionName = value;

			cachedFunctionString = null;

		}

	}

	/// <summary>
	/// The list of variables present in this function, in proper sorted order
	/// </summary>
	public Flex[] Variables {

		get {

			Flex[] flexArray = new Flex[vars.Count];

			var varIt = vars.GetEnumerator();

			for (int i = 0; i < vars.Count; i++) {

				flexArray[i] = Flex.All[varIt.Current];

				varIt.MoveNext();

			}

			return flexArray;

		}

	}

	/// <summary>
	/// Check if the function is dependent on a certain independent variable
	/// </summary>
	/// <param name="variable">Variable to check for</param>
	/// <returns>true if variable is within this function's polynomial</returns>
	public bool ContainsVariable(Flex variable) => vars.Contains(variable.Id);

	#endregion

	#region Helpers

	// bool should be false in the constructor's call to UpdatePoly, so the constructor can handle the caching itself
	private void UpdatePoly(PolyEx poly) {

		if (vars == null)
			vars = new SortedSet<int>();

		SortedSet<int> cachedVars = new SortedSet<int>(vars);

		vars.Clear();

		//poly.Sort();
		// sort only needed for display

		this.poly = poly;

		cachedFunctionString = null;

		foreach (var mono in this.poly.AsSpan())
			for (int i = 0; i < Flex.NumVars; i++)
				if (mono.DegreeOfVariable(Flex.All[i]) != 0)
					vars.Add(i);

		if (vars.Count == 0)
			vars = cachedVars;

	}

	// ai generated
	private static double DoublePow(double x, int n) {
		// Handle negative exponents without stack overflow
		// note from not an ai: n will never be less than 0 
		if (n < 0) {
			if (x == 0.0)
				throw new DivideByZeroException();

			// Handle int.MinValue overflow safely
			if (n == int.MinValue)
				return 1.0 / ( x * DoublePow(x, int.MaxValue) );

			return 1.0 / DoublePow(x, -n);
		}

		double result = 1.0;
		double currentProduct = x;

		while (n > 0) {

			if (( n & 1 ) == 1)
				result *= currentProduct;

			currentProduct *= currentProduct;
			n >>= 1;

		}

		return result;
	}

	#endregion

	#region Operators 

	/// <summary> Create a function using the given polynomial </summary>
	/// <param name="poly">Polynomial to turn into function</param>
	public static implicit operator Function(PolyEx poly) => new Function(poly);

	/// <summary> Create a function using the given monomial</summary>
	/// <param name="mono">Monomial to turn into function</param>
	public static implicit operator Function(MonoEx mono) => new Function(mono);

	/// <summary> Create a function using the given double constant</summary>
	/// <param name="num">Constant monomial to turn into function</param>
	public static implicit operator Function(double num) => new Function(num);

	/// <summary> Create a function using the given independent variable</summary>
	/// <param name="idp">Variable to turn into function</param>
	public static implicit operator Function(Flex idp) => new Function(idp);

	#endregion

	#region Behaviors (Calculus)

	/// <summary>
	/// 
	/// </summary>
	/// <param name="poly"></param>
	/// <param name="changeInVariable"></param>
	/// <returns></returns>
	private static PolyEx DifferentiatePoly(PolyEx poly, Flex changeInVariable) {

		PolyEx derivative = new PolyEx();

		foreach (ref readonly var mono in poly.AsSpan()) {

			int degree = mono.DegreeOfVariable(changeInVariable);

			if (degree != 0) {

				MonoEx.DegreeList newDegrees = mono.DegreeData;
				newDegrees[changeInVariable.Id] = degree - 1;

				derivative.Add(new MonoEx(mono.Coefficient * degree, newDegrees));

			}

		}

		return derivative;

	}

	/// <summary>
	/// Find the derivative of this function of a specific order with respect to a variable. (mutates this function) <br/>
	/// If this function has one variable, the result is the derivative, but if there are multiple variables, the
	/// result is the partial derivative.<br/>
	/// </summary>
	/// <param name="changeInVariable">variable to differentiate with respect towards</param>
	/// <param name="order">degree of derivative (first, second, etc)</param>
	/// <returns>this (as the order-th derivative of itself prior to the execution of the method)</returns>
	public Function Differentiate(Flex changeInVariable, int order = 1) {

		if (order <= 0)
			return this;

		if (NumVariables == 0)
			poly = 0;
		else
			for (int i = 0; i < order; i++) {

				poly = Function.DifferentiatePoly(poly, changeInVariable);
				this.Name = functionName + PRIME;

			}

		return this;

	}

	/// <summary>
	/// Find the derivative of this function of a specific order with respect to 
	/// the first variable found in the function by sorted order (mutates this function)<br/> 
	/// <i>(useful for single-var functions)</i> <br/>
	/// If this function has one variable, the result is the derivative, but if there are multiple variables, the
	/// result is the partial derivative.<br/>
	/// </summary>
	/// <param name="order">degree of derivative (first, second, etc)</param>
	/// <returns>this (as the order-th derivative of itself prior to the execution of the method)</returns>
	public Function Differentiate(int order = 1) => NumVariables != 0 ? Differentiate(Flex.All[this.vars.First()], order)
																		: Differentiate(DEFAULT_VAR, order);

	/// <summary>
	/// Find the derivative of this function of a specific order with respect to a variable. <br/>
	/// If this function has one variable, the result is the derivative, but if there are multiple variables, the
	/// result is the partial derivative.<br/>
	/// </summary>
	/// <param name="changeInVariable">variable to differentiate with respect towards</param>
	/// <param name="order">degree of derivative (first, second, etc)</param>
	/// <returns>The order-th derivative of the function with respect to the change in variable, or this if order is 0 or less</returns>
	public Function Derivative(Flex changeInVariable, int order = 1) {

		if (order <= 0)
			return this;

		if (NumVariables == 0)
			return new Function(0);

		PolyEx derivative = new PolyEx(this.poly);

		string newFunctionName = this.functionName;

		for (int i = 0; i < order; i++) {

			derivative = Function.DifferentiatePoly(derivative, changeInVariable);
			newFunctionName += PRIME;

		}

		return new Function(derivative, newFunctionName);

	}

	/// <summary>
	/// Find the derivative of this function of a specific order with respect to 
	/// the first variable found in the function by sorted order <br/> 
	/// <i>(useful for single-var functions)</i> <br/>
	/// If this function has one variable, the result is the derivative, but if there are multiple variables, the
	/// result is the partial derivative.<br/>
	/// </summary>
	/// <param name="order">degree of derivative (first, second, etc)</param>
	/// <returns>The order-th derivative of the function with respect to the change in variable, or this if order is 0 or less</returns>
	public Function Derivative(int order = 1) => NumVariables != 0 ? Derivative(Flex.All[this.vars.First()], order)
																		: Derivative(DEFAULT_VAR, order);

	/// <summary>
	/// Find the gradient of a function, aka an array of its partial derivatives with respect to each variable. <br/>
	/// </summary>
	/// <returns>array of derivatives, each a derivative of this function with respect to a variable it containst</returns>
	public Function[] Gradient() {

		Function[] gradient = new Function[vars.Count];

		int i = 0;
		foreach (int idp in vars) {

			gradient[i] = this.Derivative(Flex.All[idp]);

			i++;

		}

		return gradient;

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