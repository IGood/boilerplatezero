using System.Windows;
using Butt = System.Windows.Controls.Button;

namespace PropChangedEvents
{
	public partial class TestMe : Butt
	{
		public static readonly DependencyProperty AProperty = Gen.A<int>();
		public static readonly RoutedEvent AChangedEvent = Gen.AChanged<int>();

		public static readonly DependencyProperty BProperty = Gen.B<object>();
		public static readonly RoutedEvent BChangedEvent = Gen.BChanged<object>();
	}
}
