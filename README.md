# ![Logo](product/bpz%20logo%20dark.png) boilerplatezero (BPZ)

[![NuGet version (boilerplatezero)](https://img.shields.io/nuget/v/boilerplatezero.svg?style=flat-square)](https://www.nuget.org/packages/boilerplatezero/)
[![MIT License](https://img.shields.io/badge/license-MIT-green.svg?style=flat-square)](/LICENSE)

boilerplatezero (BPZ) is a collection of C# source generators that simplify the code required for common C# patterns.

----

## WPF Dependency Property Generator

Dependency properties in WPF are great! However, they do require quite a bit of ceremony in order to define one.<br>
Luckily, dependency properties (and attached properties) always follow the same pattern when implemented.<br>
As such, this kind of boilerplate code is the perfect candidate for a source generator.

The dependency property generator in BPZ works by identifying `DependencyProperty` and `DependencyPropertyKey` fields that are initialized with calls to appropriately-named `Gen` or `GenAttached` methods.<br>
When this happens, the source generator adds private static classes as nested types inside your class &amp; implements the dependency property for you.<br>
Additionally...
- If an appropriate property-changed handler method is found, then it will be used during registration.
- If an appropriate coercion method is found, then it will be used during registration.

🔗 Jump to
- [🤖 Generated Code Example](#dpgenerated)
- [✨ Features List](#dpfeatures)
- [🐛 Known Issues List](#-known-issues)

### 🛠 Example Dependency Property

```csharp
// Dependency property written idiomatically:
private static readonly DependencyPropertyKey FooPropertyKey = DependencyProperty.RegisterReadOnly(nameof(Foo), typeof(string), typeof(MyClass), null);
public static readonly DependencyProperty FooProperty = FooPropertyKey.DependencyProperty;
public string Foo
{
    get => (string)this.GetValue(FooProperty);
    private set => this.SetValue(FooPropertyKey, value);
}

// Dependency property written with BPZ (new hotness):
private static readonly DependencyPropertyKey FooPropertyKey = Gen.Foo<string>();
```

### 🛠 Example Attached Property

```csharp
// Attached property written idiomatically:
private static readonly DependencyPropertyKey BarPropertyKey = DependencyProperty.RegisterAttachedReadOnly("Bar"), typeof(string), typeof(MyClass), null);
public static readonly DependencyProperty BarProperty = BarPropertyKey.DependencyProperty;
public static string GetBar(DependencyObject d) => (string)d.GetValue(BarProperty);
private static void SetBar(DependencyObject d, string value) => d.SetValue(BarPropertyKey, value);

// Attached property written with BPZ (new hotness):
private static readonly DependencyPropertyKey BarPropertyKey = GenAttached.Bar<string>();
```

### <a name="dpgenerated"></a>🤖 Generated Code

Here's an example of hand-written code &amp; the corresponding generated code.<br>
Note that the default value for the dependency property is specified in the user's code &amp; included in the property metadata along with the appropriate property-changed handler.<br>
The documentation comment on `IdPropertyKey` is copied to the generated `Id` property.

```csharp
// 👩‍💻 Widget.cs
namespace Goodies
{
    public partial class Widget : DependencyObject
    {
        /// <Summary>Gets the ID of this instance.</Summary>
        protected static readonly DependencyPropertyKey IdPropertyKey = Gen.Id("<unset>");

        private static void IdPropertyChanged(Widget self, DependencyPropertyChangedEventArgs e)
        {
            // This method will be used as the property-changed callback during registration!
            // It was selected because...
            // - its name contains "Id" & ends with "Changed"
            // - it is `static` with return type `void`
            // - the type of parameter 0 is compatible with the owner type
            // - the type of parameter 1 is DependencyPropertyChangedEventArgs
        }
    }
}

// 🤖 Widget.g.cs (actual name may vary)
namespace Goodies
{
    partial class Widget
    {
        public static readonly DependencyProperty IdProperty = IdPropertyKey.DependencyProperty;

        /// <Summary>Gets the ID of this instance.</Summary>
        public string Id
        {
            get => (string)this.GetValue(IdProperty);
            protected set => this.SetValue(IdPropertyKey, value);
        }

        private static partial class Gen
        {
            public static DependencyPropertyKey Id<__T>(__T defaultValue)
            {
                PropertyMetadata metadata = new PropertyMetadata(defaultValue, (d, e) => IdPropertyChanged((Goodies.Widget)d, e), null);
                return DependencyProperty.RegisterReadOnly("Id", typeof(__T), typeof(Widget), metadata);
            }
        }
    }
}
```

### <a name="dpfeatures"></a>✨ Features 

- generates instance properties for dependency properties
- generates static methods for attached properties
- optional default values
- <details><summary>detects suitable property-changed handlers</summary>

  ```csharp
  // 👩‍💻 user
  public static readonly DependencyProperty SeasonProperty = Gen.Season("autumn");
  private static void SeasonPropertyChanged(Widget self, DependencyPropertyChangedEventArgs e)
  {
      // This method will be used as the property-changed callback during registration!
      // It was selected because...
      // - its name contains "Season" & ends with "Changed"
      // - it is `static` with return type `void`
      // - the type of parameter 0 is compatible with the owner type
      // - the type of parameter 1 is DependencyPropertyChangedEventArgs
  }
  ```
  <details>
- <details><summary>detects suitable value coercion handlers</summary>

  ```csharp
  // 👩‍💻 user
  public static readonly DependencyProperty AgeProperty = Gen.Age(0);
  private static int CoerceAge(Widget self, int baseValue)
  {
      // This method will be used as the value coercion method during registration!
      // It was selected because...
      // - its name is "Coerce" + the property name
      // - it is `static`
      // - the return type is compatible with the property type
      // - the type of parameter 0 is compatible with the owner type
      // - the type of parameter 1 is compatible with the property type
      return (baseValue >= 0) ? baseValue : 0;
  }
  ```
  <details>
- supports generic owner types
- <details><summary>supports nullable types</summary>

  ```csharp
  public static readonly DependencyProperty IsCheckedProperty = Gen.IsChecked<bool?>(false);
  public static readonly DependencyProperty NameProperty = Gen.Name<string?>();
  ```
  </details>
- access modifiers are preserved on generated members
- documentation is preserved (for dependency properties) on generated members
- <details><summary>generates dependency property fields for read-only properties (if necessary)</summary>

  ```csharp
  // 👩‍💻 user
  // Instance field `FooProperty` is defined, so it will not be generated.
  // Access modifiers for generated get/set of the `Foo` instance property will match the property & key.
  private static readonly DependencyPropertyKey FooPropertyKey = GenAttached.Foo(3.14f);
  protected static readonly DependencyProperty FooProperty = FooPropertyKey.DependencyProperty;

  // Instance field `BarProperty` is not defined, so it will be generated.
  private static readonly DependencyPropertyKey BarPropertyKey = Gen.Bar<Guid>();

  // 🤖 generated
  protected float Foo
  {
      get => (float)this.GetValue(FooProperty);
      private set => this.SetValue(FooPropertyKey, value);
  }

  public static readonly DependencyProperty BarProperty = BarPropertyKey.DependencyProperty;
  public System.Guid Bar
  {
      get => (System.Guid)this.GetValue(BarProperty);
      private set => this.SetValue(BarPropertyKey, value);
  }
  ```
  </details>
- <details><summary>attached properties may constrain their target type by specifying a generic type parameter on <code>GenAttached</code></summary>

  ```csharp
  // 👩‍💻 user
  // Attached property `Standard` may be used with any dependency object.
  public static readonly DependencyProperty StandardProperty = GenAttached.Standard("🍕");

  // Attached property `IsFancy` may only be used with objects of type <see cref="Widget"/>.
  public static readonly DependencyProperty IsFancyProperty = GenAttached<Goodies.Widget>.IsFancy(true);

  // 🤖 generated
  public static string GetStandard(DependencyObject d) => (string)d.GetValue(StandardProperty);
  public static void SetStandard(DependencyObject d, string value) => d.SetValue(StandardProperty, value);

  public static bool GetIsFancy(Goodies.Widget d) => (bool)d.GetValue(IsFancyProperty);
  public static void SetIsFancy(Goodies.Widget d, bool value) => d.SetValue(IsFancyProperty, value);
  ```
  </details>

----

### 🐛 Known Issues

related: Source Generator support for WPF project blocked? [#3404](https://github.com/dotnet/wpf/issues/3404)
