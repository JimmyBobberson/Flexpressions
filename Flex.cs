using System.Numerics;

namespace Flexpressions;

public readonly record struct Flex<NumType> where NumType : INumber<NumType> {

	// the worst thing ive ever had to write
	// rules for self:
	// FlexVar elements are only created with the static thingies
	// order of elements in VarToChar must match index order of FlexVar
	// monomial internal arrays rely on the values of FlexVars as indices
	// NUM_VARS has to be manually maintained for accuracy

	//	x being 0 means the 0th element in any monoex's internal array is its degree and its char representation should be VAR_TO_CHAR[0]

	#region Data and Construction

	internal const int NUM_VARS = 3;

	internal static readonly char[] VAR_TO_CHAR = new char[] { 'x', 'y', 'z' };
	internal static readonly Flex<NumType>[] ID_TO_VAR = new Flex<NumType>[] { new(0), new(1), new(2) };

	// Private constructor prevents external code from doing 'new FlexVar(5)'
	internal Flex(int id) => Id = id;

	public static readonly Flex<NumType> x = new(0);
	public static readonly Flex<NumType> y = new(1);
	public static readonly Flex<NumType> z = new(2);
	/*public static readonly Flex<NumType> a = new(3);
	public static readonly Flex<NumType> b = new(4);
	public static readonly Flex<NumType> c = new(5);*/

	#endregion

	#region Accessors

	internal int Id { get; }

	internal char AsChar() => VAR_TO_CHAR[this.Id];

	public override int GetHashCode() => Id;

	#endregion

	#region Operators

	public static MonoEx<NumType> operator ^(Flex<NumType> flex, NumType deg) => new MonoEx<NumType>(flex, deg);
	public static PolyEx<NumType> operator +(Flex<NumType> flex1, Flex<NumType> flex2) {

		PolyEx<NumType> ret = new MonoEx<NumType>(flex1) + new MonoEx<NumType>(flex2);

		return ret;

	}

	#endregion

}

/*
namespace Flexpressions;

public readonly record struct Flex
{
	// 1. Define your variables in ONE place
	public static readonly Flex x = new(0, 'x');
	public static readonly Flex y = new(1, 'y');
	public static readonly Flex z = new(2, 'z');

	// 2. The engine handles the arrays automatically
	internal static readonly char[] VarToChar;
	internal static readonly int NumVars;

	static Flex() 
	{
		// This runs once when the app starts
		var fields = typeof(Flex).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
								 .Where(f => f.FieldType == typeof(Flex))
								 .Select(f => (Flex)f.GetValue(null))
								 .OrderBy(f => f.Id)
								 .ToList();

		VarToChar = fields.Select(f => f.Symbol).ToArray();
		NumVars = fields.Count;
	}

	internal int Id { get; init; }
	internal char Symbol { get; init; }

	private Flex(int id, char symbol) { Id = id; Symbol = symbol; }

	// 3. The "Cute" Operators (Now using double/float)
	public static MonoEx operator ^(Flex f, double deg) => new MonoEx(f, (float)deg);
	
	public static PolyEx operator +(Flex f1, Flex f2) => 
		new PolyEx(new MonoEx(f1), new MonoEx(f2));

	public override int GetHashCode() => Id;
}
 
*/

