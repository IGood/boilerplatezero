// Copyright © Ian Good

using NUnit.Framework;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace Bpz.Test
{
	/// <summary>
	/// Exercises basic attached property behavior.
	/// This won't compile if the properties we expect weren't generated.
	/// </summary>
	[Apartment(ApartmentState.STA)]
	public class GridSnapTests
	{
		[TestCaseSource(nameof(MetadataTestCases))]
		public void ExpectMetadata(DependencyPropertyAssert.DependencyPropertyValues testCase)
		{
			DependencyPropertyAssert.Matches(testCase);
		}

		public static IEnumerable<DependencyPropertyAssert.DependencyPropertyValues> MetadataTestCases
		{
			get
			{
				yield return new()
				{
					OwnerType = typeof(GridSnap),
					Name = "IsEnabled",
					PropertyType = typeof(bool),
					DefaultValue = false,
					AttachmentNarrowingType = typeof(UIElement),
					Flags = FrameworkPropertyMetadataOptions.Inherits,
				};

				yield return new()
				{
					OwnerType = typeof(GridSnap),
					Name = "CellSize",
					PropertyType = typeof(double),
					DefaultValue = 100.0,
					AttachmentNarrowingType = typeof(Canvas),
					Flags = FrameworkPropertyMetadataOptions.Inherits,
				};

			}
		}

		[Test(Description = "Change-handers should raise events.")]
		public void ExpectEventHandlers()
		{
			var c = new Canvas();

			bool eventWasRaised;
			RoutedPropertyChangedEventHandler<bool> handler = (s, e) =>
			{
				Assert.AreSame(c, s);
				eventWasRaised = true;
			};

			c.AddHandler(GridSnap.IsEnabledChangedEvent, handler);

			{
				eventWasRaised = false;
				GridSnap.SetIsEnabled(c, true);
				Assert.IsTrue(eventWasRaised);
			}
		}
	}
}
