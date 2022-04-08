// Copyright © Ian Good

using NUnit.Framework;
using System;
using System.Reflection;
using System.Windows;

namespace Bpz.Test.Maui;

public static class BindablePropertyAssert
{
	public static void Matches(BindablePropertyValues expectedValues)
	{
		string expectedBpFieldName = expectedValues.Name + "Property";
		string expectedBpkFieldName = expectedValues.Name + "PropertyKey";

		FieldInfo? bpFieldInfo = expectedValues.OwnerType.GetField(expectedBpFieldName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		Assert.IsNotNull(bpFieldInfo, $"Missing static bindable property field `{expectedBpFieldName}`.");

		// Check bindable property instance.
		BindableProperty? bindableProperty = bpFieldInfo!.GetValue(null) as BindableProperty;
		Assert.IsNotNull(bindableProperty);

		Assert.AreEqual(expectedValues.OwnerType, bindableProperty!.DeclaringType);
		Assert.AreEqual(expectedValues.Name, bindableProperty.PropertyName);
		Assert.AreEqual(expectedValues.PropertyType, bindableProperty.ReturnType);
		Assert.AreEqual(expectedValues.IsReadOnly, bindableProperty.IsReadOnly);
		Assert.AreEqual(expectedValues.DefaultBindingMode, bindableProperty.DefaultBindingMode);

		// Default value?
		{
			object? expectedDefaultValue;
			if (expectedValues.HasExplicitDefaultValue)
			{
				expectedDefaultValue = expectedValues.DefaultValue;
			}
			else
			{
				expectedDefaultValue = bindableProperty.ReturnType.IsValueType ? Activator.CreateInstance(bindableProperty.ReturnType) : null;
			}

			Assert.AreEqual(expectedDefaultValue, bindableProperty.DefaultValue);
		}

		if (expectedValues.IsReadOnly)
		{
			FieldInfo? bpkFieldInfo = expectedValues.OwnerType.GetField(expectedBpkFieldName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			Assert.IsNotNull(bpkFieldInfo, $"Missing static bindable property key field `{expectedBpkFieldName}`.");

			// Check bindable property key instance.
			BindablePropertyKey? dependencyPropertyKey = bpkFieldInfo!.GetValue(null) as BindablePropertyKey;
			Assert.IsNotNull(bindableProperty);

			Assert.AreSame(bindableProperty, dependencyPropertyKey!.BindableProperty);
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

				Type expectedTargetType = expectedValues.AttachmentNarrowingType ?? typeof(BindableObject);
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

	public class BindablePropertyValues
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

		public BindingMode DefaultBindingMode { get; set; }

		public override string ToString() => $"{this.OwnerType.Name}.{this.Name}";
	}
}
