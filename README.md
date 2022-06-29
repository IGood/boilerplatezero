# ![Logo](product/bpz%20logo%20dark.png) boilerplatezero (BPZ)

[![NuGet version (boilerplatezero)](https://img.shields.io/nuget/v/boilerplatezero.svg?style=flat-square)](https://www.nuget.org/packages/boilerplatezero/)
[![MIT License](https://img.shields.io/badge/license-MIT-green.svg?style=flat-square)](/LICENSE)

boilerplatezero (BPZ) is a collection of C# source generators that simplify the code required for common C# patterns.

🔗 Jump to...
- [WPF Dependency Property Generator](#wpf-dependency-property-generator)
- [WPF Routed Event Generator](#wpf-routed-event-generator)
- [🐛 Known Issues List](#-known-issues)

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
- If an appropriate validation method is found, then it will be used during registration.

🔗 Jump to...
- [👩‍💻 Write This, Not That](#-write-this-not-that-property-examples)
- [🤖 Generated Code Example](#-generated-code)
- [✨ Features List](#-features)

----

### 👩‍💻 Write This, Not That: Property Examples

#### 🔧 Dependency Property

```csharp
// Write this (using BPZ):
private static readonly DependencyPropertyKey FooPropertyKey = Gen.Foo<string>();

// Not that (idiomatic implementation):
private static readonly DependencyPropertyKey FooPropertyKey = DependencyProperty.RegisterReadOnly(nameof(Foo), typeof(string), typeof(MyClass), null);
public static readonly DependencyProperty FooProperty = FooPropertyKey.DependencyProperty;
public string Foo
{
    get => (string)this.GetValue(FooProperty);
    private set => this.SetValue(FooPropertyKey, value);
}
```

<details><summary>A more complex example...</summary>

```csharp
// Write this (using BPZ):
public static readonly DependencyProperty TextProperty = Gen.Text("", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault);
protected virtual void OnTextChanged(string oldText, string newText) { ... }

// Not that (idiomatic implementation):
public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
    nameof(Text), typeof(string), typeof(MyClass),
    new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, TextPropertyChanged));
public string Text
{
    get => (string)this.GetValue(TextProperty);
    set => this.SetValue(TextProperty, value);
}
private static void TextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
{
    ((MyClass)d).OnTextChanged((string)e.OldValue, (string)e.NewValue);
}
protected virtual void OnTextChanged(string oldText, string newText) { ... }
```
</details>

#### 🔧 Attached Property

```csharp
// Write this (using BPZ):
private static readonly DependencyPropertyKey BarPropertyKey = GenAttached.Bar<string>();

// Not that (idiomatic implementation):
private static readonly DependencyPropertyKey BarPropertyKey = DependencyProperty.RegisterAttachedReadOnly("Bar", typeof(string), typeof(MyClass), null);
public static readonly DependencyProperty BarProperty = BarPropertyKey.DependencyProperty;
public static string GetBar(DependencyObject d) => (string)d.GetValue(BarProperty);
private static void SetBar(DependencyObject d, string value) => d.SetValue(BarPropertyKey, value);
```

----

### 🤖 Generated Code

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
            // - its name contains the property name, "Id", & ends with "Changed"
            // - it is `static` with return type `void`
            // - the type of parameter 0 is compatible with the owner type
            // - the type of parameter 1 is `DependencyPropertyChangedEventArgs`
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
                PropertyMetadata metadata = new PropertyMetadata(
                    defaultValue,
                    (d, e) => IdPropertyChanged((Goodies.Widget)d, e),
                    null);
                return DependencyProperty.RegisterReadOnly("Id", typeof(__T), typeof(Widget), metadata);
            }
        }
    }
}
```

----

### ✨ Features

- generates instance properties for dependency properties
- generates static methods for attached properties
- optional default values
- <details><summary>optional <code>FrameworkPropertyMetadataOptions</code> flags</summary>
  A <code>flags</code> argument may be specified for the property's <code>FrameworkPropertyMetadata</code>.

  ```csharp
  // 👩‍💻 user
  public static readonly DependencyProperty TextProperty = Gen.Text<string?>(FrameworkPropertyMetadataOptions.BindsTwoWayByDefault);
  public static readonly DependencyProperty ErrorBrushProperty = GenAttached.ErrorBrush(Brushes.Red, FrameworkPropertyMetadataOptions.Inherits);
  ```
  </details>
- <details><summary>detects suitable property-changed handlers</summary>
  There are 4 options for property-changed handlers.
  If multiple candidates are found, then they are prioritized (from highest to lowest) as shown below (static methods, instance methods, routed events).

  ```csharp
  // 👩‍💻 user
  public static readonly DependencyProperty SeasonProperty = Gen.Season("autumn");

  // Option 1 - static method, named "*Season*Changed"
  private static void SeasonPropertyChanged(Widget self, DependencyPropertyChangedEventArgs e)
  {
      // This method can be used as the property-changed callback during registration!
      // It is a candidate because...
      // - its name contains the property name, "Season", & ends with "Changed"
      // - it is `static`
      // - return type is `void`
      // - type of parameter 0 is compatible with the owner type
      // - type of parameter 1 is `DependencyPropertyChangedEventArgs`
  }

  // Option 2 - instance method, named "[On]SeasonChanged", 2 parameters
  protected virtual void OnSeasonChanged(string oldSeason, string newSeason)
  {
      // This method can be used as the property-changed callback during registration!
      // It is a candidate because...
      // - its name is "OnSeasonChanged" ("SeasonChanged" is also acceptable)
      // - it is not `static`
      // - return type is `void`
      // - types of parameter 0 & 1 match the property type
      // - names of parameter 0 & 1 start with "old" & "new" (respectively)
  }

  // Option 3 - instance method, named "[On]SeasonChanged", 1 parameter
  protected virtual void OnSeasonChanged(DependencyPropertyChangedEventArgs e)
  {
      // This method can be used as the property-changed callback during registration!
      // It is a candidate because...
      // - its name is "OnSeasonChanged" ("SeasonChanged" is also acceptable)
      // - it is not `static`
      // - return type is `void`
      // - type of parameter 0 is `DependencyPropertyChangedEventArgs`
  }

  // Option 4 - routed event, named "SeasonChangedEvent"
  public static readonly RoutedEvent SeasonChangedEvent = Gen.SeasonChanged<string>();
      // Invoking this event can be used as the property-changed callback during registration!
      // It is a candidate because...
      // - it is a `static readonly RoutedEvent`
      // - it's name is "SeasonChangedEvent"
  ```
  </details>
- <details><summary>detects suitable value coercion handlers</summary>

  ```csharp
  // 👩‍💻 user
  public static readonly DependencyProperty AgeProperty = Gen.Age(0);
  private static int CoerceAge(Widget self, int baseValue)
  {
      // This method will be used as the value coercion method during registration!
      // It was selected because...
      // - its name is "CoerceAge" (i.e. "Coerce" + the property name)
      // - it is `static`
      // - return type is `object` or matches the property type
      // - type of parameter 0 is compatible with the owner type
      // - type of parameter 1 is `object` or matches the property type
      return (baseValue >= 0) ? baseValue : 0;
  }
  ```
  </details>
- <details><summary>detects suitable value validation handlers</summary>

  ```csharp
  // 👩‍💻 user
  public static readonly DependencyProperty WrapModeProperty = Gen.WrapMode(TextWrapping.NoWrap);
  private static bool IsValidWrapMode(TextWrapping value)
  {
      // This method will be used as the value validation method during registration!
      // It was selected because...
      // - its name is "IsValidWrapMode" (i.e. "IsValid" + the property name)
      // - it is `static`
      // - return type is `bool`
      // - type of parameter 0 is `object` or matches the property type
      return Enum.IsDefined(value);
  }
  ```
  </details>
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
- <details><summary>attached properties may constrain their target type by specifying a generic type argument on <code>GenAttached</code></summary>

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

## WPF Routed Event Generator

Similar to dependency properties, routed events always use the same pattern when implemented correctly.

The routed event generator in BPZ works by identifying `RoutedEvent` fields that are initialized with calls to appropriately-named `Gen` or `GenAttached` methods.<br>
When this happens, the source generator adds private static classes as nested types inside your class &amp; implements the routed event for you.

🔗 Jump to...
- [👩‍💻 Write This, Not That](#-write-this-not-that-event-examples)
- [✨ Features List](#-features-1)

----

### 👩‍💻 Write This, Not That: Event Examples

#### ⚡ Routed Event

```csharp
// Write this (using BPZ):
public static readonly RoutedEvent FooChangedEvent = Gen.FooChanged<string>();

// Not that (idiomatic implementation):
public static readonly RoutedEvent FooChangedEvent = EventManager.RegisterRoutedEvent(nameof(FooChanged), RoutingStrategy.Direct, typeof(RoutedPropertyChangedEventHandler<string>), typeof(MyClass));
public event RoutedPropertyChangedEventHandler<string> FooChanged
{
    add => this.AddHandler(FooChangedEvent, value);
    remove => this.RemoveHandler(FooChangedEvent, value);
}
```

#### ⚡ Attached Event

```csharp
// Write this (using BPZ):
public static readonly RoutedEvent ThingUpdatedEvent = GenAttached.ThingUpdatedChanged(RoutingStrategy.Bubble);

// Not that (idiomatic implementation):
public static readonly RoutedEvent ThingUpdatedEvent = EventManager.RegisterRoutedEvent(nameof(BarChanged), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(MyClass));
public static void AddThingUpdatedHandler(DependencyObject d, RoutedEventHandler handler) => (d as UIElement)?.AddHandler(BarChangedEvent, handler);
public static void RemoveThingUpdatedHandler(DependencyObject d, RoutedEventHandler handler) => (d as UIElement)?.RemoveHandler(BarChangedEvent, handler);
```

----

### ✨ Features

- generates instance events for routed events
- generates static methods for attached events
- handler type may be specified via generic type argument on the generator method (e.g. `Gen.FooChanged<RoutedPropertyChangedEventHandler<string>()`)
- handler type is optional (default is `RoutedEventHandler`)
- handler types that are **not** delegates (e.g. `Gen.FooChanged<string>()`) are treated as `RoutedPropertyChangedEventHandler<T>` with the specified type
- routing strategy may be specified via generator method argument (e.g. `Gen.ThingUpdated(RoutingStrategy.Bubble)`)
- routing strategy is optional (default is `RoutingStrategy.Direct`)
- supports generic owner types
- access modifiers are preserved on generated members
- documentation is preserved (for routed events) on generated members
- attached events may constrain their target type by specifying a generic type argument on `GenAttached`

----

### 🐛 Known Issues

related: Source Generator support for WPF project blocked? [#3404](https://github.com/dotnet/wpf/issues/3404)
- fix / workaround: use this in project file
  ```xml
  <IncludePackageReferencesDuringMarkupCompilation>true</IncludePackageReferencesDuringMarkupCompilation>
  ```
