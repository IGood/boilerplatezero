// Copyright © Ian Good

using NUnit.Framework;
using System;
using System.Reflection;
using System.Windows;

namespace Bpz.Test
{
	public static class DependencyPropertyAssert
	{
		public static void Matches(DependencyPropertyValues expectedValues)
		{
			string expectedDpFieldName = expectedValues.Name + "Property";
			string expectedDpkFieldName = expectedValues.Name + "PropertyKey";

			FieldInfo? dpFieldInfo = expectedValues.OwnerType.GetField(expectedDpFieldName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			Assert.IsNotNull(dpFieldInfo, $"Missing static dependency property field `{expectedDpFieldName}`.");

			// Check dependency property instance.
			DependencyProperty? dependencyProperty = dpFieldInfo!.GetValue(null) as DependencyProperty;
			Assert.IsNotNull(dependencyProperty);

			Assert.AreEqual(expectedValues.OwnerType, dependencyProperty!.OwnerType);
			Assert.AreEqual(expectedValues.Name, dependencyProperty.Name);
			Assert.AreEqual(expectedValues.PropertyType, dependencyProperty.PropertyType);
			Assert.AreEqual(expectedValues.IsReadOnly, dependencyProperty.ReadOnly);

			Type expectedMetadataType = expectedValues.Flags.HasValue ? typeof(FrameworkPropertyMetadata) : typeof(PropertyMetadata);
			Assert.AreEqual(expectedMetadataType, dependencyProperty.DefaultMetadata.GetType());

			// Default value?
			{
				object? expectedDefaultValue;
				if (expectedValues.HasExplicitDefaultValue)
				{
					expectedDefaultValue = expectedValues.DefaultValue;
				}
				else
				{
					expectedDefaultValue = dependencyProperty.PropertyType.IsValueType ? Activator.CreateInstance(dependencyProperty.PropertyType) : null;
				}

				Assert.AreEqual(expectedDefaultValue, dependencyProperty.DefaultMetadata.DefaultValue);
			}

			if (expectedValues.IsReadOnly)
			{
				FieldInfo? dpkFieldInfo = expectedValues.OwnerType.GetField(expectedDpkFieldName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
				Assert.IsNotNull(dpkFieldInfo, $"Missing static dependency property key field `{expectedDpkFieldName}`.");

				// Check dependency property key instance.
				DependencyPropertyKey? dependencyPropertyKey = dpkFieldInfo!.GetValue(null) as DependencyPropertyKey;
				Assert.IsNotNull(dependencyProperty);

				Assert.AreSame(dependencyProperty, dependencyPropertyKey!.DependencyProperty);
			}

			if (expectedValues.IsAttached)
			{
				// Check generated getter/setter methods.
				_AssertAccessor("Get" + expectedValues.Name, expectedValues.GetterAttributes);
				_AssertAccessor("Set" + expectedValues.Name, expectedValues.SetterAttributes);

				void _AssertAccessor(string name, MethodAttributes attributes)
				{
					MethodInfo? methodInfo = expectedValues.OwnerType.GetMethod(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

					// Exists?
					Assert.IsNotNull(methodInfo, $"Missing static method `{name}`.");

					// Visibility?
					Assert.IsTrue(methodInfo!.Attributes.HasFlag(attributes));

					// Parameters?
					ParameterInfo[] parameters = methodInfo.GetParameters();

					bool isSetter = name.StartsWith("Set");
					Assert.AreEqual(isSetter ? 2 : 1, parameters.Length);

					Type expectedTargetType = expectedValues.AttachmentNarrowingType ?? typeof(DependencyObject);
					Assert.AreEqual(expectedTargetType, parameters[0].ParameterType);

					if (isSetter)
					{
						Assert.AreEqual(expectedValues.PropertyType, parameters[1].ParameterType);
					}
				}
			}
			else
			{
				// Check generated CLR property.
				PropertyInfo? clrPropPropInfo = expectedValues.OwnerType.GetProperty(expectedValues.Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

				// Exists?
				Assert.IsNotNull(clrPropPropInfo, $"Missing property `{expectedValues.Name}`.");

				// Type?
				Assert.AreEqual(expectedValues.PropertyType, clrPropPropInfo!.PropertyType);

				// Accessors visibility?
				Assert.IsTrue(clrPropPropInfo.GetMethod!.Attributes.HasFlag(expectedValues.GetterAttributes));
				Assert.IsTrue(clrPropPropInfo.SetMethod!.Attributes.HasFlag(expectedValues.SetterAttributes));
			}
		}

		public class DependencyPropertyValues
		{
			public Type OwnerType { get; init; } = null!;

			public string Name { get; init; } = "";

			public Type PropertyType { get; init; } = null!;

			public bool HasExplicitDefaultValue { get; private init; }

			public object? DefaultValue
			{
				get => this._defaultValue;
				init
				{
					this.HasExplicitDefaultValue = true;
					this._defaultValue = value;
				}
			}
			public object? _defaultValue;

			public bool IsReadOnly { get; init; }

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

			public MethodAttributes GetterAttributes { get; init; } = MethodAttributes.Public;

			public MethodAttributes SetterAttributes { get; init; } = MethodAttributes.Public;

			public FrameworkPropertyMetadataOptions? Flags { get; init; }

			public override string ToString() => $"{this.OwnerType.Name}.{this.Name}";
		}
	}
}
