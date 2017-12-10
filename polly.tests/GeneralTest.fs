namespace polly.tests
open System
open NUnit.Framework

module GeneralTest =

    [<Test>]
    let `` `` () =
        let ms = 3600000L * 24L + 3600000L
        let ts = TimeSpan.TicksPerMillisecond * ms |> TimeSpan
        Assert.AreEqual ("01.01:00:00", ts.ToString @"dd\.hh\:mm\:ss")
