// Copyright © Ian Good

using System.Collections.Generic;
using System.Windows;

namespace Bpz.Test
{
	public static partial class MyService
	{
		public static readonly RoutedEvent ThingUpdatedEvent = GenAttached.ThingUpdated();

		public static readonly RoutedEvent FooChangedEvent = GenAttached.FooChanged<RoutedPropertyChangedEventHandler<int>>(RoutingStrategy.Bubble);

		public static readonly RoutedEvent BarChangedEvent = GenAttached.BarChanged<List<float>>();

		public static readonly RoutedEvent InspectedEvent = GenAttached<System.Windows.Controls.Canvas>.Inspected();

		public static readonly RoutedEvent SomeToolTipEvent = GenAttached.SomeToolTip<System.Windows.Controls.ToolTipEventHandler>();

		public static readonly RoutedEvent DataTransferEvent = GenAttached.DataTransfer<System.EventHandler<System.Windows.Data.DataTransferEventArgs>>();

		private static readonly RoutedEvent SomethingPrivateHappenedEvent = GenAttached.SomethingPrivateHappened();
	}
}
