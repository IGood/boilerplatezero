// Copyright © Ian Good

using NUnit.Framework;
using System;

namespace Bpz.Test
{
	/// <summary>
	/// Exercises basic dependency property behavior.
	/// This won't compile if the properties we expect weren't generated.
	/// </summary>
	public class WidgetTests
	{
		[Test(Description = "Checks default values.")]
		public void ExpectDefaults()
		{
			var w = new Widget();

			Assert.AreEqual(true, w.MyBool0);
			Assert.AreEqual(false, w.MyBool1);
			Assert.AreEqual(false, w.MyBool2);

			Assert.AreEqual("asdf", w.MyString0);
			Assert.AreEqual(null, w.MyString1);
			Assert.AreEqual(null, w.MyString2);
			Assert.AreEqual("qwer", w.MyString3);

			Assert.AreEqual(3.14f, w.MyFloat0);
			Assert.AreEqual(0, w.MyFloat1);
		}

		[Test(Description = "Change-handers should raise events.")]
		public void ExpectEvents()
		{
			var w = new Widget();

			bool eventWasRaised;
			EventHandler handler = (s, e) =>
			{
				Assert.AreSame(w, s);
				eventWasRaised = true;
			};

			w.MyString0Changed += handler;
			w.MyString1Changed += handler;
			w.MyString2Changed += handler;
			w.MyString3Changed += handler;
			w.MyFloat1Changed += handler;

			{
				eventWasRaised = false;
				w.MyString0 = "foo";
				Assert.IsTrue(eventWasRaised);
			}
			{
				eventWasRaised = false;
				w.MyString1 = "foo";
				Assert.IsTrue(eventWasRaised);
			}
			{
				eventWasRaised = false;
				w.MyString2 = "foo";
				Assert.IsTrue(eventWasRaised);
			}
			{
				eventWasRaised = false;
				w.MyString3 = "foo";
				Assert.IsTrue(eventWasRaised);
			}
			{
				eventWasRaised = false;
				w.SetMyFloat1(123.456f);
				Assert.IsTrue(eventWasRaised);

				eventWasRaised = false;
				w.SetMyFloat1(123.456f);
				Assert.IsFalse(eventWasRaised);
			}
		}

		[Test(Description = "Coercion should take place.")]
		public void ExpectCoercion()
		{
			var w = new Widget();

			bool eventWasRaised = false;
			w.MyFloat1Changed += (s, e) =>
			{
				Assert.AreSame(w, s);
				eventWasRaised = true;
			};

			// `Float1` coerces values using `Math.Abs`.
			w.SetMyFloat1(-40);
			Assert.AreEqual(40, w.MyFloat1);
			Assert.IsTrue(eventWasRaised);

			// The value has been coerced from negative to positive,
			// so assigning the positive value should not produce a "changed" event.
			eventWasRaised = false;
			w.SetMyFloat1(40);
			Assert.AreEqual(40, w.MyFloat1);

			Assert.IsFalse(eventWasRaised);
		}
	}
}
