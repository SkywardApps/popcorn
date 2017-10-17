# [Popcorn](../README.md) > Releases and Release Notes
---
## Major Release: 2.0.0
+ **Feature Additions:**  
	+ The inspector has been slightly revamped to also require a handle for exceptions!
	+ A SetDefaultApiResponseInspector option has been added to the configuration for easier out of the box usage
	+ Added [Setting Inspectors](dotnet/DotNetTutorialInspectors.md) tutorial
+ **Maintenance:**
    + Test additions

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