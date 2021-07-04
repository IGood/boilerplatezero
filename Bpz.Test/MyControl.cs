// Copyright © Ian Good

using System.Windows;

namespace Bpz.Test
{
	public partial class MyControl : UIElement
	{
		public static readonly RoutedEvent FooChangedEvent = Gen.FooChanged<RoutedPropertyChangedEventHandler<int>>();

		public static readonly DependencyProperty FooProperty = Gen.Foo(123);

		private static void FooPropertyChanged(UIElement elem, DependencyPropertyChangedEventArgs e)
		{
			elem.RaiseEvent(new RoutedPropertyChangedEventArgs<int>((int)e.OldValue, (int)e.NewValue, FooChangedEvent));
		}

		public static readonly RoutedEvent ThingUpdatedEvent = Gen.ThingUpdated(RoutingStrategy.Bubble);

		public void DoThingUpdate()
		{
			this.RaiseEvent(new RoutedEventArgs(ThingUpdatedEvent, this));
		}
	}
}
