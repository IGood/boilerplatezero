using System.Windows;

namespace MinimalRE
{
	public partial class TestMe : UIElement
	{
		// This is not documentation.
		public static readonly RoutedEvent FooChangedEvent = Gen.FooChanged();
	}
}
