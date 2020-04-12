module polly.tests.EmailTest

open NUnit.Framework
open polly
open polly.Config

[<Test>]
let ``test send email`` () =
    let sender =
        { SmtpHost = "smtp.gmail.com"
          SmtpPort = 587
          Address = "pollymonitor3@gmail.com"
          Password = "Minh12345678"
          DisplayedName = "Polly" }
    Email.sendPublicIp sender [ "katatunix@gmail.com" ] "1.2.3.4"
