namespace polly

open System
open System.Net
open System.Net.Mail

module Email =

    let send (toAddress : string) (reason, html) =
        let fromAddress = MailAddress ("pollymonitor2@gmail.com", "Polly")
        let toAddress = MailAddress toAddress
        let fromPassword = "G1gabyt3?az"
        let subject = "Reset notification"

        let computer = sprintf "Computer = %s" Environment.MachineName
        let reason = sprintf "\nReason = %s" reason
        let log = match html with   | None -> ""
                                    | Some text -> sprintf "\n\nLog =\n%s" text
        let body = sprintf "%s%s%s" computer reason log

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
