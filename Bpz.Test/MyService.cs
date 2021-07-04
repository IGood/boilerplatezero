// Copyright © Ian Good

using System.Windows;

namespace Bpz.Test
{
	public static partial class MyService
	{
		public static readonly RoutedEvent ThingUpdatedEvent = GenAttached.ThingUpdated();

		public static readonly RoutedEvent FooChangedEvent = GenAttached.FooChanged<RoutedPropertyChangedEventHandler<int>>(RoutingStrategy.Bubble);
	}
}
