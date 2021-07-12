// Copyright © Ian Good

using System.Windows;
using System.Windows.Controls;

namespace Bpz.Test
{
	public abstract partial class NumericUpDownBase<TValue> : Control
	{
		public static readonly DependencyProperty ValueProperty = Gen.Value<TValue>(FrameworkPropertyMetadataOptions.BindsTwoWayByDefault);

		public static readonly RoutedEvent ValueChangedEvent = Gen.ValueChanged<TValue>();

		//protected virtual void OnValueChanged(TValue oldValue, TValue newValue)
		//{
		//	this.RaiseEvent(new RoutedPropertyChangedEventArgs<TValue>(oldValue, newValue, ValueChangedEvent));
		//}
	}

	public class DoubleUpDown : NumericUpDownBase<double> { }
}
