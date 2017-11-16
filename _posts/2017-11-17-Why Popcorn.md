![Popcorn]({{ "/assets/imgs/mashup.png" | absolute_url }})

**Why adopt a new technology?**

The eternal question asked by every business owner, employee, software developer, politician, mathematician… yeah, ok, it's kind of **the** question, we get it.

There's really only one way to know for sure if a technology is appropriate for your particular project and that is to give it a try. We recognize that is highly inefficient and sometimes completely impractical, so we are going to try help with that headache here.

### What is Popcorn?

Popcorn is a communication protocol on top of a RESTful API that allows requesting clients to

identify individual fields of resources to include when retrieving the resource or resource collection.

It allows for a recursive selection of fields, allowing multiple calls to be condensed into one - among other features like sorting, authentication, and defaults.

What is the need Popcorn fills (i.e. **the big why**)?

Simply put, dynamic configuration of response objects and what can be returned is a feature that most API consumers would love to have. Being able to specify exactly what a consumer would like and how they would like it from an API is starting to become an expectation, not just an added feature.

## The Big Task

We are going to compare four existing tools that can be used to achieve the effect of allowing consumers to request exactly what they need from your API&#39;s and nothing more.

### [Skyward&#39;s Popcorn](https://skywardapps.github.io/popcorn/)

_As described above, Skyward released Popcorn to be a seamless addition to an existing .NET API._

### Microsoft&#39;s [OData](http://www.odata.org/)

