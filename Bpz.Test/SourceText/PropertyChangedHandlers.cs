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
		private static void CChanged(object d, DependencyPropertyChangedEventArgs e) { }

		public static readonly DependencyProperty DProperty = Gen.D<int>();
		//private static void OnDChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) { }

		public static readonly DependencyProperty EProperty = Gen.E<int>(123);
		//private static void OnEChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) { }

		public static readonly DependencyProperty FProperty = Gen.F<int>(456);
		private static void OnFChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) { }

		public static readonly DependencyProperty FProperty = GenAttached<Butt>.Name("None");
		private static void NamePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) { }

		public static readonly DependencyProperty GProperty = Gen.G<int?>();
		protected virtual void GChanged(DependencyPropertyChangedEventArgs args) { }

		public static readonly DependencyProperty HProperty = Gen.H<int>();
		protected virtual void OnHChanged(int oldH, int newH) { }

		public static readonly DependencyProperty IProperty = Gen.I<string?>();
		protected virtual void OnIChanged(object newI, object oldI) { }

		public static readonly DependencyProperty JProperty = Gen.J((string?)null);
		protected virtual void OnJChanged(string? newJ, string? oldJ) { }

		// void PropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)

		// OnNamePropertyChanged
		//   NamePropertyChanged
		// OnName        Changed
	}
}
