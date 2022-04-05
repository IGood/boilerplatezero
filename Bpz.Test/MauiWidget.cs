// Copyright © Ian Good

using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;

namespace Bpz.Test;

public partial class MauiWidget : BindableObject
{
	public static readonly BindableProperty MyBool0Property = Gen.MyBool0(true);
	public static readonly BindableProperty MyBool1Property = Gen.MyBool1<bool>();
	public static readonly BindableProperty MyBool2Property = Gen.MyBool2((bool?)false);

	/// <summary>
	/// Test dox! Gets or sets a string value or something.
	/// </summary>
	public static readonly BindableProperty MyString0Property = Gen.MyString0("asdf");
	public static readonly BindableProperty MyString1Property = Gen.MyString1<string?>();
	public static readonly BindableProperty MyString2Property = Gen.MyString2(default(string?));
	public static readonly BindableProperty MyString3Property = Gen.MyString3("qwer");

	public static readonly BindableProperty MyDictionaryProperty = Gen.MyDictionary<Dictionary<int, List<string>>?>();

	private static void MyString0PropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		((Widget)bindable).MyString0Changed?.Invoke(bindable, EventArgs.Empty);
	}

	private static void MyString1PropertyChanged(Widget self, object oldValue, object newValue)
	{
		self.MyString1Changed?.Invoke(self, EventArgs.Empty);
	}

	private void OnMyString2Changed(string? oldValue, string? newValue)
	{
		this.MyString2Changed?.Invoke(this, EventArgs.Empty);
	}

	private void OnMyString3Changed(string oldValue, string newValue)
	{
		this.MyString3Changed?.Invoke(this, EventArgs.Empty);
	}

	public event EventHandler? MyString0Changed;
	public event EventHandler? MyString1Changed;
	public event EventHandler? MyString2Changed;
	public event EventHandler? MyString3Changed;

	private static readonly BindablePropertyKey MyFloat0PropertyKey = Gen.MyFloat0(3.14f);
	protected static readonly BindablePropertyKey MyFloat1PropertyKey = Gen.MyFloat1<float>();

	private static float CoerceMyFloat1(Widget self, float baseValue)
	{
		return Math.Abs(baseValue);
	}

	private void OnMyFloat1Changed(float old, float @new)
	{
		this.MyFloat1Changed?.Invoke(this, EventArgs.Empty);
	}

	public void SetMyFloat1(float value) => this.MyFloat1 = value;

	public event EventHandler? MyFloat1Changed;

	private static readonly BindablePropertyKey MyNinjaPropertyKey = Gen.MyNinja<NinjaTurtle>();
	protected static readonly BindableProperty MyNinjaProperty = MyNinjaPropertyKey.BindableProperty;

	protected enum NinjaTurtle
	{
		Leo,
		Raph,
		Mike,
		Don,
	}
}
