using System.Windows;

namespace MinimalDP.GenericOwner
{
	public partial class TestMe<U, TBargle> : DependencyObject
	{
		public static readonly DependencyProperty FooProperty = Gen.Foo<int>();
		public static readonly DependencyProperty BarProperty = Gen.Bar<TBargle>();
	}
}
