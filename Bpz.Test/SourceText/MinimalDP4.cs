namespace MinimalDP
{
	public partial class TestMe : System.Windows.DependencyObject
	{
		public static readonly System.Windows.DependencyProperty FooProperty = Gen.Foo<int>();
	}
}

namespace WpfApp1
{
	using System.Windows;

	public partial class TestMe : DependencyObject
	{
		public static readonly DependencyProperty NameProperty = Gen.Name(string.Empty);
		public static readonly DependencyProperty AgeProperty = Gen.Age<int>();

		public TestMe()
		{
			this.Name = "bob";
			this.Age = 61;
		}
	}

	public partial class TestMeToo : DependencyObject
	{
		private static readonly DependencyPropertyKey IdPropertyKey = Gen.Id(System.Guid.Empty);

		public TestMeToo()
		{
			this.Id = System.Guid.NewGuid();
		}
	}
}
