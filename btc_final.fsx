#time "on"
#r "nuget: Akka.FSharp" 

open Akka.Actor
open Akka.FSharp
open System.Security.Cryptography

//command line arguments
let zeroCount = fsi.CommandLineArgs.[1] |> int

//Getting processor count
let actorCount = System.Environment.ProcessorCount

//Actor count
let mutable count = 0

//creating an actor system
let system = ActorSystem.Create("FSharp")

//union of messages to an actor
type ActorMsg =
    | StartMining
    | MineCoins of int*int
    | PrintCoins of list<string>
    | StopMining

//Print actor to print the input string and hash value
let PrintingActor (mailbox:Actor<_>)=
    let rec loop()=actor{
        let! msg = mailbox.Receive()
        match msg with
        | PrintCoins(result) -> 
            printfn "The input is = %s   The hash value is %s" result.[0] result.[1]
            
        | _ -> failwith "unknown message"
        return! loop()
    }
    loop()
let printRef = spawn system "PrintingActor" PrintingActor 

// let getValidHash input = generateRandomString >>

// let generateRandomString input = fun 

// let genereateHash = fun

// let isValidHash = fun (x:string,y:int) -> int64 ("0x" + x.[0..y-1]) = 0L

//Miner actor
let MinerActor (mailbox:Actor<_>)=
    let rec loop()=actor{
        let! msg = mailbox.Receive()
        match msg with 
        | MineCoins(zeroCount, actorId) ->  let hashObj = SHA256Managed.Create()
                                            for i in 0 .. actorCount .. 1000000 do
                                                let  input = "venkateshpramani"+ string (i+actorId)
                                                let hashString = input 
                                                                |> System.Text.Encoding.ASCII.GetBytes 
                                                                |> hashObj.ComputeHash
                                                                |> Seq.map(fun x -> x.ToString("x2")) 
                                                                |> Seq.reduce(+)
                                                let isValidHash = int64 ("0x" + hashString.[0..zeroCount-1]) = 0L
                                                
                                                if isValidHash then printRef <! PrintCoins([hashString; input])
                                            
                                            mailbox.Sender() <! StopMining

        | _ -> printfn "Miner Actor received a wrong message"
    }
    loop()

//Master Actor
let MasterActor (mailbox:Actor<_>) =
    let rec loop()=actor{
        let! msg = mailbox.Receive()
        match msg with 
        | StartMining -> let actorRefList = [for i in 1 .. actorCount do yield(spawn system ("Actor" + (string i)) MinerActor)] //creating workers
                         for i in 0 .. actorCount-1 do //distributing work to the workers
                            actorRefList.Item(i) <! MineCoins(zeroCount, i) //sending message to worker

        | StopMining -> count <- count + 1
                        if count = actorCount then 
                            mailbox.Context.System.Terminate() |> ignore
                                            
        | _ -> printfn "Master actor received a wrong message"
        return! loop()
    }
    loop()


//creating master actor
let actorRef = spawn system "MasterActor" MasterActor
actorRef <! StartMining

//waiting for boss actor to terminate the actor system
system.WhenTerminated.Wait()