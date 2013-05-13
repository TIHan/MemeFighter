namespace MemeFighter

type Process<'State, 'Msg> (initial: 'State, execute) =
    let mailbox = new MailboxProcessor<'Msg> (fun agent ->
        let rec loop (state: 'State)  =
            async {
                let! msg = agent.Receive ()
                return! loop (execute state msg)
            }
        loop initial)
        
    do
        mailbox.Start ()

    member this.Send msg =
        mailbox.Post msg
    
    member this.SendAndReply msg =
        mailbox.PostAndReply msg
