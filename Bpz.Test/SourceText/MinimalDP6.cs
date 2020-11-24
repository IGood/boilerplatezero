#nullable enable

using System;
using System.Windows;

namespace MinimalDP
{
	public partial class TestMe : DependencyObject
	{
		public static readonly DependencyProperty Foo1Property = Gen.Foo1<string>();
		public static readonly DependencyProperty Foo2Property = Gen.Foo2((string)null!);
		public static readonly DependencyProperty Bar1Property = Gen.Bar1<string?>();
		public static readonly DependencyProperty Bar2Property = Gen.Bar2((string?)null);

		public static readonly DependencyProperty MyIntProperty = Gen.MyInt<int>();
		public static readonly DependencyProperty MyNint1Property = Gen.MyNint1<int?>();
		public static readonly DependencyProperty MyNint2Property = Gen.MyNint2<Nullable<int>>();

		public static readonly DependencyProperty StartProperty = Gen.Start<Point>();
		public static readonly DependencyProperty End1Property = Gen.End1<Point?>();
		public static readonly DependencyProperty End2Property = Gen.End2<Nullable<Point>>();

		public static readonly DependencyProperty Wid1Property = Gen.Wid1<Widget>();
		public static readonly DependencyProperty Wid2Property = Gen.Wid2<Widget?>();
	}

	public struct Point
	{
		public double X { get; set; }
		public double Y { get; set; }
	}

	public class Widget
	{
		public string Name { get; set; }
	}
}
