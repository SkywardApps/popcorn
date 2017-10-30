# [Popcorn](../../README.md) > [Documentation](../Documentation.md) > [DotNet](DotNetDocumentation.md) > Tutorial: Sorting

[Table Of Contents](../TableOfContents.md)

As we are a few releases in, we are going to assume you've read [Getting Started](DotNetTutorialGettingStarted.md) and our other tutorials to familiarize
yourself with Popcorn. Now lets say not only do you want to have your response objects from your API's return only the specific properties you desire, but
you'd also like the results to be sorted server side so you can just plug and play with your front end. We now have an answer.

This tutorial will walk you through the two new additions to our query parameters that we've added, "sort" and its related partner "sortDirection", and how they 
can be leveraged with your API's.

<a name="sort"/>

### "sort" Parameter

First let's see an example of a typical response object to the following request from our Cars endpoint we've referenced before
```javascript
http://localhost:50353/api/example/cars?include=[Make, Model, Color, Owner[FirstName, LastName]]

{
    "Success": true,
    "Data": [
        {
            "Owner": {
                "FirstName": "Liz",
                "LastName": "Lemon"
            },
            "Model": "Firebird",
            "Make": "Pontiac",
            "Color": "Blue"
        },
        {
            "Owner": {
                "FirstName": "Jack",
                "LastName": "Donaghy"
            },
            "Model": "250 GTO",
            "Make": "Ferrari N.V.",
            "Color": "Red"
        },
        {
            "Owner": {
                "FirstName": "Jack",
                "LastName": "Donaghy"
            },
            "Model": "Cayman",
            "Make": "Porsche",
            "Color": "Yellow"
        }
    ]
}
```

This is all fine and good as a response as it only includes the information we want, but let's take it a step further and assume this list
is 500 results long. We ultimately want to be able to present this list of Cars to a user in a browser and it may benefit us to push the sorting
to our server and minimize the code and load on the front end (admittedly, this example may not necessitate that, but you get the point).

Now show me the sorting!
Introducing our newest query parameter "sort" in action
```javascript
http://localhost:49695/api/example/cars?include=[Make, Model, Color, Owner[FirstName, LastName]]&sort=Model

{
    "Success": true,
    "Data": [
        {
            "Owner": {
                "FirstName": "Jack",
                "LastName": "Donaghy"
            },
            "Model": "250 GTO",
            "Make": "Ferrari N.V.",
            "Color": "Red"
        },
        {
            "Owner": {
                "FirstName": "Jack",
                "LastName": "Donaghy"
            },
            "Model": "Cayman",
            "Make": "Porsche",
            "Color": "Yellow"
        },
        {
            "Owner": {
                "FirstName": "Liz",
                "LastName": "Lemon"
            },
            "Model": "Firebird",
            "Make": "Pontiac",
            "Color": "Blue"
        }
    ]
}
```

You now see that the results are sorted in an ascending order by their Model property, easy as that!

#### Requirements and constraints
We didn't aim to hit all use cases here so if there is something you'd like to see or use, please submit a PR or write us up an issue 
and we'd love to work with you on that.

+ The "sort" parameter passed in may only be one parameter and it is case sensitive.
+ The parameter being targeted for "sort" may only be one on the primary response object.
	+ i.e. "Model" works great, but sort=FirstName will not work.
	+ Also targeting complex types will not work either because, for example, sorting based on "Vehicle" is a bit more tricky than a simple sort.
+ An "include" is not required to sort a response.
+ For that matter, neither is a "sortDirection" as it will default to ascending.

<a name="sortDirection"/>

### "sortDirection" Parameter

"sortDirection" is a far more simple query parameter as it just accepts two values, "Ascending" and "Descending" (case sensitive) 
and specifies the way the specified sort target is to be sorted.
Just remember, as we mentioned already, the sort direction defaults to "Ascending" should a sort be provided and no "sortDirection".

Without further ado, here we see it being used in the wild (with our standard Cars example)
```javascript
http://localhost:49695/api/example/cars?include=[Make, Model, Color, Owner[FirstName, LastName]]&sort=Model&sortDirection=Descending

{
    "Success": true,
    "Data": [
        {
            "Owner": {
                "FirstName": "Liz",
                "LastName": "Lemon"
            },
            "Model": "Firebird",
            "Make": "Pontiac",
            "Color": "Blue"
        },
        {
            "Owner": {
                "FirstName": "Jack",
                "LastName": "Donaghy"
            },
            "Model": "Cayman",
            "Make": "Porsche",
            "Color": "Yellow"
        },
        {
            "Owner": {
                "FirstName": "Jack",
                "LastName": "Donaghy"
            },
            "Model": "250 GTO",
            "Make": "Ferrari N.V.",
            "Color": "Red"
        }
    ]
}
```

It may go without saying, but if no "sort" is provided, then we don't apply a "sortDirection" by default because.. well.. you get it.

Easy as that! Please don't hesitate to submit a PR or issue with requests for added functionality or updates that will help you in using 
the sort functionality (or Popcorn in general).