<p align="center">
<img width="570" height="100" alt="Untitled-1" src="https://github.com/user-attachments/assets/1577e9f9-794b-430d-ab04-a8b40c25054f" />
</p>

# Flexpressions

A symbolic algebra utility for C# that introduces flexible and adorable polynomial composition and evaluation. 

*Who said you need linear algebra?*

## Demo

*No string parsing here!*

<img width="900" height="300" alt="FlexpressionsDemoA" src="https://github.com/user-attachments/assets/90eeda23-8aa0-4f53-920e-9ae4b0aa7565" />

## About

Flexpressions is a polynomial math eDSL/tool for C#. 

**Whiteboard:** The utility focuses on syntactical sugar which seeks to resemble marker-on-whiteboard syntax as closely as the C# compiler allows. This specific language is referred to as *Whiteboard.* Write math functions with familar notation while also leveraging the added power that comes with computer programming languages. 

**Flexibility:** Flexpressions is flexible in its syntax but also in its overall usage. By default, Flexpressions can be used *immutably with Whiteboard,* or with more programming-standard *mutable behavior for performance-intensive scenarios* where there is a weaker desire for backend aesthetics and accessibility. Additionally, the legacy version of Flexpressions can be used when C# generic math is desired, though with worse performance and syntax versus the standard, nongeneric Flexpressions.

**Abstraction:** Flexpression has its own stack of moving parts, but Whiteboard usage patterns completely hide this, allowing the devloper to *focus entirely on the polynomials without regard for boilerplate.* 

**Performance:** Flexpressions does not seek to match the performance of other premier algebra engines, and tends to prioritize syntax over all else, but the tool does not neglect memory and runtime optimizations, even in Whiteboard. 

## Use Cases

Flexpressions allows for polynomial manipulation in C# that anyone who knows algebra can understand by looking at, as Whiteboard has minimal boilerplate. The tool is great for use cases where developer experience beats out raw performance. 

**Computation for Non-Technical Developers:** Flexpression's emphasis on developer experience and Whiteboard allows non-programmers to do math in a highly familiar format as they navigate the complexities of programming languages. In large teams, non-technical devlopers can intuitively understand and tweak backend math when needed, as Whiteboard largely looks just like that stuff from 8th grade.

**Educational Tooling:** Flexpressions can be used in academic scenarios where students need to work with polynomials without an emphasis on how to manipulate some computerized polynomial abstraction, as Whiteboard makes it as simple as typical algebra.

**Designer-Facing Tools:** Since Flexpressions puts DX before performance, tools that are meant for other programmers more than end users can leverage Flexpressions for math. 

**Math GUI:** Flexpressions supports robust string conversions for displaying its objects in a console (or anywhere else). 

**Graphics Computation:** Flexpressions is not the ideal choice for raw performance. However, in some cases, designers of graphics engines may desire DX over minimized performance footprint, making this tool ideal.

Additionally, Flexpressions is perfect if your math never goes far beyond what polynomial functions can achieve. 

## Versions

Flexpressions has 2 main versions: Standard (nongeneric) and Legacy (generic).

The Standard version is the optimal choice for most use cases. It is the better choice out of the two for performance, ease of use, and robustness.

The Legacy version has one edge over the nongeneric version: it supports C# generic math. If you really want C# generic math, you can use legacy. 

Legacy will ideally recieve continual maintenance to establish functional parity between the versions, but some features and optimizations are impossible in the Legacy system, which is why Standard is nongeneric, on top of the fact that the boilerplate for generics weakens Whiteboard.

## Roadmap

(in no specific order)

- Further performance improvements.

- Basic calculus for functions.

- Advanced functions, such as rational, exponential, floating-point power, etc., with the abiltiy to compose functions together, ideally all with Whiteboard support. 

- Function graphing.

- Use actual variables (i.e., memory locations) as variables in functions.

- String formatting options.


