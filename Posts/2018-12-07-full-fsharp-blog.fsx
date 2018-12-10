(**
*this post is part of the [F# Advent Calendar 2018](https://sergeytihon.com/2018/10/22/f-advent-calendar-in-english-2018/)*


Christmas arrived early this year: my [pull request](https://github.com/fsprojects/FSharp.Formatting/pull/480) to update 
FSharp.Formatting to netstandard2.0 was accepted on Oct 22 <i class="fas fa-grin-stars"></i>.

This, combined with a discussion at [Open FSharp](https://www.openfsharp.org/) with [Alfonso](https://twitter.com/@alfonsogcnunez) - famous for creating [Fable](https://fable.io/) -
led me to use F#, and only F# to publish my blog.

## Why not just using wordpress ?

This blog was previously hosted by Gandi on dotclear, and it was ok for years.

Pros:

* Free
* Managed
* No ads

Cons:

* Hard to style
* No extensibility
* Low ownership
* Painful editing

The editing was really painful in the browser, especialy since Live Writer was not supported anymore. I managed
to use FSharp.Formatting for some posts. But when pasting the code in the editor, most of the formatting was 
rearranged due to platform limitations. I took infinite time to gets the tips working.

But it was free.

I could have found another blogging platform, but most of them are free because ads. 
Or I could pay. But those plateforms are not necessary better...

And there was anyway an extra challenge: **I didn't want to break existing links**.

I have some traffic on my blog. External posts, Stack Overflow answers, tweets pointing to my posts. It would be
a waste to lose all this.

So it took some time and I finally decided to host it myself. *)
(**
## My stack

The constraint of not breaking existing structure forced me to actually develop my own solution. And since
I'm quite fluent in F#... everything would eventually be in F#.

* F# scripts for building
  * FSharp.Literate to convert md and fsx files to HTML.
  * Fable.React server side rendering for a static full F# template engine
  * FSharp.Data for RSS generation
* F# script for testing
  * Giraffe to host a local web server to view the result
* F# script for publishing

The other constraint was price. And since previous solution was free, I took it has a chanlenge to
try to make it as cheap as possible. There are lots of free options, but never with custom domain 
(needed to not break links), and never with https (mandatory since google is showing HTTP sites as unsecure).

I choosed Azure Functions, but with no code. I get:

* Custom domain for free
* Free SSL certificate thanks to [letsencrypt](https://letsencrypt.org/)
* KeyVault for secret management
* Staging for deployment
* Full ownership
* Almost free (currently around 0.01€/month)

I'll detail the F# part in this post. The azure hosting will be for part 2, and letsencrypt for part 3.
*)
(**
## Fake 5
[Fake 5](https://fake.build/) is **the** tool to write build scripts or simply scripts in F# (thx [forki](https://twitter.com/@sforkmann)).

To install it, simply type in a command line

    [lang=sh]
    dotnet tool install fake-cli -g

and you're done.

The strength of Fake is that it can reference and load nuget packages using [paket](https://fsprojects.github.io/Paket/index.html) (thx again [forki](https://twitter.com/@sforkmann))
directly in the fsx script:

    #r "paket: 
    source https://api.nuget.org/v3/index.json
    nuget FSharp.Data //"

Fake will then dowload and reference specified packages.

To help with completion at design time you can reference an autogenerated
fsx like this:

    #load "../.fake/blog.fsx/intellisense.fsx" 

*)
(*** hide ***)
#r "netstandard"
#I @"..\packages\full\FSharp.Literate\lib\netstandard2.0\"
#r "FSharp.Literate"
#r "FSharp.Markdown"
#r "FSharp.CodeFormat"
#r @"..\packages\full\Fable.React\lib\netstandard2.0\Fable.React.dll"

(**
*here I use .. because this blog post is in a subfolder in my project*

Packages can then be used:
*)
open FSharp.Data
(**
## FSharp.Literate

[FSharp.Formatting](http://fsprojects.github.io/FSharp.Formatting/) is an awsome project
to convert F# and MarkDown to HTML.

Conversion to netstandard has been stuck for some time due to its
dependency on Razor for HTML templating.

Razor has changed a lot in AspNetCore, and porting existing code was a
real nightmare.

To speed up things, I proposed to only port FSharp.Literate and
the rest of the project but to get rid of formatting and this dependency
on Razor. There is now a beta nuget package deployed on appveyor at
https://ci.appveyor.com/nuget/fsharp-formatting :
so for my build script I use the following references:

    #r "paket:
    source https://api.nuget.org/v3/index.json
    source https://ci.appveyor.com/nuget/fsharp-formatting

    nuget Fake.IO.FileSystem
    nuget Fake.Core.Trace
    nuget FSharp.Data
    nuget Fable.React
    nuget FSharp.Literate //" 

    #load "../.fake/blog.fsx/intellisense.fsx" 

*)
open FSharp.Literate

(**
### Markdown

The simplest usage of FSharp.Literate is for posts with
no code. In this case, I write it as MarkDown file and convert them
using the TransformHtml function:
*)
let md = """# Markdown is cool
especially with *FSharp.Formatting* ! """
            |> FSharp.Markdown.Markdown.TransformHtml
(** which returns: *)
(*** include-value: md ***)


(**
### Fsx

We can also take a snipet of F# and convert it to HTML:
*)
let snipet  =
    """
    (** # *F# literate* in action *)
    printfn "Hello"
    """
let parse source =
    let doc = 
      let fsharpCoreDir = "-I:" + __SOURCE_DIRECTORY__ + @"\..\lib"
      let systemRuntime = "-r:System.Runtime"
      Literate.ParseScriptString(
                  source, 
                  compilerOptions = systemRuntime + " " + fsharpCoreDir,
                  fsiEvaluator = FSharp.Literate.FsiEvaluator([|fsharpCoreDir|]))
    FSharp.Literate.Literate.FormatLiterateNodes(doc, OutputKind.Html, "", true, true)
let format (doc: LiterateDocument) =
    Formatting.format doc.MarkdownDocument true OutputKind.Html
let fs =
    snipet 
    |> parse
    |> format
(** 
The fsharpCoreDir and the -I options are necessary to help FSharp.Literate resolve
the path to FSharp.Core. System.Runtime must also be referenced to get tooltips working
fine with netstandard assemblies. FSharp interactive is not totally ready for production due
to this problem, but with some helps, it works for our need.

Running this code we get: *)
(*** include-value: fs ***)
(** As you can see, the code contains a reference to a javascript
functions. You can find an implementation on [github](https://github.com/fsprojects/FSharp.Formatting/blob/master/docs/files/content/tips.js).
It displays type information tool tips generated by the compiler.
All the type information is generated during parsing phase:
*)
let tips =
    let doc = parse snipet
    doc.FormattedTips

(*** include-value: tips ***)
(**
this way readers get full type inference information in the browser !

But it's even better than that. You can also get the value of some bindings in your ouput:
*)
let values  = 
    """
(** # code execution *)
let square x = x * x
let v = square 3
(** the value is: *)
(*** include-value: v ***)"""
    |> parse
    |> format
(** and the result is: *)
(*** include-value: values ***)
(**
You can see that the value of v - 9 - has been computed in the output HTML !

As you can gess, I just used this feature to print the HTML output! **Inception** !

It also works for printf:

 *)
let output  = 
    """
(** # printing *)
let square x = x * x
(*** define-output: result ***)
printfn "result: %d" (square 3)
(** the value is: *)
(*** include-output: result ***)"""
    |> parse
    |> format

(** Notice the presence of the printf output on the last line: *)
(*** include-value: output ***)

(**
## Templating
Now that we can convert the content to HTML, we need to add the surrounding layout.

I use Fable.React for this, but just the server side rendering. So there is no need for
the JS tools, only the .net nuget.

After adding the `nuget Fable.React` in the paket includes, we can open it and
start a HTML template:
*)
open Fable.Helpers.React
open Fable.Helpers.React.Props
open FSharp.Markdown

type Post = {
    title: string
    content: string
}

let template post = 
    html [Lang "en"] [
        head [] [
            title [] [ str ("My blog / " + post.title) ]
        ]
        body [] [
            RawText post.content
        ]
    ]
(** to convert it too string, we simply add the doctype to make it HTML5 compatible and
use renderToString *)
let render html =
  fragment [] [ 
    RawText "<!doctype html>"
    RawText "\n" 
    html ]
  |> Fable.Helpers.ReactServer.renderToString 

(** let's use it : *)
let myblog =
    { title = "super post"
      content = Markdown.TransformHtml "# **interesting** things" }
    |> template
    |> render 
(** now we get the final page: *)
(*** include-value: myblog ***)

(**
## RSS

Rss has lost attraction lately, but I still have requests every day on the atom feed.

Using Fsharp data, generating the RSS feed is straight forward:

 *)

#I @"..\packages\full\FSharp.Data\lib\netstandard2.0\"
#r "System.Xml.Linq"
#r "FSharp.Data"
open System
open FSharp.Data
open System.Security.Cryptography

[<Literal>]
let feedXml = """<?xml version="1.0" encoding="utf-8"?>
<feed xmlns="http://www.w3.org/2005/Atom"
  xmlns:dc="http://purl.org/dc/elements/1.1/"
  xmlns:wfw="http://wellformedweb.org/CommentAPI/"
  xml:lang="en">
  
  <title type="html">Think Before Coding</title>
  <link href="http://thinkbeforecoding.com:82/feed/atom" rel="self" type="application/atom+xml"/>
  <link href="http://thinkbeforecoding.com/" rel="alternate" type="text/html"
  title=""/>
  <updated>2017-12-09T01:20:21+01:00</updated>
  <author>
    <name>Jérémie Chassaing</name>
  </author>
  <id>urn:md5:18477</id>
  <generator uri="http://www.dotclear.net/">Dotclear</generator>
  <entry>
    <title>fck: Fake Construction Kit</title>
    <link href="http://thinkbeforecoding.com/post/2016/12/04/fck%3A-Fake-Construction-Kit" rel="alternate" type="text/html"
    title="fck: Fake Construction Kit" />
    <id>urn:md5:d78962772329a428a89ca9d77ae1a56b</id>
    <updated>2016-12-04T10:34:00+01:00</updated>
    <author><name>Jérémie Chassaing</name></author>
        <dc:subject>f</dc:subject><dc:subject>FsAdvent</dc:subject>    
    <content type="html">    &lt;p&gt;Yeah it's christmas time again, and santa's elves are quite busy.&lt;/p&gt;
ll name: Microsoft.FSharp.Core.Operators.not&lt;/div&gt;</content>
      </entry>
  <entry>
    <title>Ukulele Fun for XMas !</title>
    <link href="http://thinkbeforecoding.com/post/2015/12/17/Ukulele-Fun-for-XMas-%21" rel="alternate" type="text/html"
    title="Ukulele Fun for XMas !" />
    <id>urn:md5:5919e73c387df2af043bd531ea6edf47</id>
    <updated>2015-12-17T10:44:00+01:00</updated>
    <author><name>Jérémie Chassaing</name></author>
        <dc:subject>F#</dc:subject>
    <content type="html">    &lt;div style=&quot;margin-top:30px&quot; class=&quot;container row&quot;&gt;
lt;/div&gt;</content>
      </entry>
</feed>"""

type Rss = XmlProvider<feedXml>
let links: Rss.Link[] = [|
    Rss.Link("https://thinkbeforecoding.com/feed/atom","self", "application/atom+xml", null)
    Rss.Link("https://thinkbeforecoding.com/","alternate", "text/html", "thinkbeforecoding")
    |]

let entry title link date content = 
    let md5Csp = MD5CryptoServiceProvider.Create()
    let md5 =
        md5Csp.ComputeHash(Text.Encoding.UTF8.GetBytes(content: string))
        |> Array.map (sprintf "%2x")
        |> String.concat ""
        |> (+) "urn:md5:"

    Rss.Entry(
        title,
        Rss.Link2(link, "alternate", "text/html", title),
        md5,
        DateTimeOffset.op_Implicit date,
        Rss.Author2("Jérémie Chassaing"),
        [||],
        Rss.Content("html", content)
        )
let feed entries =
    Rss.Feed("en", 
        Rss.Title("html","thinkbeforecoding"), 
        links,DateTimeOffset.UtcNow, 
        Rss.Author("Jérémie Chassaing"),
        "urn:md5:18477",
        Rss.Generator("https://fsharp.org","F# script"),
        List.toArray entries
         )

(**
just pass all posts to the feed function, and you get a full RSS feed.

## Migration

To migrate from my previous blog, I exported all data to csv, and used the CSV type provider
to parse it.

I extracted the HTML and put it in files, and generated a fsx file containing a list of posts
with metadata:

* title
* date
* url
* category

Once done, I just have to map the post list using conversion and templates, and I have my
new blog.

## Wrapping it up

Using F# tools, I get easily a full control on my blog. And all this in my favorite language!

See you in [next part about hosting in Azure](/post/2018/12/09/full-fsharp-blog-2).

Happy Christmas!

*)



