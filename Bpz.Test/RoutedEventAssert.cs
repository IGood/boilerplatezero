// Copyright © Ian Good

using NUnit.Framework;
using System;
using System.Reflection;
using System.Windows;

namespace Bpz.Test
{
	public static class RoutedEventTestOps
	{
		public static void AssertValues<TExpectedOwner, TExpectedHandler>(RoutedEvent routedEvent, string expectedName, RoutingStrategy expectedRoutingStrategy)
		{
			AssertValues(routedEvent, expectedName, expectedRoutingStrategy, typeof(TExpectedHandler), typeof(TExpectedOwner));

			var eventInfo = typeof(TExpectedOwner).GetEvent(expectedName, BindingFlags.Public | BindingFlags.Instance);
			Assert.IsNotNull(eventInfo);
			if (eventInfo != null)
			{
				Assert.AreEqual(typeof(TExpectedHandler), eventInfo.EventHandlerType);
			}
		}

		public static void AssertValues<TExpectedHandler>(RoutedEvent routedEvent, string expectedName, RoutingStrategy expectedRoutingStrategy, Type expectedOwnerType)
		{
			AssertValues(routedEvent, expectedName, expectedRoutingStrategy, typeof(TExpectedHandler), expectedOwnerType);

			_AssertStaticMethod($"Add{expectedName}Handler");
			_AssertStaticMethod($"Remove{expectedName}Handler");

			void _AssertStaticMethod(string expectedName)
			{
				var methodInfo = expectedOwnerType.GetMethod(expectedName, BindingFlags.Public | BindingFlags.Static);
				Assert.IsNotNull(methodInfo);
				if (methodInfo != null)
				{
					var parameters = methodInfo.GetParameters();
					Assert.AreEqual(2, parameters.Length);
					if (parameters.Length == 2)
					{
						Assert.AreEqual(typeof(DependencyObject), parameters[0].ParameterType);
						Assert.AreEqual(typeof(TExpectedHandler), parameters[1].ParameterType);
					}
				}
			}
		}

		private static void AssertValues(RoutedEvent routedEvent, string expectedName, RoutingStrategy expectedRoutingStrategy, Type expectedHandlerType, Type expectedOwnerType)
		{
			Assert.AreEqual(expectedName, routedEvent.Name);
			Assert.AreEqual(expectedRoutingStrategy, routedEvent.RoutingStrategy);
			Assert.AreEqual(expectedHandlerType, routedEvent.HandlerType);
			Assert.AreEqual(expectedOwnerType, routedEvent.OwnerType);
		}
	}
}
