using System.Windows;
using System.Windows.Controls;

namespace AttachedDP
{
	public static partial class TestMe
	{
		public static readonly DependencyProperty FooProperty = GenAttached.Foo<string>();

		public static readonly DependencyProperty BarProperty = GenAttached<Button>.Barz<string>();
	}
}
