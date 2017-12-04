﻿namespace polly

module Out =

    let private o = obj ()

    let print x = lock o (fun _ -> printf "%s" x)

    let println x = lock o (fun _ -> printfn "%s" x)

    let printSpecial (text : string) =
        let spaces len =
            let s = System.Text.StringBuilder ()
            for i = 1 to len do s.Append " " |> ignore
            s.ToString ()
        let hr = "==================================================================";
        let spaces1 = spaces ((hr.Length - text.Length) / 2)
        let spaces2 = spaces (hr.Length - spaces1.Length)
        hr + "\n" +
        spaces1 + text + spaces2 + "\n" +
        hr + "\n"
        |> print