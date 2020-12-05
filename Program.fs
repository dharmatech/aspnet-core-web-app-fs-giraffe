module giraffe_test_a.App

open System
open System.IO
open System.Diagnostics
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Cors.Infrastructure
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection
open Giraffe

// ---------------------------------
// Models
// ---------------------------------

type Message = { Text : string }

type ErrorViewModel = { RequestId : string; ShowRequestId : bool }

// ---------------------------------
// Views
// ---------------------------------

module Views =
    open Giraffe.ViewEngine

    let layout (title_data: string) (content: XmlNode list) =
        html [] [
            head [] [
                title []  [ 
                    encodedText title_data 
                    encodedText " - WebApplicationCs"
                ]
                link [ _rel "stylesheet"; _type "text/css"; _href "/lib/bootstrap/css/bootstrap.min.css" ]
                link [ _rel "stylesheet"; _type "text/css"; _href "/main.css" ]
            ]
            body [] [ 
                header [] [
                    nav [ _class "navbar navbar-expand-sm navbar-toggleable-sm navbar-light bg-white border-bottom box-shadow mb-3" ] [
                        div [ _class "container" ] [
                            a [ _class "navbar-brand"; _href "/" ] [ encodedText "WebApplicationCs" ]
                            button [ 
                                _class "navbar-toggler"
                                _type "button"
                                attr "data-toggle" "collapse"
                                attr "data-target" ".navbar-collapse"
                                attr "aria-controls" "navbarSupportedContent"
                                attr "aria-expanded" "false"
                                attr "aria-label" "Toggle navigation"
                            ] [
                                span [ _class "navbar-toggler-icon" ] []
                            ]
                            div [ _class "navbar-collapse collapse d-sm-inline-flex flex-sm-row-reverse" ] [
                                ul [ _class "navbar-nav flex-grow-1" ] [
                                    li [ _class "nav-link dark-text" ] [ a [ _href "/"             ] [ encodedText "Home"   ] ]
                                    li [ _class "nav-link dark-text" ] [ a [ _href "/Home/Privacy" ] [ encodedText "Privacy"] ]
                                ]
                            ]
                        ]
                    ]
                ]
                
                div [ _class "container" ] [ main [ attr "role" "main"; _class "pb-3" ] content ]

                footer [ _class "border-top footer text-muted" ] [
                    div [ _class "container" ] [
                        rawText "&copy; "
                        encodedText "2020 - WebApplicationCs - "
                        a [ _href "/Home/Privacy" ] [ encodedText "Privacy" ]
                    ]
                ]

                script [ _src "/lib/jquery/jquery.min.js" ] []
                script [ _src "/lib/bootstrap/js/bootstrap.bundle.min.js" ] [] 
            ]
        ]

    let index =
        [
            div [ _class "text-center" ] [
                h1 [ _class "display-4" ] [ encodedText "Welcome" ]
                p [] [
                    encodedText "Learn about "
                    a [ _href "https://docs.microsoft.com/aspnet/core" ] [ 
                        encodedText "building Web apps with ASP.NET Core" 
                    ]
                ]
            ]
        ] |> layout "Home Page"

    let privacy =
        let title = "Privacy Policy"
        [ 
            h1 [] [ encodedText title ]
            p [] [ encodedText "Use this page to detail your site's privacy policy." ]
        ] |> layout title

    let error (model : ErrorViewModel) =
        [
            h1 [ _class "text-danger" ] [ encodedText "Error." ]
            h2 [ _class "text-danger" ] [ encodedText "An error occurred while processing your request." ]

            if model.ShowRequestId then p [] [ 
                strong [] [ encodedText "Request ID:"; ]
                code [] [ encodedText model.RequestId ]
            ]

            h3 [] [ encodedText "Development Mode" ]

            p [] [
                encodedText "Swapping to "
                strong [] [ encodedText "Development" ]
                encodedText " environment will display more detailed information about the error that occurred."
            ]

            p [] [
                rawText """
                    <strong>The Development environment shouldn't be enabled for deployed applications.</strong>
                    It can result in displaying sensitive information from exceptions to end users.
                    For local debugging, enable the <strong>Development</strong> environment by setting the <strong>ASPNETCORE_ENVIRONMENT</strong> environment variable to <strong>Development</strong>
                    and restarting the app.                
                    """
            ]

        ] |> layout "Error"


// ---------------------------------
// Web app
// ---------------------------------

let indexHandler (name : string) =
    htmlView Views.index

let error_handler =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        htmlView (Views.error { 
            RequestId = 
                match Activity.Current with
                | null -> ctx.TraceIdentifier
                | activity_current -> activity_current.Id
            ShowRequestId = true 
        }) next ctx

let webApp =
    choose [
        GET >=>
            choose [
                route  "/" >=>     htmlView Views.index
                routef "/hello/%s" indexHandler
                route "/Home/Privacy" >=> htmlView Views.privacy
                route "/Home/Error" >=> error_handler
                route "/error" >=> (fun _ _ -> failwith "234")
            ]
        setStatusCode 404 >=> text "Not Found" 
    ]

// ---------------------------------
// Error handler
// ---------------------------------

let errorHandler (ex : Exception) (logger : ILogger) =
    logger.LogError(ex, "An unhandled exception has occurred while executing the request.")
    clearResponse >=> setStatusCode 500 >=> error_handler

// ---------------------------------
// Config and Main
// ---------------------------------

let configureCors (builder : CorsPolicyBuilder) =
    builder.WithOrigins("http://localhost:8080")
           .AllowAnyMethod()
           .AllowAnyHeader()
           |> ignore

let configureApp (app : IApplicationBuilder) =
    let env = app.ApplicationServices.GetService<IWebHostEnvironment>()

    (match env.EnvironmentName with
    | "Development" -> app.UseDeveloperExceptionPage()
    | "Alt"         -> app.UseExceptionHandler("/Home/Error")
    | _             -> app.UseGiraffeErrorHandler(errorHandler))
        .UseHttpsRedirection()
        .UseCors(configureCors)
        .UseStaticFiles()
        .UseGiraffe(webApp)

let configureServices (services : IServiceCollection) =
    services.AddCors()    |> ignore
    services.AddGiraffe() |> ignore

let configureLogging (builder : ILoggingBuilder) =
    builder.AddFilter(fun l -> l.Equals LogLevel.Error)
           .AddConsole()
           .AddDebug() |> ignore

[<EntryPoint>]
let main args =
    let contentRoot = Directory.GetCurrentDirectory()
    let webRoot     = Path.Combine(contentRoot, "WebRoot")
    Host.CreateDefaultBuilder(args)
        .ConfigureWebHostDefaults(
            fun webHostBuilder ->
                webHostBuilder
                    .UseContentRoot(contentRoot)
                    .UseWebRoot(webRoot)
                    .Configure(Action<IApplicationBuilder> configureApp)
                    .ConfigureServices(configureServices)
                    .ConfigureLogging(configureLogging)
                    |> ignore)
        .Build()
        .Run()
    0