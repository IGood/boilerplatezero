using System.Windows;

namespace MinimalRE
{
	public partial class TestMe : UIElement
	{
		public static readonly RoutedEvent FooChangedEvent = Gen.FooChanged<int>();

		public static readonly RoutedEvent BarChangedEvent = Gen.BarChanged<int?>();

		public static readonly RoutedEvent BazChangedEvent = Gen.BazChanged<string?>();
	}
}
