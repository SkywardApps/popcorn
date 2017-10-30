# [Popcorn](../README.md) > Releases and Release Notes

[Table Of Contents](TableOfContents.md)

---
## Major Release: 2.0.0
+ **Feature Additions:**
	+ Condensed our entire .NET offering into one .NET Standard project
	+ Enhanced the map ability so a single source type can be mapped to multiple destination types
	+ Added a default response inspector implementation
	+ Authorizers added as a configuration option to restrict access to certain objects as specified
+ **Bug Fixes:**
	+ Enabled the handling of polymorphism  in DefaultIncludes
	+ MapEntityFramework method allows for custom configurations without additional setup
+ **Maintenance**
	+ Documentation added: [Authorizers](dotnet/DotNetTutorialAuthorizers.md), [Factories](dotnet/DotNetTutorialAdvancedProjections.md) in Advanced Projections tutorial, 
	Response [Inspectors](dotnet/DotNetTutorialInspectors.md)
	+ Test additions and added CI to GitHub project

---
### Minor Release: 1.3.0
+ **Feature Additions:**  
    + Query parameter "sort" added to allow the sorting of responses based on a simple comparable property
		+ Query parameter "sortDirection" added to be used in conjunction with "sort" to specify ascending or descending sort order
    + Added [Sorting](dotnet/DotNetTutorialSorting.md) tutorial
+ **Maintenance:**
    + Test additions

---
### Minor Release: 1.2.0
+ **Feature Additions:**  
    + [IncludeByDefault] added as a property option for projections to allow users to set their default return properties in the projection itself.
    + Naming of [SubPropertyIncludeByDefault] updated
    + Added [DefaultIncludes](dotnet/DotNetTutorialDefaultIncludes.md) tutorial
+ **Maintenance:**
    + Test additions

---
#### Patch Release: 1.1.3
+ **Bug Fixes:**
	+ Adding a solution to allow nulls to be passed to an inspector
	+ Allowing spaces to be passed in an include request, i.e ?include=[property1, property2[subproperty1, subproperty2]]

---
#### Patch Release: 1.1.2
+ **Bug Fixes:**
	+ Got reference navigation properties working again.

---
#### Patch Release: 1.1.1
+ **Bug Fixes:**
	+ Allowed blind expansion to be accomplished when specifically designated

--- 
### Minor Release: 1.1.0
+ **Feature Additions:**  
	+ Added all initial documentation

---
## Major Release: 1.0.0
	+ Project inception!