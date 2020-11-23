using System.Windows;

namespace MinimalDP
{
	public partial class TestMe : DependencyObject
	{
		public static readonly DependencyProperty FooProperty = Gen.Foo<int>();
	}
}

namespace MinimalDP
{
	partial class TestMe
	{
		public static readonly DependencyProperty BarProperty = Gen.Bar(3.14f);

		private static readonly int DefaultBazValue = -12;
		public static readonly DependencyProperty BazProperty = Gen.Baz(DefaultBazValue);

		public static readonly DependencyProperty LocationProperty = Gen.Location(new Point { X = 8, Y = 9 });

		public class Point
		{
			public float X { get; set; }
			public float Y { get; set; }
		}
	}
}

namespace MinimalDP2
{
	using System.Collections.Generic;

	public partial class TestMe : DependencyObject
	{
		public static readonly DependencyProperty FooProperty = Gen.Foo<int?>(123);

		private static readonly DependencyPropertyKey BarPropertyKey = Gen.Bar<List<int>>();
	}
}