_&quot;OData (Open Data Protocol) is an_ [_ISO/IEC approved_](https://www.oasis-open.org/news/pr/iso-iec-jtc-1-approves-oasis-odata-standard-for-open-data-exchange)_,_ [_OASIS standard_](https://www.oasis-open.org/committees/tc_home.php?wg_abbrev=odata) _that defines a set of best practices for building and consuming RESTful APIs.&quot;_

### Facebook&#39;s [GraphQL](https://www.graphql.com/)

_&quot;GraphQL is a query language for APIs and a runtime for fulfilling those queries with your existing data.&quot;_

### [JSON API](http://jsonapi.org/)

_JSON API is a full schema built on top of JSON that sets a convention for all API structures and responses._

Now, there needs to be an agreed upon set of assumptions or else we could go down some deep, dark tabs or spaces debate that frankly just may not have a completely &quot;correct&quot; answer.

These assumptions are mostly built around our own internal needs and uses, but should generally give you a basis for our rationale behind how we evaluate each option.

#### Assumptions

- A RESTful API is the preferred result
- The preferred back end development language is C#
- Front end direct access to our database is not the ideal setup

#### Evaluation Criteria

Now for the moment of truth, we will rate each option below on a 0 (poor) - 10 (great) scale on each of the following criteria

- **Ease of integration (new project)** - If we are starting from scratch, how easy is it to integrate said system.
- **Ease of integration (existing project)** - If we are starting with an already existing RESTful API service, how easy is it to integrate said system.
- **Feature set** - How does the feature set for this system stack up in this space?
- **Security** - Does the system provide an acceptable layer of security for data? (Partially referring back to our assumption around data access)
- **Responsiveness** - How responsive is the community to me if I end up needing support or help and how actively is it maintained?

Ok, enough of that, let&#39;s see some results!

### TL;DR

What will fit your needs is going to be specific to you and your project. We find that **Popcorn** fits our needs the best (hence why we made it in the first place) and think it certainly could do the same for you!

You&#39;ll see our &quot;ratings&quot; below for each category with the following scale and deeper reasoning provided further along.

- &quot;+&quot; Good option
- &quot;-&quot; Poor option

| **Product** | Integration Ease  (New) | Integration Ease(Existing) | Feature Set | Security | Responsiveness |
| --- | --- | --- | --- | --- | --- |
| Popcorn | **+** | **+** | **-** | **+** | **+** |
| OData | **+** | **-** | **+** | **-** | **+** |
| GraphQL | **+** | **-** | **+** | **+** | **-** |
| JSON API | **-** | **-** | **+** | **-** | **-** |

#### Ease of integration (new project)

This one goes to **Popcorn** , but not by miles over GraphQL or OData.

As JSON API is a specification and has an approved media-type it has some very unique requirements to even be considered a true JSON API. This overhead is an added layer of complexity when it comes to integrating &quot;the big why&quot; functionality and may actually be a bit more bulk than necessary, albeit there are quite a few .NET integrations that ease this pain quite dramatically.

The same is ostensibly true of GraphQL as it does require its own custom set up of queries and learning a completely new way of viewing your API&#39;s as it&#39;s not inherently designed to be RESTful (while it doesn&#39;t prevent that either).

Due to OData and its [WebAPI](http://odata.github.io/WebApi/#01-02-getting-started) implementation being very tightly integrated with the data model, it is just not the easiest to configure right out of the box which makes it slightly less than ideal for a completely new project and as with the other options, may just be too much overhead.

Popcorn takes the cake in this one because it offers the most flexibility with the easiest out of the box setup for someone looking to offer a RESTful service. Pulling the Nuget package and setting up a Popcorn configuration up front is about all that is minimally required to start seeing benefits of filtering, sorting, etc.

#### Ease of integration (existing project)

Of all the categories this is one that **Popcorn** really struts its stuff in.

For a shop already running a .NET RESTful service, Popcorn will be far and away the easiest to integrate into their service. It is possible to bring in Popcorn without even having to affect their consumers in any noticeable way, but all the while adding a whole suite of functionality that wasn&#39;t there before.

Simply put, that is not possible with any of the other options (at the very least not nearly as easily as with Popcorn in the case of JSON API or OData).

#### Feature Set

**OData** is the frontrunner in this category, with JSON API and GraphQL also looking good.

OData offers an ISO approved standard set of features that can layer right over the top of a .NET RESTful service, which contributes mightily to it being the most robust option with regards to features. Some of those features that aren&#39;t offered in Popcorn are things like complex filtering and accepting lambda functions in RESTful calls.

Due to the fact that JSON API and GraphQL have been around for a bit, you will see features and implementation options available with these services that Popcorn simply doesn&#39;t offer yet like all the various programming languages they have implementations for.

With that said Popcorn still stays close to the pack because of our assumptions beforehand and when it comes to purely a .NET implementation - all four are pretty similar in functionality and feature offerings at their cores.

#### Security

**Popcorn** and GraphQL both handle data protection similarly with the way they use context (i.e. pushing most of the logic to the business logic layer) and integrating that context with the actual API consumption.

OData and JSON API don&#39;t really offer much in their .NET implementations with regards to security and require that most of that logic be set by the actual developer using their libraries.

Popcorn does jump out as a little more featureful than the others here in that it does extend an Authorization option native to its .NET implementation. This can be used as a part of the server logic to handle what can and can&#39;t be returned from your application under specific circumstances, whereas with the other services you&#39;d essentially be forced to roll your own logic to achieve a similar effect.

#### Responsiveness

The Skyward Apps team has 8 active maintainers of the **Popcorn** project and moves quickly with new technologies and techniques in a way that only a small business can so it scores very well in this category. New adopters can have a very active role in the future development of Popcorn in ways they wouldn&#39;t with the other options.

The same simply can&#39;t be true of GraphQL, OData, and JSON API. They are open source projects that do take a huge amount of community feedback, but JSON API doesn&#39;t appear to have a very active maintainer at this moment and GraphQL is maintained primarily by one developer. OData does pretty well in this category as the project and its implementations do appear to be quite actively maintained by Microsoft.

**In summary** , you&#39;ve got to pick the solution that best meets your needs and we, at Skyward Apps, are here to help should you have any questions or concerns as you get started enhancing your APIs! #apifirst