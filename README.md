### This is an add-in for [Fody](https://github.com/Fody/Fody/) [![NuGet Status](http://img.shields.io/nuget/v/Equatable.Fody.svg?style=flat)](https://www.nuget.org/packages/Equatable.Fody/) [![Build status](https://ci.appveyor.com/api/projects/status/7d90l86aaw7ke7eq?svg=true)](https://ci.appveyor.com/project/tom-englert/equatable-fody)


![Icon](Icon.png)

Generate the Equals, GetHashCode and operators methods from properties or fields explicitly decorated with the `[Equals]` Attribute.

This add in is inspired by [Equals.Fody](https://github.com/Fody/Equals/), but uses only explicit annotated members to generate the code.

[Introduction to Fody](http://github.com/Fody/Fody/wiki/SampleUsage).


### NuGet installation

Install the [Equatable.Fody NuGet package](https://nuget.org/packages/Equatable.Fody/) and update the [Fody NuGet package](https://nuget.org/packages/Fody/):

```
PM> Install-Package Equatable.Fody
PM> Update-Package Fody
```

The `Update-Package Fody` is required since NuGet always defaults to the oldest, and most buggy, version of any dependency.

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
