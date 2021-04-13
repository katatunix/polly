module polly.Miner

open System
open System.Text
open System.Diagnostics
open System.Threading
open System.IO
open System.IO.Pipes
open System.Management

let private killProcess processId =
    try Process.GetProcessById(processId).Kill() with _ -> ()

let rec private killProcessTree rootProcessId =
    killProcess rootProcessId

    let query = sprintf "SELECT * FROM Win32_Process WHERE ParentProcessID=%d" rootProcessId
    use searcher = new ManagementObjectSearcher (query)
    let moc = searcher.Get ()
    for mo in moc do
        killProcessTree (Convert.ToInt32 mo.["ProcessID"])

let private startWith (bootstrap: Process) onLine onExit =
    let pipe = new NamedPipeServerStream ("Polly", PipeDirection.In)
    (Thread (ThreadStart (fun _ -> Thread.Sleep 500; bootstrap.Start () |> ignore))).Start ()
    pipe.WaitForConnection ()

    let stream = new StreamReader (pipe, Encoding.UTF8)
    let minerProcessId = stream.ReadLine () |> int

    let stop () =
        try
            killProcessTree minerProcessId
            stream.Dispose ()
            pipe.Dispose ()
            bootstrap.Kill ()
        with _ ->
            ()

    let beginTime = TimeMs.Now
    let rec loop () =
        let line = try stream.ReadLine () with _ -> null
        if isNull line then
            stop ()
            onExit (TimeMs.Now - beginTime)
        else
            onLine line
            loop ()
    (Thread (ThreadStart loop)).Start ()

    StopFun stop

let start bootstrapFile minerPath minerArgs noDevFee onLine onExit =
    let bootstrap = new Process ()
    let baseDir = AppDomain.CurrentDomain.BaseDirectory
    bootstrap.StartInfo.WorkingDirectory <- baseDir
    bootstrap.StartInfo.UseShellExecute <- false
    bootstrap.StartInfo.FileName <- Path.Combine (baseDir, bootstrapFile)
    bootstrap.StartInfo.Arguments <- sprintf "%s %s %s" (if noDevFee then "yes" else "no") minerPath minerArgs

    startWith bootstrap onLine onExit
