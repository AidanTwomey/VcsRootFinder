#r "packages/FSharp.Data/lib/net40/FSharp.Data.dll" 
#r "System.Xml.Linq.dll"

open FSharp.Data
open FSharp.Data.HttpRequestHeaders
open System.Xml
open System

type VcsRoots = XmlProvider<"""
<vcs-roots count="175">
  <vcs-root id="id1" />
  <vcs-root id="id2" />
</vcs-roots> """>

type VcsRoot = XmlProvider<"""
<vcs-root>
<project id="projid"/>
<properties>
<property name="url" value="git@gitorious.7digital.local:rake/rake-build.git"/>
<property name="usernameStyle" value="NAME"/>
</properties>
<vcsRootInstances href="ref"/>
</vcs-root>
""">

let teamCityUrl = "http://teamcity.win.sys.7d/httpAuth/app/rest"

let vcsRootsUrl = "vcs-roots"
let projectsUrl = "projects"

let isUrlProperty (prop:VcsRoot.Property) =
    if ( prop.Name = "url") then Some prop.Value else None

let isGitorious (value:string option) =
    match value with
    | Some i -> i.Contains("git@gitorious")
    | None   -> false

let mutable password = "aidantc"

let makeHttpQuery url =
    Http.RequestString  ( String.Format( "{0}/{1}/", teamCityUrl, url), headers = [ BasicAuth "aidantwomey" password  ])

makeHttpQuery vcsRootsUrl

let archiveProject archived id =    
    printfn "archive TeamCIty project %s" id
    Http.RequestString
      ( String.Format( "{0}/{1}/{2}/archived", teamCityUrl, projectsUrl, id ),
        headers = [ BasicAuth "aidantwomey" password ; ContentType HttpContentTypes.Text  ],
        body = TextRequest archived,
        httpMethod = "PUT")


VcsRoots.Parse( makeHttpQuery vcsRootsUrl ).VcsRoots 
    |> Seq.map (fun vcs -> String.Format("{0}/{1}", vcsRootsUrl, vcs.Id ) )
    |> Seq.map makeHttpQuery    
    |> Seq.map VcsRoot.Parse
    |> Seq.groupBy ( fun root -> root.Project.Id )
    |> Seq.map (fun id -> String.Format( "{0}/{1}/archived", projectsUrl, fst id ) )
    |> Seq.map (fun url -> (url, makeHttpQuery url))
    |> Seq.where (fun (url,archived) -> archived = "false")
    |> Seq.iter Console.WriteLine
    //|> Seq.map (fun (id,archived) -> archiveProject "false" id)
    // |> ignore

