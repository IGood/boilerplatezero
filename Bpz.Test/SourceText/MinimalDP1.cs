using System.Windows;

namespace MinimalDP
{
	public partial class TestMe : DependencyObject
	{
		// This is not documentation.
		public static readonly DependencyProperty FooProperty = Gen.Foo(default(int));
	}
}
