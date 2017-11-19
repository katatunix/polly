namespace polly

open System
open System.Net
open System.Net.Mail

module Email =

    type SenderInfo = {
        SmtpHost : string
        SmtpPort : int
        Email : string
        Password : string
        DisplayedName : string }

    let private send (senderInfo : SenderInfo) (toAddress : string) subject body =
        let fromAddress = MailAddress (senderInfo.Email, senderInfo.DisplayedName)
        let toAddress = MailAddress toAddress
        let fromPassword = senderInfo.Password
        use smtp =
            new SmtpClient (
                Host = senderInfo.SmtpHost,
                Port = senderInfo.SmtpPort,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = NetworkCredential (fromAddress.Address, fromPassword) )
        use message = new MailMessage (fromAddress, toAddress, Subject = subject, Body = body)
        smtp.Send message

    let private makeComputerText () =
        sprintf "Computer = %s" Environment.MachineName

    let sendReset senderInfo (toAddress : string) reason log =
        let subject = "Reset notification"

        let computer = makeComputerText ()
        let reason = sprintf "\nReason = %s" reason
        let log = match log with    | None -> ""
                                    | Some text -> sprintf "\n\nLog =\n%s" text
        let body = sprintf "%s%s%s" computer reason log

        send senderInfo toAddress subject body

    let sendPublicIp senderInfo (toAddress : string) (ip : string) =
        let subject = "New IP address"

        let computer = makeComputerText ()
        let ip = sprintf "IP address = %s" ip
        let body = sprintf "%s\n%s\n" computer ip

        send senderInfo toAddress subject body
