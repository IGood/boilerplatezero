using System.Windows;

namespace FxPropMetadata
{
	public partial class TestMe
	{
		public static readonly DependencyProperty Text1Property = Gen.Text1("", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault);

		public static readonly DependencyProperty Text2Property = Gen.Text2<string?>(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault);

		public static readonly DependencyProperty Text3Property = Gen.Text3<string?>(FrameworkPropertyMetadataOptions.BindsTwoWayByDefault);

		// This one is bad (compiler error) because no generic argument is specified.
		public static readonly DependencyProperty BadProperty = Gen.Bad(FrameworkPropertyMetadataOptions.None);
	}
}
