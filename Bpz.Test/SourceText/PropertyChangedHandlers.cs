using System.Windows;
using Butt = System.Windows.Controls.Button;

namespace PropsChanged
{
	public partial class TestMe : Butt
	{
		public static readonly DependencyProperty AProperty = Gen.A<int>();
		private static void APropertyChanged(TestMe self, DependencyPropertyChangedEventArgs e) { }

		public static readonly DependencyProperty BProperty = Gen.B<int>();
		private static void OnBPropertyChanged(Butt self, DependencyPropertyChangedEventArgs e) { }

		public static readonly DependencyProperty CProperty = Gen.C<int>();
		private static void CChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) { }

		public static readonly DependencyProperty DProperty = Gen.D<int>();
		//private static void OnDChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) { }

		public static readonly DependencyProperty EProperty = Gen.E<int>(123);
		//private static void OnEChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) { }

		public static readonly DependencyProperty FProperty = Gen.F<int>(456);
		private static void OnFChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) { }

		// void PropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
		// object CoerceValueCallback(DependencyObject d, object baseValue)
		// private static object OnCoerceClip(DependencyObject d, object baseValue)

		// OnNamePropertyChanged
		//   NamePropertyChanged
		// OnName        Changed

		// DependencyObject, OwnerType
	}
}
