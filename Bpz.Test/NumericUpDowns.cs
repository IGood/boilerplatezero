// Copyright © Ian Good

using System.Windows;
using System.Windows.Controls;

namespace Bpz.Test
{
	public abstract partial class NumericUpDownBase<TValue> : Control
	{
		public static readonly DependencyProperty ValueProperty = Gen.Value<TValue>(FrameworkPropertyMetadataOptions.BindsTwoWayByDefault);

		public static readonly RoutedEvent ValueChangedEvent = Gen.ValueChanged<TValue>();
	}

	public class DoubleUpDown : NumericUpDownBase<double> { }
}
