// Copyright © Ian Good

using NUnit.Framework;
using System.Threading;
using System.Windows;

namespace Bpz.Test
{
	/// <summary>
	/// Checks behavior of generic base types and raising routed events from generated change-handlers.
	/// </summary>
	public class NumericUpDownsTests
	{
		[Test]
		public void ExpectMetadata()
		{
			RoutedEventAssert.Matches(new()
			{
				OwnerType = typeof(NumericUpDownBase<double>),
				Name = "ValueChanged",
				HandlerType = typeof(RoutedPropertyChangedEventHandler<double>),
			});

			DependencyPropertyAssert.Matches(new()
			{
				OwnerType = typeof(NumericUpDownBase<double>),
				Name = "Value",
				PropertyType = typeof(double),
			});
		}

		[Test]
		[Apartment(ApartmentState.STA)]
		public void ExpectEventHandlers()
		{
			var dud = new DoubleUpDown();

			bool eventWasRaised;
			dud.ValueChanged += (s, e) =>
			{
				Assert.AreSame(dud, s);
				eventWasRaised = true;
				e.Handled = true;
			};

			{
				eventWasRaised = false;
				dud.Value = 867.5309;
				Assert.IsTrue(eventWasRaised);
			}
		}
	}
}
