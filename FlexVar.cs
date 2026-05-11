namespace Flexpressions;


public readonly record struct FlexVar {

	// the worst thing ive ever had to write
	// rules for self:
	// FlexVar elements are only created with the static thingies
	// order of elements in VarToChar must match index order of FlexVar
	// monomial internal arrays rely on the values of FlexVars as indices
	// NUM_VARS has to be manually maintained for accuracy

	//	x being 0 means the 0th element in any monoex's internal array is its degree and its char representation should be VAR_TO_CHAR[0]

	internal const int NUM_VARS = 6;

	internal static readonly char[] VAR_TO_CHAR = new char[] { 'x', 'y', 'z', 'a', 'b', 'c' };

	public static readonly FlexVar x = new(0);
	public static readonly FlexVar y = new(1);
	public static readonly FlexVar z = new(2);
	public static readonly FlexVar a = new(3);
	public static readonly FlexVar b = new(4);
	public static readonly FlexVar c = new(5);

	internal int Id { get; }
	internal char AsChar() => VAR_TO_CHAR[this.Id];

	// Private constructor prevents external code from doing 'new FlexVar(5)'
	// unless you want to allow it.
	private FlexVar(int id) => Id = id;

	// Optional: This allows the dictionary lookups inside your method
	public override int GetHashCode() => Id;

}




