using System.Windows;

namespace MinimalDP.ReadOnly
{
	public partial class TestMe : DependencyObject
	{
		protected static readonly DependencyPropertyKey BazPropertyKey = Gen.Baz(true);
	}
}
