module polly.tests.GeneralTest

open System
open NUnit.Framework
open FsUnit

[<Test>]
let ``douma`` () =
    let ms = 3600000L * 24L + 3600000L
    let ts = TimeSpan.TicksPerMillisecond * ms |> TimeSpan
    ts.ToString @"dd\.hh\:mm\:ss" |> should equal "01.01:00:00"
    