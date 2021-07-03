// Copyright © Ian Good

using NUnit.Framework;
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
		[Test(Description = "Checks default values.")]
		public void ExpectDefaults()
		{
			var c = new Canvas();

			Assert.AreEqual(false, GridSnap.GetIsEnabled(c));
			Assert.AreEqual(100, GridSnap.GetCellSize(c));
		}

		[Test(Description = "Change-handers should raise events.")]
		public void ExpectEvents()
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
