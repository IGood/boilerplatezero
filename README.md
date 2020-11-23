# boilerplatezero
boilerplatezero is a collection of C# source generators

# WPF Generators
## Dependency Property Generator
### Dependency Properties
```csharp
// Dependency property written idiomatically:
private static readonly DependencyPropertyKey FooPropertyKey = DependencyProperty.RegisterReadOnly(nameof(Foo), typeof(string), typeof(MyClass), null);
public static readonly DependencyProperty FooProperty = FooPropertyKey.DependencyPropety;
public string Foo
{
    get => (string)this.GetValue(FooProperty);
    private set => this.SetValue(FooPropertyKey, value);
}

// Dependency property written with BPZ:
private static readonly DependencyPropertyKey FooPropertyKey = Gen.Foo<string>();
```
### Attached Properties
```csharp
// Attached property written idiomatically:
private static readonly DependencyPropertyKey BarPropertyKey = DependencyProperty.RegisterAttachedReadOnly("Bar"), typeof(string), typeof(MyClass), null);
public static readonly DependencyProperty BarProperty = BarPropertyKey.DependencyPropety;
public static string GetBar(DependencyObject d) => (string)d.GetValue(BarProperty);
private static void SetBar(DependencyObject d, string value) => d.SetValue(BarPropertyKey, value);

// Attached property written with BPZ:
private static readonly DependencyPropertyKey FooPropertyKey = GenAttached.Bar<string>();
```
