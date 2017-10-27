Thanks for checking out Popcorn! 

Like many open source libraries, Popcorn started as a solution to a problem that came up in a few of our projects around the same time.
 Our team needed to be able to stitch together objects that were being returned by a .Net Core Restful API. At times, we had to retrieve
 a collection of a thousand items; then, for *each* item, make an identical request to the API to get a piece of dependent data.

That makes for a ton of network traffic, and it wasn’t very efficient.  We could have changed the endpoint for a specific usage, or added
 a new endpoint... but by then you are designing the API around one particular application’s usage parameters, and if you plan to release 
 a public API... well, that way lies madness.

A few quick googles later, we found a couple of things:
There are indeed solutions for this problem; jsonapi and graphql are two well established players in this space.
 The existing solutions all require specific schemas and would require us to start over from scratch with our client api implementations, 
 and at least partially rewrite our server code.

Well, starting over didn’t sound great, and we didn’t love the idea of handing over our schema design entirely anyway.  Instead, we created
 our own answer that would ‘sit on top’ of an existing API and provide a pluggable mechanic so it could handle a variety of actual 
 implementations.  

Thus, Popcorn was born!

![Popcorn]({{ "/assets/imgs/georgia-vagim-381292.jpg" | absolute_url }})
<a style="background-color:black;color:white;text-decoration:none;padding:4px 6px;font-family:-apple-system, BlinkMacSystemFont, &quot;San Francisco&quot;, &quot;Helvetica Neue&quot;, Helvetica, Ubuntu, Roboto, Noto, &quot;Segoe UI&quot;, Arial, sans-serif;font-size:12px;font-weight:bold;line-height:1.2;display:inline-block;border-radius:3px;" href="https://unsplash.com/@georgiavagim?utm_medium=referral&amp;utm_campaign=photographer-credit&amp;utm_content=creditBadge" target="_blank" rel="noopener noreferrer" title="Download free do whatever you want high-resolution photos from Georgia Vagim"><span style="display:inline-block;padding:2px 3px;"><svg xmlns="http://www.w3.org/2000/svg" style="height:12px;width:auto;position:relative;vertical-align:middle;top:-1px;fill:white;" viewBox="0 0 32 32"><title></title><path d="M20.8 18.1c0 2.7-2.2 4.8-4.8 4.8s-4.8-2.1-4.8-4.8c0-2.7 2.2-4.8 4.8-4.8 2.7.1 4.8 2.2 4.8 4.8zm11.2-7.4v14.9c0 2.3-1.9 4.3-4.3 4.3h-23.4c-2.4 0-4.3-1.9-4.3-4.3v-15c0-2.3 1.9-4.3 4.3-4.3h3.7l.8-2.3c.4-1.1 1.7-2 2.9-2h8.6c1.2 0 2.5.9 2.9 2l.8 2.4h3.7c2.4 0 4.3 1.9 4.3 4.3zm-8.6 7.5c0-4.1-3.3-7.5-7.5-7.5-4.1 0-7.5 3.4-7.5 7.5s3.3 7.5 7.5 7.5c4.2-.1 7.5-3.4 7.5-7.5z"></path></svg></span><span style="display:inline-block;padding:2px 3px;">Georgia Vagim</span></a>

With Popcorn we could specify in our initial request what data we wanted to drill down into, so that first call that retrieved a thousand
 items already contained the information we needed. But if we didn’t request it, the data wasn’t included, meaning a smaller, quicker
 transfer and less stress on the database.  Either way, this request was simply an additional query parameter that didn’t require modifying
 our endpoint routes.

It has grown a bit since then with some other features we found valuable along the way — server side sorting and per-object authorization
 calls are a couple of my favorites — and we hope others find it as quick and easy as we did.  We’re still actively developing it, and
 already getting some great community feedback and pull requests.  

Head on over to our [documentation site]() to learn more, or visit the github repo to get involved! (Provide link)


