using System.Windows;

namespace FxPropMetadata
{
	public partial class TestMe
	{
		public static readonly DependencyProperty Text1Property = Gen.Text1("", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault);

		public static readonly DependencyProperty Text2Property = Gen.Text2<string?>(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault);

		public static readonly DependencyProperty Text3Property = Gen.Text3<string?>(FrameworkPropertyMetadataOptions.BindsTwoWayByDefault);

		// This one is bad (compiler error) because no generic argument is specified.
		public static readonly DependencyProperty Bad1Property = Gen.Bad1(FrameworkPropertyMetadataOptions.None);

		// This one is bad (compiler error) because we don't support dependency properties whose type is `FrameworkPropertyMetadataOptions`.
		public static readonly DependencyProperty Bad2Property = Gen.Bad2(FrameworkPropertyMetadataOptions.None, FrameworkPropertyMetadataOptions.None);

		// This one is bad (compiler error) because we don't support dependency properties whose default value is `FrameworkPropertyMetadataOptions`.
		public static readonly DependencyProperty Bad3Property = Gen.Bad3<object>(FrameworkPropertyMetadataOptions.None, FrameworkPropertyMetadataOptions.None);
	}
}
