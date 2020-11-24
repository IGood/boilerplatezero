using System.Windows;

namespace MinimalDP.ReadOnly
{
	public partial class TestMe : DependencyObject
	{
		/// <summary>
		/// Gets or sets the Baz. This is good documentation.
		/// </summary>
		protected static readonly DependencyPropertyKey BazPropertyKey = Gen.Baz(true);
	}
}
