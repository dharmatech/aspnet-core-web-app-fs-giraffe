When you create an ASP.NET Core application based on the `webapp` template like so:

    dotnet new webapp
    
you get a simple web application as a result.

This is that simple application, ported to F#/[Giraffe](https://github.com/giraffe-fsharp/Giraffe).

Here's what it looks like:

<img src="https://i.imgur.com/xyo83dJ.png" width="500">

Going to:

    https://localhost:5001/error

simulates a runtime exception:

<img src="https://i.imgur.com/eoeBRVG.png" width="500">

Enter the following to run the app:

    dotnet watch run

The code for the app is in [Program.fs](https://github.com/dharmatech/aspnet-core-web-app-fs-giraffe/blob/main/Program.fs).
