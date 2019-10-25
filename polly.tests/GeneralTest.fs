module polly.tests.GeneralTest

open System
open NUnit.Framework

[<Test>]
let ``douma`` () =
    let ms = 3600000L * 24L + 3600000L
    let ts = TimeSpan.TicksPerMillisecond * ms |> TimeSpan
    Assert.AreEqual ("01.01:00:00", ts.ToString @"dd\.hh\:mm\:ss")
