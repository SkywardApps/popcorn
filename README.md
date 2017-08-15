# Popcorn

## What is Popcorn?
Popcorn is a specification layer on top of a RESTful API that allows requesting clients to 
identify individual fields of resources to include when retrieving the resource or resource
collection.

It allows for a recursive selection of fields, allowing multiple calls to be condensed 
into one.  

**Yeah so.... what is it?**

Okay, maybe some examples will help!

Lets say you have a REST API with an endpoint like so:

``` https://myserver.com/api/1/contacts ```

Which returns a list of contacts in the form:

``` 
[
 {
    
   "Id":1,
   "Name":"Liz Lemon"
 },
 {
   "Id":2,
   "Name":"Pete Hornberger"
 },
 {
   "Id":3,
   "Name": "Jack Donaghy"
 },
 ...
}
```

Now, if you want to get a list of phone numbers for each of those, you now need to make a series
of calls to further endpoints, one for each contact you want to look up the information for:

``` https://myserver.com/api/1/contacts/1/phonenumbers ```
```
[
  {"Type":"cell","Number":"867-5309"}
]
```
``` https://myserver.com/api/1/contacts/2/ ```
```
[
  {"Type":"landline","Number":"555-5555"}
]
```
``` https://myserver.com/api/1/contacts/3/phonenumbers ```
```
[
  {"Type":"cell","Number":"123-4567"}
]
```

That's quite a lot of overhead and work!  Popcorn aims to simplify this at the client's request.
Let's say that while we want the numbers for each contact, we don't really need the type of the number
(cell or landline) and would prefer to save the bandwidth by not transfering it.  Now, instead of 
making many calls, all the above can be reduced down to:

``` https://myserver.com/api/1/contacts?include=[Id,Name,PhoneNumbers[Number]] ```

Which provides:

```
[
 {
    
   "Id":1,
   "Name":"Liz Lemon",
   "PhoneNumbers":
   [
    {
     "Number":"867-5309"
    }
   ]
 },
 {
   "Id":2,
   "Name":"Pete Hornberger",
   "PhoneNumbers":
   [
    {
     "Number":"555-5555"
    }
   ]
 },
 {
   "Id":3,
   "Name": "Jack Donaghy",
   "PhoneNumbers":
   [
    {
     "Number":"123-4567"
    }
   ]
 },
 ...
}
```

Presto! All the information we wanted at our fingertips, and none of the data we didn't!

## Why would I use it?

By implementing the Popcorn protocol, you get a consistent, well defined API abstraction that your
API consumers will be able to easily utilize.  You will able to take advantage of the various
libraries and tools Popcorn offers; right now this includes a C# automatic implementation for 
asp.net core and entityframework core, but many more platforms are on the roadmap.

### Pros
+ Faster calls
+ Saves Data
+ Potential for Server-Side optimization
+ Less boilerplate code to write

### Cons
+ You don't get to write as much code
+ Your consumers don't get to write as much code

## How can I use it in my project?

First you need to figure out if you're working with a platform that has a provider implemented in 
the popcorn project. (Probably check out [Documentation](docs/Documentation)).

If there's a provider already in the project, great!  The platform-specific documentation will walk
you though incorporating the provider into your project.

If there isn't a provider yet, you'll have to roll your own.  You'll still get the benefit of 
working with a standard, so your consumers will have a well documented spec to work with.  Feel free
to contribute any platform-specific providers you come up with!

## Further Reading

+ [Quick Start](docs/QuickStart)
+ [Documentation](docs/Documentation)
+ [Roadmap](docs/Roadmap)
+ [Contributing](docs/Contributing)
+ [License](LICENSE)
