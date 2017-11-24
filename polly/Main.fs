namespace polly

module Main =

    let o = obj ()
    let out x = lock o (fun _ -> printfn "%s" x)

    let start config =
        let ipTimer = PublicIp.startCheck out config
        Monitor.exec out config
        ipTimer.Stop ()

    [<EntryPoint>]
    let main argv =
        match Config.load () with
        | Error msg ->
            printfn "Error: %s" msg
            1
        | Ok config ->
            start config
            0
