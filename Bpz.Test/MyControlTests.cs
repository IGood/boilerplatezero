// Copyright © Ian Good

using NUnit.Framework;
using System.Windows;

namespace Bpz.Test
{
	/// <summary>
	/// Exercises basic routed event behavior.
	/// This won't compile if the events we expect weren't generated.
	/// </summary>
	public class MyControlTests
	{
		[Test(Description = "Routed event properties should have expected values.")]
		public void ExpectEventProperties()
		{
			RoutedEventTestOps.AssertValues<MyControl, RoutedPropertyChangedEventHandler<int>>(
				MyControl.FooChangedEvent, "FooChanged", RoutingStrategy.Direct);

			RoutedEventTestOps.AssertValues<MyControl, RoutedEventHandler>(
				MyControl.ThingUpdatedEvent, "ThingUpdated", RoutingStrategy.Bubble);
		}

		[Test(Description = "Event handlers should get called.")]
		public void ExpectEvents()
		{
			var c = new MyControl();

			bool eventWasRaised;
			int expectedFoo = 0xBAD;

			c.FooChanged += (s, e) =>
			{
				Assert.AreSame(c, s);
				Assert.AreEqual(expectedFoo, e.NewValue);
				eventWasRaised = true;
			};

			{
				eventWasRaised = false;
				expectedFoo = 456;
				c.Foo = expectedFoo;
				Assert.IsTrue(eventWasRaised);
			}
			{
				eventWasRaised = false;
				expectedFoo = -789;
				c.Foo = expectedFoo;
				Assert.IsTrue(eventWasRaised);
			}

			c.ThingUpdated += (s, e) =>
			{
				Assert.AreSame(c, s);
				eventWasRaised = true;
			};

			{
				eventWasRaised = false;
				c.DoThingUpdate();
				Assert.IsTrue(eventWasRaised);
			}
		}
	}
}
