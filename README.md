### This is an add-in for [Fody](https://github.com/Fody/Fody/) [![NuGet Status](http://img.shields.io/nuget/v/Equatable.Fody.svg?style=flat)](https://www.nuget.org/packages/Equatable.Fody/) ![badge](https://tom-englert.visualstudio.com/_apis/public/build/definitions/75bf84d2-d359-404a-a712-07c9f693f635/16/badge) 

![Icon](Assets/Icon.png)

Generate the Equals, GetHashCode and operators methods from properties or fields explicitly decorated with the `[Equals]` Attribute.

This add in is inspired by [Equals.Fody](https://github.com/Fody/Equals/), but uses only explicit annotated members to generate the code.

[Introduction to Fody](http://github.com/Fody/Fody/wiki/SampleUsage).


### The nuget package

https://nuget.org/packages/Equatable.Fody/

    PM> Install-Package Equatable.Fody


### Your Code

```C#
[ImplementsEquatable]
public class Point
{
    [Equals]
    private int _x;
        
    [Equals(StringComparison.OrdinalIgnoreCase)]
    public string Y { get; set; }
        
    public int Z { get; set; }
        
    [CustomEquals]
    bool CustomLogic(Point other)
    {
        return Z == other.Z || Z == 0 || other.Z == 0;
    }
}

[ImplementsEquatable]
public class CustomGetHashCode
{
    [Equals]
    private int _x;
        
    [Equals]
    public int Y { get; set; }

    public int Z { get; set; }

    [CustomGetHashCode]
    int CustomGetHashCodeMethod()
    {
        return 42;
    }
}
```

### What gets compiled

```C
public class Point : IEquatable<Point>
{
    private int _x;

    public string Y { get; set; }

    public int Z { get; set; }

    private bool CustomLogic(Point other)
    {
      if (this.Z != other.Z && this.Z != 0)
        return other.Z == 0;
      return true;
    }

    private static bool <InternalEquals>(Point left, Point right)
    {
      if (left.CustomLogic(right) && left._x == right._x)
        return StringComparer.OrdinalIgnoreCase.Equals(left.Y, right.Y);
      return false;
    }

    public virtual bool Equals(Point other)
    {
      if ((object) other == null)
        return false;
      if ((object) this == (object) other)
        return true;
      return Point.<InternalEquals>(this, other);
    }

    public override int GetHashCode()
    {
      return <HashCode>.Aggregate(<HashCode>.Aggregate(0, this._x), <HashCode>.GetStringHashCode(this.Y, StringComparer.OrdinalIgnoreCase));
    }

    public override bool Equals(object obj)
    {
      return this.Equals(obj as Point);
    }

    public static bool operator ==(Point left, Point right)
    {
      return Point.<InternalEquals>(left, right);
    }

    public static bool operator !=(Point left, Point right)
    {
      return !Point.<InternalEquals>(left, right);
    }
}

public class CustomGetHashCode : IEquatable<CustomGetHashCode>
{
    private int _x;

    public int Y { get; set; }

    public int Z { get; set; }

    private int CustomGetHashCodeMethod()
    {
      return 42;
    }

    private static bool <InternalEquals>(CustomGetHashCode left, CustomGetHashCode right)
    {
      if (left._x == right._x)
        return left.Y == right.Y;
      return false;
    }

    public virtual bool Equals(CustomGetHashCode other)
    {
      if ((object) other == null)
        return false;
      if ((object) this == (object) other)
        return true;
      return CustomGetHashCode.<InternalEquals>(this, other);
    }

    public override int GetHashCode()
    {
      return <HashCode>.Aggregate(<HashCode>.Aggregate(<HashCode>.Aggregate(0, this.CustomGetHashCodeMethod()), this._x), this.Y);
    }

    public override bool Equals(object obj)
    {
      return this.Equals(obj as CustomGetHashCode);
    }

    public static bool operator ==(CustomGetHashCode left, CustomGetHashCode right)
    {
      return CustomGetHashCode.<InternalEquals>(left, right);
    }

    public static bool operator !=(CustomGetHashCode left, CustomGetHashCode right)
    {
      return !CustomGetHashCode.<InternalEquals>(left, right);
    }
}

internal static class <HashCode>
{
    static int Aggregate(int hash1, int hash2)
    {
        return (hash1 << 5) + hash1 ^ hash2;
    }

    static int GetHashCode(object value)
    {
        if (value == null)
            return 0;
        return value.GetHashCode();
    }

    static int GetStringHashCode(string value, StringComparer comparer)
    {
        if (value == null)
            return 0;
        return comparer.GetHashCode(value);
    }
}
```
