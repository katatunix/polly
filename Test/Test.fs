module polly.Test

open System
open NUnit.Framework

open polly
open polly.Main

[<Test>]
let ``test send email`` () =
    Email.send "nghiabuivan1987@gmail.com" ("fan=0%", Some "haha fan=0% haha")

[<Test>]
let ``test get html`` () =
    let html = getHtml 3333
    printfn "%s" html.Value

[<Test>]
let ``test clean html`` () =
    let html = """<html><body bgcolor="#000000" style="font-family: monospace;">
{"result": ["10.0 - ETH", "336", "180898;890;0", "30139;30141;30147;30154;30158;30156", "0;0;0", "off;off;off;off;off;off", "59;55;59;58;61;59;58;55;58;51;54;41", "asia1.ethermine.org:4444", "1;0;0;0"]}<br><br><font color="#ff00ff">GPU0 t=59C fan=55%, GPU1 t=59C fan=58%, GPU2 t=61C fan=59%, GPU3 t=58C fan=55%, GPU4 t=58C fan=52%, GPU5 t=54C fan=42%
</font><br><font color="#00ff00">ETH: 10/28/17-08:28:30 - SHARE FOUND - (GPU 5)
</font><br></body></html>"""
    let clean = cleanHtml html
    let expected = """{"result": ["10.0 - ETH", "336", "180898;890;0", "30139;30141;30147;30154;30158;30156", "0;0;0", "off;off;off;off;off;off", "59;55;59;58;61;59;58;55;58;51;54;41", "asia1.ethermine.org:4444", "1;0;0;0"]}

GPU0 t=59C fan=55%, GPU1 t=59C fan=58%, GPU2 t=61C fan=59%, GPU3 t=58C fan=55%, GPU4 t=58C fan=52%, GPU5 t=54C fan=42%
ETH: 10/28/17-08:28:30 - SHARE FOUND - (GPU 5)
"""
    Assert.AreEqual (expected, clean)

//===============================================================================
type Node = {
    Value : int
    Left : Node option
    Right : Node option }

let rec printTree node =
    printfn "Node: %d" node.Value
    match node.Left with
    | Some n -> printTree n
    | None -> ()
    match node.Right with
    | Some n -> printTree n
    | None -> ()

let rec countTree nodeOp cont =
    match nodeOp with
    | None -> cont 0
    | Some node ->
        countTree node.Left (fun numLeft ->
            countTree node.Right (fun numRight ->
                1 + numLeft + numRight |> cont))


[<Test>]
let ``test print tree`` () =
    let node = {
        Value = 1
        Left = Some { Value = 2; Left = None; Right = None }
        Right = Some { Value = 3; Left = None; Right = None }
    }
    printTree node
    Assert.AreEqual (3, countTree (Some node) id)
