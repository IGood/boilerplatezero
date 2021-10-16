// Copyright © Ian Good

using NUnit.Framework;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;

namespace Bpz.Test
{
	/// <summary>
	/// Exercises basic attached event behavior.
	/// </summary>
	public class MyServiceTests
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
					OwnerType = typeof(MyService),
					Name = "ThingUpdated",
					IsAttached = true,
				};

				yield return new()
				{
					OwnerType = typeof(MyService),
					Name = "FooChanged",
					HandlerType = typeof(RoutedPropertyChangedEventHandler<int>),
					RoutingStrategy = RoutingStrategy.Bubble,
					IsAttached = true,
				};

				yield return new()
				{
					OwnerType = typeof(MyService),
					Name = "BarChanged",
					HandlerType = typeof(RoutedPropertyChangedEventHandler<List<float>>),
					IsAttached = true,
				};

				yield return new()
				{
					OwnerType = typeof(MyService),
					Name = "Inspected",
					AttachmentNarrowingType = typeof(System.Windows.Controls.Canvas),
				};

				yield return new()
				{
					OwnerType = typeof(MyService),
					Name = "SomeToolTip",
					HandlerType = typeof(System.Windows.Controls.ToolTipEventHandler),
					IsAttached = true,
				};

				yield return new()
				{
					OwnerType = typeof(MyService),
					Name = "DataTransfer",
					HandlerType = typeof(System.EventHandler<System.Windows.Data.DataTransferEventArgs>),
					IsAttached = true,
				};

				yield return new()
				{
					OwnerType = typeof(MyService),
					Name = "SomethingPrivateHappened",
					IsAttached = true,
					Visibility = MethodAttributes.Private,
				};
			}
		}
	}
}
