using System;
using System.Diagnostics;

namespace Flexpressions {

	class Program {

		static void Main(string[] args) {

			MonoEx<float> one = new MonoEx<float>(1);
			MonoEx<int> ntwo = new MonoEx<int>(-2);
			MonoEx<int> nthree = new MonoEx<int>(-3);
			MonoEx<int> quad = new MonoEx<int>('x', 2);
			MonoEx<double> flippy = new MonoEx<double>(7, 't', -2);
			MonoEx<double> cube = new MonoEx<double>('t', -3);
			MonoEx<double> quart = new MonoEx<double>('x', 4);

			Console.WriteLine(one);
			Console.WriteLine(ntwo);
			Console.WriteLine(quad);
			Console.WriteLine(flippy);
			Console.WriteLine();

			Console.WriteLine(ntwo * quad);
			Console.WriteLine(ntwo * nthree);
			Console.WriteLine(flippy * cube);
			Console.WriteLine(flippy * cube * quart);

			PolyEx<int> poly = ntwo + quad;
			Console.WriteLine(poly);
			

			//Console.WriteLine("\u207B");

		}

	}
}