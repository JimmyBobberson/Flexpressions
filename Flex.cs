using System.Numerics;

namespace Flexpressions;

public readonly record struct Flex {

	// source of truth: maintain this and the static Flex() maintains everything else
	// hardcoded but ig what can you do
	// note: max 8 variables due to monoex using ulong
	public static readonly Flex x = new(0, 'x');
	public static readonly Flex y = new(1, 'y');
	public static readonly Flex z = new(2, 'z');
	internal static readonly Flex[] NUM_TO_FLEX = [x, y, z];

	internal static readonly char[] NUM_TO_FLEX_CHAR;
	internal static readonly int NUM_VARS;

	static Flex() {
		// runs on app start
		// this is the one ai generated code im using without knowing what it does but it automatically maintains the internal data
		//		as long as my vars are manually maintained
		var fields = typeof(Flex).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
								 .Where(f => f.FieldType == typeof(Flex))
								 .Select(f => (Flex)f.GetValue(null))
								 .OrderBy(f => f.Id)
								 .ToList();

		NUM_TO_FLEX_CHAR = fields.Select(f => f.Symbol).ToArray();
		NUM_VARS = fields.Count;
	}

	internal int Id { get; init; }
	internal char Symbol { get; init; }

	private Flex(int id, char symbol) { Id = id; Symbol = symbol; }


	public static MonoEx operator ^(Flex f, int deg) => new MonoEx(f, deg);

	public static PolyEx operator +(Flex f1, Flex f2) => new MonoEx(f1) + new MonoEx(f2);

	public static PolyEx operator *(double num, Flex f) => new MonoEx(num, f, 1);
	public static PolyEx operator *(Flex f, double num) => num * f;



	public override int GetHashCode() => Id;

}


