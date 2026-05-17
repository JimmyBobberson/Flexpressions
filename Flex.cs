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
public readonly record struct Flex<NumType> where NumType : INumber<NumType> {

	#region Independent Variable List and Static Data

	// source of truth: maintain this and the static Flex() maintains everything else
	// hardcoded but ig what can you do
	// note: max 8 variables due to monoex using ulong/

	/// <summary> The independent variable "x" </summary>
	public static readonly Flex<NumType> x = new(0, 'x');
	/// <summary> The independent variable "x" </summary>
	public static readonly Flex<NumType> y = new(1, 'y');
	/// <summary> The independent variable "x" </summary>
	public static readonly Flex<NumType> z = new(2, 'z');
	/// <summary> The independent variable "t" </summary>
	public static readonly Flex<NumType> t = new(3, 't');

	/// <summary>
	/// List of all defined Flex variables
	/// </summary>
	public static readonly Flex<NumType>[] All = [x, y, z, t];

	internal static readonly char[] NumToFlexChar;
	internal static readonly int NumVars;

	#endregion

	#region Construction and Initialization

	static Flex() {
		// runs on app start
		// this is the one ai generated code im using without knowing what it does but it automatically maintains the internal data
		//		as long as my vars are manually maintained
		var fields = typeof(Flex<NumType>).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
						 .Where(f => f.FieldType == typeof(Flex<NumType>))
						 .Select(f => f.GetValue(null))
						 .Where(val => val is not null)
						 .Select(val => (Flex<NumType>)val!)
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

	public static MonoEx<NumType> operator ^(Flex<NumType> flex, NumType deg) => new MonoEx<NumType>(flex, deg);
	public static PolyEx<NumType> operator +(Flex<NumType> flex1, Flex<NumType> flex2) {

		PolyEx<NumType> ret = new MonoEx<NumType>(flex1) + new MonoEx<NumType>(flex2);

		return ret;

	}

	public static PolyEx<NumType> operator +(Flex<NumType> f1, NumType num) => new MonoEx<NumType>(f1) + new MonoEx<NumType>(num);
	public static PolyEx<NumType> operator +(NumType num, Flex<NumType> f1) => f1 + num;

	public static PolyEx<NumType> operator -(Flex<NumType> f1, NumType num) => new MonoEx<NumType>(f1) - new MonoEx<NumType>(num);
	public static PolyEx<NumType> operator -(NumType num, Flex<NumType> f1) => new MonoEx<NumType>(num) - new MonoEx<NumType>(f1);

	public static MonoEx<NumType> operator -(Flex<NumType> f) => new MonoEx<NumType>(NumType.CreateChecked(-1), f, NumType.CreateChecked(1));

	/// <summary>
	/// Multiply a variable by a coefficient to get a monomial
	/// </summary>
	/// <param name="num"></param>
	/// <param name="f"></param>
	/// <returns></returns>
	public static PolyEx<NumType> operator *(NumType num, Flex<NumType> f) => new MonoEx<NumType>(num, f, NumType.CreateChecked(1));
	/// <summary>
	/// Multiply a variable by a coefficient to get a monomial
	/// </summary>
	/// <param name="num"></param>
	/// <param name="f"></param>
	/// <returns></returns>
	public static PolyEx<NumType> operator *(Flex<NumType> f, NumType num) => num * f;

	#endregion

}




