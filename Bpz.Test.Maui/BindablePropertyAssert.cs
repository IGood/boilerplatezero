// Copyright © Ian Good

using System;
using System.Reflection;
using System.Windows;
using Xunit;

namespace Bpz.Test.Maui;

public static class BindablePropertyAssert
{
	public static void Matches(BindablePropertyValues expectedValues)
	{
		string expectedBpFieldName = expectedValues.Name + "Property";
		string expectedBpkFieldName = expectedValues.Name + "PropertyKey";

		FieldInfo? bpFieldInfo = expectedValues.OwnerType.GetField(expectedBpFieldName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		Assert.NotNull(bpFieldInfo);

		// Check bindable property instance.
		BindableProperty? bindableProperty = bpFieldInfo!.GetValue(null) as BindableProperty;
		Assert.NotNull(bindableProperty);

		Assert.Equal(expectedValues.OwnerType, bindableProperty!.DeclaringType);
		Assert.Equal(expectedValues.Name, bindableProperty.PropertyName);
		Assert.Equal(expectedValues.PropertyType, bindableProperty.ReturnType);
		Assert.Equal(expectedValues.IsReadOnly, bindableProperty.IsReadOnly);
		Assert.Equal(expectedValues.DefaultBindingMode, bindableProperty.DefaultBindingMode);

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

			Assert.Equal(expectedDefaultValue, bindableProperty.DefaultValue);
		}

		if (expectedValues.IsReadOnly)
		{
			FieldInfo? bpkFieldInfo = expectedValues.OwnerType.GetField(expectedBpkFieldName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			Assert.NotNull(bpkFieldInfo);

			// Check bindable property key instance.
			BindablePropertyKey? dependencyPropertyKey = bpkFieldInfo!.GetValue(null) as BindablePropertyKey;
			Assert.NotNull(bindableProperty);

			Assert.Same(bindableProperty, dependencyPropertyKey!.BindableProperty);
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
				Assert.NotNull(methodInfo);

				// Visibility?
				Assert.True(methodInfo!.Attributes.HasFlag(attributes));

				// Parameters?
				ParameterInfo[] parameters = methodInfo.GetParameters();

				bool isSetter = name.StartsWith("Set");
				Assert.Equal(isSetter ? 2 : 1, parameters.Length);

				Type expectedTargetType = expectedValues.AttachmentNarrowingType ?? typeof(BindableObject);
				Assert.Equal(expectedTargetType, parameters[0].ParameterType);

				if (isSetter)
				{
					Assert.Equal(expectedValues.PropertyType, parameters[1].ParameterType);
				}
			}
		}
		else
		{
			// Check generated CLR property.
			PropertyInfo? clrPropPropInfo = expectedValues.OwnerType.GetProperty(expectedValues.Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

			// Exists?
			Assert.NotNull(clrPropPropInfo);

			// Type?
			Assert.Equal(expectedValues.PropertyType, clrPropPropInfo!.PropertyType);

			// Accessors visibility?
			Assert.True(clrPropPropInfo.GetMethod!.Attributes.HasFlag(expectedValues.GetterAttributes));
			Assert.True(clrPropPropInfo.SetMethod!.Attributes.HasFlag(expectedValues.SetterAttributes));
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
