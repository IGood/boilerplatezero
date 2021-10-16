// Copyright © Ian Good

using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Bpz.Test
{
	/// <summary>
	/// Exercises basic dependency property behavior.
	/// This won't compile if the properties we expect weren't generated.
	/// </summary>
	public class WidgetTests
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
					OwnerType = typeof(Widget),
					Name = "MyBool0",
					PropertyType = typeof(bool),
					DefaultValue = true,
				};

				yield return new()
				{
					OwnerType = typeof(Widget),
					Name = "MyBool1",
					PropertyType = typeof(bool),
				};

				yield return new()
				{
					OwnerType = typeof(Widget),
					Name = "MyBool2",
					PropertyType = typeof(bool?),
					DefaultValue = false,
				};

				yield return new()
				{
					OwnerType = typeof(Widget),
					Name = "MyString0",
					PropertyType = typeof(string),
					DefaultValue = "asdf",
				};

				yield return new()
				{
					OwnerType = typeof(Widget),
					Name = "MyString1",
					PropertyType = typeof(string),
				};

				yield return new()
				{
					OwnerType = typeof(Widget),
					Name = "MyString2",
					PropertyType = typeof(string),
					DefaultValue = null,
				};

				yield return new()
				{
					OwnerType = typeof(Widget),
					Name = "MyString3",
					PropertyType = typeof(string),
					DefaultValue = "qwer",
				};

				yield return new()
				{
					OwnerType = typeof(Widget),
					Name = "MyDictionary",
					PropertyType = typeof(Dictionary<int, List<string>>),
					DefaultValue = null,
				};

				yield return new()
				{
					OwnerType = typeof(Widget),
					Name = "MyFloat0",
					PropertyType = typeof(float),
					DefaultValue = 3.14f,
					IsReadOnly = true,
					SetterAttributes = MethodAttributes.Private,
				};

				yield return new()
				{
					OwnerType = typeof(Widget),
					Name = "MyFloat1",
					PropertyType = typeof(float),
					IsReadOnly = true,
					SetterAttributes = MethodAttributes.Family,
				};

				yield return new()
				{
					OwnerType = typeof(Widget),
					Name = "MyNinja",
					PropertyType = typeof(Widget).GetNestedType("NinjaTurtle", BindingFlags.NonPublic)!,
					IsReadOnly = true,
					GetterAttributes = MethodAttributes.Family,
					SetterAttributes = MethodAttributes.Private,
				};
			}
		}

		[Test(Description = "Change-handers should raise events.")]
		public void ExpectEventHandlers()
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
