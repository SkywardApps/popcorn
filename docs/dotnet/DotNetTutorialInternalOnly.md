# [Popcorn](../../README.md) > [Documentation](../Documentation.md) > [DotNet](DotNetDocumentation.md) > Tutorial: Internal Only Attribute

We asume you've already learned the basics of Popcorn. If not, you should probably go back and complete [Getting Started](DotNetTutorialGettingStarted.md) first.

This tutorial will walk you through a few ways to protect your data.
We will achieve this using the [InternalOnly] attribute. It can be used on Classes, Properties and Methods.
```csharp
[InternalOnly(throwException)]
```
throwException is a bool parameter, the default value is true.
* When used on classes:
    * If true an InternalOnlyViolationException will be thrown when you will try to expand the class. 
    * If false, you will receive a null object in response.
 * When used on methods and properties:
    * If true an InternalOnlyViolationException will be thrown when you will try to acces the marked field or method. 
    * If false, you will receive a null object in response.

Example class usage:
```csharp
[InternalOnly] //note, the default true value is used
public class InternalClass
{
    string Field1;
}
```
Assuming you've mapped the projection as discussed in [Getting Started](DotNetTutorialGettingStarted.md), let's check what  comes back from our "InternalClass" endpoint.
localhost:yourport/api/example/internalc
```json
{
  "Success": false,
  "ErrorCode": "Skyward.Popcorn.InternalOnlyViolationException",
  "ErrorMessage": "Expand: InternalClass class is marked [InternalOnly]",
  "ErrorDetails": "--truncated for saving space--"
}
```

Moving on to fields and methods with the throwException parameter set to false:
```csharp
public class InternalFieldsClass
{
    [InternalOnly(false)] //note, the throwException parameter is set to false
    public string Field1 { get; set; } //this is an internal field
    public string Field2 { get; set; } //this in not an internal field

    [InternalOnly(false)]
    public string Method1() { return "method1"; }
}
```
And the result:
localhost:yourport/api/example/internalf
```json
{
  "Success": true,
  "Data": {
    "Field2": "Field2"
  }
}
```
Note that the values returned for Field1 and Method1 are null (actually, they don't exist in the json).

Moving on to an example of properties with the throwException parameter set to true:
```csharp
public class InternalFieldClassException
{
    [InternalOnly(true)] //note the true parameter
    public string Field1 { get; set; }
}
```
And the response:
localhost:yourport/api/example/internalferror
```json
{
  "Success": false,
  "ErrorCode": "Skyward.Popcorn.InternalOnlyViolationException",
  "ErrorMessage": "Expand: Field1 property inside InternalFieldClassException class is marked [InternalOnly]",
  "ErrorDetails": "--truncated for saving space--"
}
```
An exception was thrown as expected.

It is up to you on how you will use the throwException parameter according to your needs. It's default true value is generally better for finding errors in your code.

And that's it, you can now use the [InternalOnly] attribute.
