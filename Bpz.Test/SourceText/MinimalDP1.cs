using System.Windows;

namespace MinimalDP
{
	public partial class TestMe : DependencyObject
	{
		public static readonly DependencyProperty FooProperty = Gen.Foo(default(int));
	}
}
