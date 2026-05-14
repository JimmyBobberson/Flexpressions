using System.Collections.Immutable;
using System.Numerics;
using System.Diagnostics;

namespace Flexpressions;

/// <summary>
/// Flex objects represent independent variables used in Flexpressions objects (monomials, polynomials, functions); <br/>
/// For example, Flex.x is just the variable "x." <br/>
/// Flexpressions can only use the specific variables supported by the Flex type (currently can only use x, y, z). <br/>
/// Use Flex.All to read through every defined variable
/// </summary>
public readonly record struct Flex {

	#region Independent Variable List and Static Data

	// source of truth: maintain this and the static Flex() maintains everything else
	// hardcoded but ig what can you do
	// note: max 8 variables due to monoex using ulong/

	/// <summary> The independent variable "x" </summary>
	public static readonly Flex x = new(0, 'x');
	/// <summary> The independent variable "x" </summary>
	public static readonly Flex y = new(1, 'y');
	/// <summary> The independent variable "x" </summary>
	public static readonly Flex z = new(2, 'z');

	/// <summary>
	/// List of all defined Flex variables
	/// </summary>
	public static readonly Flex[] All = [x, y, z];

	internal static readonly char[] NumToFlexChar;
	internal static readonly int NumVars;

	#endregion'

	#region Construction and Initialization

	static Flex() {
		// runs on app start
		// this is the one ai generated code im using without knowing what it does but it automatically maintains the internal data
		//		as long as my vars are manually maintained
		var fields = typeof(Flex).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
						 .Where(f => f.FieldType == typeof(Flex))
						 .Select(f => f.GetValue(null))
						 .Where(val => val is not null)
						 .Select(val => (Flex)val!)
						 .OrderBy(f => f.Id)
						 .ToList();

		NumToFlexChar = fields.Select(f => f.Symbol).ToArray();
		NumVars = fields.Count;

		Debug.Assert(NumVars == All.Length, $"Critical constant mismatch! Flex.All size ({All.Length}) does not match statically defined NumVars ({NumVars})!");

	}

	private Flex(int id, char symbol) { Id = id; Symbol = symbol; }

	#endregion

	#region Accessors

	internal int Id { get; init; }

	internal char Symbol { get; init; }

	/// <returns>hash code</returns>
	public override int GetHashCode() => Id;

	#endregion

	#region Operators

	/// <summary>
	/// Take a variable to a power and get the resulting monomial
	/// </summary>
	/// <param name="f"></param>
	/// <param name="deg"></param>
	/// <returns></returns>
	public static MonoEx operator ^(Flex f, int deg) => new MonoEx(f, deg);

	/// <summary>
	/// Sum variables to get a polynomial (which will only have one term if the variables are the same)
	/// </summary>
	/// <param name="f1"></param>
	/// <param name="f2"></param>
	/// <returns></returns>
	public static PolyEx operator +(Flex f1, Flex f2) => new MonoEx(f1) + new MonoEx(f2);

	/// <summary>
	/// Multiply a variable by a coefficient to get a monomial
	/// </summary>
	/// <param name="num"></param>
	/// <param name="f"></param>
	/// <returns></returns>
	public static PolyEx operator *(double num, Flex f) => new MonoEx(num, f, 1);
	/// <summary>
	/// Multiply a variable by a coefficient to get a monomial
	/// </summary>
	/// <param name="num"></param>
	/// <param name="f"></param>
	/// <returns></returns>
	public static PolyEx operator *(Flex f, double num) => num * f;

	#endregion

}


