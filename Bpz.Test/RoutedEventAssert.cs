// Copyright © Ian Good

using NUnit.Framework;
using System;
using System.Reflection;
using System.Windows;

namespace Bpz.Test
{
	public static class RoutedEventAssert
	{
		public static void Matches(RoutedEventValues expectedValues)
		{
			string expectedFieldName = expectedValues.Name + "Event";

			FieldInfo? reFieldInfo = expectedValues.OwnerType.GetField(expectedFieldName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			Assert.IsNotNull(reFieldInfo, $"Missing static routed event field `{expectedFieldName}`.");

			// Check routed event instance.
			RoutedEvent? routedEvent = reFieldInfo!.GetValue(null) as RoutedEvent;
			Assert.IsNotNull(routedEvent);

			Assert.AreEqual(expectedValues.OwnerType, routedEvent!.OwnerType);
			Assert.AreEqual(expectedValues.Name, routedEvent.Name);
			Assert.AreEqual(expectedValues.HandlerType, routedEvent.HandlerType);
			Assert.AreEqual(expectedValues.RoutingStrategy, routedEvent.RoutingStrategy);

			if (expectedValues.IsAttached)
			{
				// Check generated add/remove methods.
				_AssertMethod($"Add{expectedValues.Name}Handler");
				_AssertMethod($"Remove{expectedValues.Name}Handler");

				void _AssertMethod(string name)
				{
					MethodInfo? methodInfo = expectedValues.OwnerType.GetMethod(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

					// Exists?
					Assert.IsNotNull(methodInfo, $"Missing method `{name}`.");

					// Visibility?
					Assert.IsTrue(methodInfo!.Attributes.HasFlag(expectedValues.Visibility));

					// Parameters?
					ParameterInfo[] parameters = methodInfo.GetParameters();
					Assert.AreEqual(2, parameters.Length);

					Type targetType = expectedValues.AttachmentNarrowingType ?? typeof(DependencyObject);
					Assert.AreEqual(targetType, parameters[0].ParameterType);
					Assert.AreEqual(expectedValues.HandlerType, parameters[1].ParameterType);
				}
			}
			else
			{
				// Check generated CLR event.
				EventInfo? clrEventEventInfo = expectedValues.OwnerType.GetEvent(expectedValues.Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

				// Exists?
				Assert.IsNotNull(clrEventEventInfo, $"Missing CLR event `{expectedValues.Name}`.");

				// Visibility?
				Assert.IsTrue(clrEventEventInfo!.AddMethod!.Attributes.HasFlag(expectedValues.Visibility));

				// Type?
				Assert.AreEqual(expectedValues.HandlerType, clrEventEventInfo!.EventHandlerType);
			}
		}

		public class RoutedEventValues
		{
			public Type OwnerType { get; init; } = null!;

			public string Name { get; init; } = "";

			public Type HandlerType { get; init; } = typeof(RoutedEventHandler);

			public RoutingStrategy RoutingStrategy { get; init; } = RoutingStrategy.Direct;

			public bool IsAttached { get; init; }

			public Type? AttachmentNarrowingType
			{
				get => this._attachmentNarrowingType;
				init
				{
					this.IsAttached = true;
					this._attachmentNarrowingType = value;
				}
			}
			private Type? _attachmentNarrowingType;

			public MethodAttributes Visibility { get; init; } = MethodAttributes.Public;

			public override string ToString() => $"{this.OwnerType.Name}.{this.Name}";
		}
	}
}
