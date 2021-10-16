// Copyright © Ian Good

using System;
using System.Collections.Generic;
using System.Windows;

namespace Bpz.Test
{
	public partial class Widget : DependencyObject
	{
		public static readonly DependencyProperty MyBool0Property = Gen.MyBool0(true);
		public static readonly DependencyProperty MyBool1Property = Gen.MyBool1<bool>();
		public static readonly DependencyProperty MyBool2Property = Gen.MyBool2((bool?)false);

		/// <summary>
		/// Test dox! Gets or sets a string value or something.
		/// </summary>
		public static readonly DependencyProperty MyString0Property = Gen.MyString0("asdf");
		public static readonly DependencyProperty MyString1Property = Gen.MyString1<string?>();
		public static readonly DependencyProperty MyString2Property = Gen.MyString2(default(string?));
		public static readonly DependencyProperty MyString3Property = Gen.MyString3("qwer");

		public static readonly DependencyProperty MyDictionaryProperty = Gen.MyDictionary<Dictionary<int, List<string>>?>();

		private static void MyString0PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			((Widget)d).MyString0Changed?.Invoke(d, EventArgs.Empty);
		}

		private static void MyString1PropertyChanged(Widget self, DependencyPropertyChangedEventArgs e)
		{
			self.MyString1Changed?.Invoke(self, EventArgs.Empty);
		}

		private void OnMyString2Changed(string? oldValue, string? newValue)
		{
			this.MyString2Changed?.Invoke(this, EventArgs.Empty);
		}

		private void OnMyString3Changed(DependencyPropertyChangedEventArgs e)
		{
			this.MyString3Changed?.Invoke(this, EventArgs.Empty);
		}

		public event EventHandler? MyString0Changed;
		public event EventHandler? MyString1Changed;
		public event EventHandler? MyString2Changed;
		public event EventHandler? MyString3Changed;

		private static readonly DependencyPropertyKey MyFloat0PropertyKey = Gen.MyFloat0(3.14f);
		protected static readonly DependencyPropertyKey MyFloat1PropertyKey = Gen.MyFloat1<float>();

		private static float CoerceMyFloat1(Widget self, float baseValue)
		{
			return Math.Abs(baseValue);
		}

		private void OnMyFloat1Changed(float old, float @new)
		{
			this.MyFloat1Changed?.Invoke(this, EventArgs.Empty);
		}

		public void SetMyFloat1(float value) => this.MyFloat1 = value;

		public event EventHandler? MyFloat1Changed;

		private static readonly DependencyPropertyKey MyNinjaPropertyKey = Gen.MyNinja<NinjaTurtle>();
		protected static readonly DependencyProperty MyNinjaProperty = MyNinjaPropertyKey.DependencyProperty;

		protected enum NinjaTurtle
		{
			Leo,
			Raph,
			Mike,
			Don,
		}
	}
}
