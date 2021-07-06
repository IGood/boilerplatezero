using System.Windows;

namespace MinimalRE
{
	public partial class TestMe : UIElement
	{
		/// <summary>
		/// Occurs when the <see cref="P:Foo"/> property changes.
		/// </summary>
		public static readonly RoutedEvent FooChangedEvent = Gen.FooChanged<RoutedPropertyChangedEventHandler<int>>();
	}
}
