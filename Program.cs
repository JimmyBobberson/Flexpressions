using System;
using System.Diagnostics;

namespace Flexpressions;

// todo: implement "complex functions" which can have trig, log, exponent, rational, etc (but do not have derivative)
// todo: advanced functions with composition and chaining
class Program {

	static void Main(string[] args) {

		// ai generated cuz im a bum who doesnt wanna write all this 
		Console.WriteLine("=== Flexpressions Core Test Suite ===\n");

		// 1. Basic Whiteboard & Evaluation
		Console.WriteLine("--- 1. Basic Composition & Evaluation ---");
		Function p = new Function(3 * ( Flex.x ^ 2 ) - 5 * Flex.x + 10, "p");
		Console.WriteLine(p);
		Console.WriteLine($"p(2) = {p[2]} \t\t\t (Expected: 12)");
		Console.WriteLine();

		// 2. Multivariable Evaluation
		Console.WriteLine("--- 2. Multivariable Ordering ---");
		// surface(x, y, z) = x^2 + y^2 * x - z^3
		Function surface = new Function(( Flex.x ^ 2 ) + ( Flex.y ^ 2 ) * Flex.x - ( Flex.z ^ 3 ), "S");
		Console.WriteLine(surface);
		// Evaluate x=2, y=3, z=1 => (2)^2 + (3)^2*(2) - (1)^3 = 4 + 18 - 1 = 21
		Console.WriteLine($"S(2, 3, 1) = {surface[2, 3, 1]} \t\t (Expected: 21)");
		Console.WriteLine();

		// 3. First-Order Partial Derivatives
		Console.WriteLine("--- 3. First-Order Partial Derivatives ---");
		Function f = new Function(( Flex.x ^ 3 ) * Flex.y + ( Flex.y ^ 2 ) * ( Flex.z ^ 2 ), "f");
		Console.WriteLine(f);
		Console.WriteLine($"df/dx: {f.Derivative(Flex.x)}");
		Console.WriteLine($"df/dy: {f.Derivative(Flex.y)}");
		Console.WriteLine();

		// 4. Higher-Order & Mixed Derivatives (Clairaut's Theorem Test)
		Console.WriteLine("--- 4. Mixed Partial Derivatives ---");
		Function mixed = new Function(5 * ( Flex.x ^ 2 ) * ( Flex.y ^ 3 ), "M");
		Console.WriteLine(mixed);

		// Differentiate wrt x, then y
		Function dMdx = mixed.Derivative(Flex.x);
		Function dMdxdY = dMdx.Derivative(Flex.y);

		// Differentiate wrt y, then x
		Function dMdy = mixed.Derivative(Flex.y);
		Function dMdydX = dMdy.Derivative(Flex.x);

		Console.WriteLine($"M_xy: {dMdxdY}");
		Console.WriteLine($"M_yx: {dMdydX}");
		Console.WriteLine($"Match? {dMdxdY.ToString() == dMdydX.ToString()} \t (Expected: True)");
		Console.WriteLine();

		// 5. The Gradient
		Console.WriteLine("--- 5. The Gradient ---");
		Function g = new Function(( Flex.x ^ 2 ) + 3 * Flex.x * Flex.y - ( Flex.y ^ 3 ), "g");
		Console.WriteLine(g);
		Function[] grad = g.Gradient();
		Console.WriteLine("Gradient Vector:");
		for (int i = 0; i < grad.Length; i++) {
			Console.WriteLine($"  Index {i}: {grad[i]}");
		}
		Console.WriteLine();

		// 6. The Heinous Poly Test
		Console.WriteLine("--- 6. The Heinous Poly Test ---");
		Function heinousPoly = new Function(
			( 13 * ( Flex.x ^ 7 ) * ( Flex.y ^ 4 ) )
			- ( 8 * ( Flex.z ^ 5 ) * ( Flex.t ^ 9 ) )
			+ ( 42 * ( Flex.x ^ 2 ) * Flex.y * ( Flex.z ^ 3 ) * ( Flex.t ^ 2 ) )
			- ( 256 * ( Flex.y ^ 12 ) )
			+ ( ( ( Flex.x ^ 5 ) * ( Flex.t ^ 6 ) ) ^ 2 )
			+ 123,
			"heinous"
		);
		Console.WriteLine(heinousPoly);
		Console.WriteLine($"Heinous Gradient Length: {heinousPoly.Gradient().Length} partials");
		Console.WriteLine($"Heinous df/dt:");
		Console.WriteLine($"  {heinousPoly.Derivative(Flex.t)}");

	}

}