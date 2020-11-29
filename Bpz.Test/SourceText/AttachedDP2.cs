using System.Windows;
using System.Windows.Controls;

namespace AttachedDP.GenericOwner
{
	public static partial class TestMe<T>
	{
		public static readonly DependencyProperty FooProperty = GenAttached.Foo<int>();
		public static readonly DependencyProperty BarProperty = GenAttached.Bar<T>();
	}
}
