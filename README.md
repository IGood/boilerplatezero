# boilerplatezero

boilerplatezero (BPZ) is a collection of C# source generators that simplifies the code that needs to be written for common C# patterns.

----

## WPF Dependency Property Generator

Dependency properties in WPF are great! However, they do require quite a bit of ceremony in order to define one.<br>
Luckily, dependency properties (and attached properties) always follow the same pattern when implemented.<br>
As such, this kind of boilerplate code is the perfect candidate for a source generator.

The dependency property generator in BPZ works by identifying `DependencyProperty` and `DependencyPropertyKey` fields that are initialized with calls to appropriately-name `Gen` or `GenAttached` methods.<br>
When this happens, the source generator adds private static classes as nested types inside your class &amp; implements the dependency property for you.

- Jump to: [Generated Code Example](#dpgenerated)
- Jump to: [Features List](#dpfeatures)

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

// Dependency property written with BPZ:
private static readonly DependencyPropertyKey FooPropertyKey = Gen.Foo<string>();
```

### 🛠 Example Attached Property

```csharp
// Attached property written idiomatically:
private static readonly DependencyPropertyKey BarPropertyKey = DependencyProperty.RegisterAttachedReadOnly("Bar"), typeof(string), typeof(MyClass), null);
public static readonly DependencyProperty BarProperty = BarPropertyKey.DependencyProperty;
public static string GetBar(DependencyObject d) => (string)d.GetValue(BarProperty);
private static void SetBar(DependencyObject d, string value) => d.SetValue(BarPropertyKey, value);

// Attached property written with BPZ:
private static readonly DependencyPropertyKey BarPropertyKey = GenAttached.Bar<string>();
```

### <a name="dpgenerated"></a>🛠 Generated Code

Here's an example of hand-written code &amp; the corresponding generated code.<br>
Note that the default value for the dependency property is specified in the user's code &amp; the documentation comment on `IdPropertyKey` is copied to the generated `Id` property.

```csharp
// 👩‍💻 Widget.cs
namespace Goodies
{
    public partial class Widget : DependencyObject
    {
        /// <Summary>Gets the ID of this instance.</Summary>
        protected static readonly DependencyPropertyKey IdPropertyKey = Gen.Id("<unset>");
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
            public static DependencyPropertyKey Id<T>(T defaultValue)
            {
                PropertyMetadata metadata = new PropertyMetadata(defaultValue);
                return DependencyProperty.RegisterReadOnly("Id", typeof(T), typeof(Widget), metadata);
            }
        }
    }
}
```

### <a name="dpfeatures"></a>✨ Features 

- generates instance properties for dependency properties
- generates static methods for attached properties
- optional default values
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
