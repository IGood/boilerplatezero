// Copyright © Ian Good

using NUnit.Framework;
using System.Windows;

namespace Bpz.Test
{
	/// <summary>
	/// Exercises basic attached property behavior.
	/// This won't compile if the properties we expect weren't generated.
	/// </summary>
	public class MyServiceTests
	{
		[Test(Description = "Attached event properties should have expected values.")]
		public void ExpectEventProperties()
		{
			RoutedEventTestOps.AssertValues<RoutedEventHandler>(
				MyService.ThingUpdatedEvent, "ThingUpdated", RoutingStrategy.Direct, typeof(MyService));

			RoutedEventTestOps.AssertValues<RoutedPropertyChangedEventHandler<int>>(
				MyService.FooChangedEvent, "FooChanged", RoutingStrategy.Bubble, typeof(MyService));
		}

		[Test(Description = "Event handlers should get called.")]
		public void ExpectEvents()
		{
			// TODO
		}
	}
}
