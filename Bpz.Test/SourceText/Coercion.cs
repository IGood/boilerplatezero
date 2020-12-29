using System.Windows;
using System.Windows.Controls;

namespace PropsChanged
{
	public partial class TestMe : Button
	{
		public static readonly DependencyProperty AProperty = Gen.A<int>();
		private static object CoerceA(TestMe self, object baseValue) { return baseValue; }

		public static readonly DependencyProperty BProperty = Gen.B<int>();
		private static object CoerceB(Button self, object baseValue) { return baseValue; }

		public static readonly DependencyProperty CProperty = Gen.C<int>();
		private static object CoerceC(object d, object baseValue) { return baseValue; }

		public static readonly DependencyProperty DProperty = Gen.D<int>();
		
		public static readonly DependencyProperty EProperty = Gen.E<int>(123);

		public static readonly DependencyProperty FProperty = Gen.F<int>(456);
		private static object CoerceF(DependencyObject d, int baseValue) { return baseValue; }

		public static readonly DependencyProperty NameProperty = GenAttached.Name("None");
		private static string CoerceName(DependencyObject d, string baseValue) { return baseValue; }
	}
}
