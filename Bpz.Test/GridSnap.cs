// Copyright © Ian Good

using System;
using System.Windows;
using System.Windows.Controls;

namespace Bpz.Test
{
	public static partial class GridSnap
	{
		public static readonly DependencyProperty IsEnabledProperty = GenAttached<UIElement>.IsEnabled(false, FrameworkPropertyMetadataOptions.Inherits);

		public static readonly RoutedEvent IsEnabledChangedEvent = EventManager.RegisterRoutedEvent("IsEnabledChanged", RoutingStrategy.Direct, typeof(RoutedPropertyChangedEventHandler<bool>), typeof(GridSnap));

		private static void IsEnabledPropertyChanged(UIElement elem, DependencyPropertyChangedEventArgs e)
		{
			/*
			elem.RaiseEvent(new RoutedPropertyChangedEventArgs<bool>((bool)e.OldValue, (bool)e.NewValue, IsEnabledChangedEvent));
			/*/
			elem.RaiseRoutedPropertyChangedEvent<bool>(IsEnabledChangedEvent, e);
			//*/
		}

		public static readonly DependencyProperty CellSizeProperty = GenAttached<Canvas>.CellSize(100.0, FrameworkPropertyMetadataOptions.Inherits);
	}

	public static class RoutedPropertyChangedEventArgsOps
	{
		public static void RaiseRoutedPropertyChangedEvent<T>(this UIElement elem, RoutedEvent routedEvent, DependencyPropertyChangedEventArgs e)
		{
			if (routedEvent.HandlerType != typeof(RoutedPropertyChangedEventHandler<T>))
			{
				throw new Exception("Invalid handler type.");
			}

			if (e.Property.PropertyType != typeof(T))
			{
				throw new Exception("Incorrect property type.");
			}

			elem.RaiseEvent(new RoutedPropertyChangedEventArgs<T>((T)e.OldValue, (T)e.NewValue, routedEvent));
		}
	}
}
