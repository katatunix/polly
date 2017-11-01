namespace polly

open System
open System.IO
open System.Text.RegularExpressions
open System.Diagnostics

open FSharp.Data

open NghiaBui.Common

module Main =

    let getHtml port =
        let url = sprintf "http://localhost:%d" port
        try
            Http.RequestString url |> Some
        with _ ->
            None

    let cleanHtml (html : string) =
        let regex = Regex """<font color=\"#.{6}\">"""
        regex
            .Replace(html, "")
            .Replace("\r\n", "")
            .Replace("\n", "")
            .Replace("<br>", "\n")
            .Replace("""<html><body bgcolor="#000000" style="font-family: monospace;">""", "")
            .Replace("</font>", "")
            .Replace("</body></html>", "")

    let readErrorIndicators () =
        "config.txt"
        |> File.ReadAllLines
        |> List.ofArray
        |> List.filter (fun line ->
            let trimmed = line.Trim ()
            trimmed.Length > 0 && not (trimmed.StartsWith "#"))

    type ErrorType =
        | NoAnswer
        | RunningError of string * string

    let checkHtml (errorIndicators : string list) (htmlOp : string option) =
        match htmlOp with
        | None ->
            Some NoAnswer
        | Some html ->
            errorIndicators
            |> List.tryFind (fun indicator -> html.IndexOf indicator > -1)
            |> Option.map (fun indicator -> RunningError (indicator, html))

    let checkMiner port errorIndicators =
        getHtml port
        |> Option.map cleanHtml
        |> checkHtml errorIndicators

    let restart () =
        Process.Start ("shutdown", "/r /t 1") |> ignore

    let makeErrorMessage = function
        | NoAnswer ->
            "No answer from miner", None
        | RunningError (indicator, html) ->
            (indicator, Some html)

    let start port minutes emailAddress =
        while true do
            Threading.Thread.Sleep (minutes * 60 * 1000)
            let errorIndicators = readErrorIndicators ()
            match checkMiner port errorIndicators with
            | None ->
                printfn "[%A] OK" DateTime.Now
            | Some error ->
                printfn "[%A] RESET" DateTime.Now
                match emailAddress with
                | Some addr -> Email.send addr (makeErrorMessage error)
                | None -> ()
                restart ()

    [<EntryPoint>]
    let main argv =
        //Process.run argv.[0] argv.[1]
        //0
        match argv with
        | [| Int port; Int minutes |] ->
            start port minutes None
            0
        | [| Int port; Int minutes; emailAddress |] ->
            start port minutes (Some emailAddress)
            0
        | _ ->
            printfn "Usage: port minutes"
            1
