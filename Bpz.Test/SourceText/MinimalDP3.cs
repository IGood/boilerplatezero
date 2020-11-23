using System.Windows;

namespace MinimalDP.PrivateProtected
{
	public partial class TestMe : DependencyObject
	{
		static readonly DependencyPropertyKey BazPropertyKey = Gen.Baz(true);
		protected static readonly DependencyProperty BazProperty = BazPropertyKey.DependencyProperty;
	}
}
