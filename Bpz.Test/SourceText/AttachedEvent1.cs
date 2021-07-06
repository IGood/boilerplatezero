using System.Windows;

namespace AttachedRE
{
	public partial class TestMe
	{
		// This is not documentation.
		public static readonly RoutedEvent FooChangedEvent = GenAttached.FooChanged();
	}
}
