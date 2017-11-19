namespace polly

open System
open System.Net
open System.Net.Mail

module Email =

    let private send (toAddress : string) subject body =
        let fromAddress = MailAddress ("pollymonitor2@gmail.com", "Polly")
        let toAddress = MailAddress toAddress
        let fromPassword = "G1gabyt3?az"
        use smtp =
            new SmtpClient (
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = NetworkCredential (fromAddress.Address, fromPassword) )
        use message = new MailMessage (fromAddress, toAddress, Subject = subject, Body = body)
        smtp.Send message

    let private makeComputerText () =
        sprintf "Computer = %s" Environment.MachineName

    let sendReset (toAddress : string) reason log =
        let subject = "Reset notification"

        let computer = makeComputerText ()
        let reason = sprintf "\nReason = %s" reason
        let log = match log with    | None -> ""
                                    | Some text -> sprintf "\n\nLog =\n%s" text
        let body = sprintf "%s%s%s" computer reason log

        send toAddress subject body

    let sendPublicIp (toAddress : string) (ip : string) =
        let subject = "New IP address"

        let computer = makeComputerText ()
        let ip = sprintf "IP address = %s" ip
        let body = sprintf "%s\n%s\n" computer ip

        send toAddress subject body
