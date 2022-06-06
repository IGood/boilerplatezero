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
			elem.RaiseRoutedPropertyChangedEvent<bool>(IsEnabledChangedEvent, e);
		}

		/// <summary>
		/// Represents the dimensions of a grid cell to which elements will be snapped.
		/// </summary>
		public static readonly DependencyProperty CellSizeProperty = GenAttached<Canvas>.CellSize(100.0, FrameworkPropertyMetadataOptions.Inherits);
	}

	public static class RoutedPropertyChangedEventArgsOps
	{
		/// <summary>
		/// Raises a specific routed property changed event.<br/>
		/// <paramref name="routedEvent"/> must be a <see cref="RoutedPropertyChangedEventHandler{T}"/> of
		/// <typeparamref name="T"/>.<br/>
		/// The type of the changed property must be <typeparamref name="T"/>.
		/// </summary>
		public static void RaiseRoutedPropertyChangedEvent<T>(this UIElement elem, RoutedEvent routedEvent, DependencyPropertyChangedEventArgs e)
		{
			if (routedEvent.HandlerType != typeof(RoutedPropertyChangedEventHandler<T>))
			{
				throw new ArgumentException("Invalid handler type.");
			}

			if (e.Property.PropertyType != typeof(T))
			{
				throw new ArgumentException("Invalid property type.");
			}

			elem.RaiseEvent(new RoutedPropertyChangedEventArgs<T>((T)e.OldValue, (T)e.NewValue, routedEvent));
		}
	}
}
