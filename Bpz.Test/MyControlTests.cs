// Copyright © Ian Good

using NUnit.Framework;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;

namespace Bpz.Test
{
	/// <summary>
	/// Exercises basic routed event behavior.
	/// This won't compile if the events we expect weren't generated.
	/// </summary>
	public class MyControlTests
	{
		[TestCaseSource(nameof(MetadataTestCases))]
		public void ExpectMetadata(RoutedEventAssert.RoutedEventValues testCase)
		{
			RoutedEventAssert.Matches(testCase);
		}

		public static IEnumerable<RoutedEventAssert.RoutedEventValues> MetadataTestCases
		{
			get
			{
				yield return new()
				{
					OwnerType = typeof(MyControl),
					Name = "FooChanged",
					HandlerType = typeof(RoutedPropertyChangedEventHandler<int>),
				};

				yield return new()
				{
					OwnerType = typeof(MyControl),
					Name = "BarChanged",
					HandlerType = typeof(RoutedPropertyChangedEventHandler<int>),
				};

				yield return new()
				{
					OwnerType = typeof(MyControl),
					Name = "ThingUpdated",
					RoutingStrategy = RoutingStrategy.Bubble,
				};

				yield return new()
				{
					OwnerType = typeof(MyControl),
					Name = "SomethingProtectedHappened",
					Visibility = MethodAttributes.Family,
				};

				yield return new()
				{
					OwnerType = typeof(MyControl),
					Name = "SomethingPrivateHappened",
					Visibility = MethodAttributes.Private,
				};
			}
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
