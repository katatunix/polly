module polly.Main

[<EntryPoint>]
let main argv =
    printfn "\npolly 5.0 - katatunix@gmail.com\n"

    let configFile = if argv.Length = 0 then None else Some argv.[0]

    match Config.load configFile with
    | Error msg ->
        printfn "Error: %s" msg
        1
    | Ok config ->
        use ipTimer = PublicIp.start config
        use monitor = new Monitor.Agent (config)
        monitor.Join ()
        printfn "EXIT!!!"
        0
