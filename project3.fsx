#time "on"
#r "nuget: Akka.FSharp" 
#r "nuget: Akka.TestKit" 


open System
open System.Diagnostics
open Akka.Actor
open Akka.Configuration
open Akka.FSharp
open Akka.TestKit
open System.Collections.Generic


type Pastry =
    |BeginRouting of string[]
    |CompleteReq of float
    |BuildNetwork of int * int
    |SetAct of IActorRef[]
    |SetBoss of IActorRef[]
    |Route of string * int * string 
    |SetNodeVals of string * int * Dictionary<string,IActorRef> * string array * string [,] * string array * string array

let r  = System.Random()

let system = ActorSystem.Create("System")


let Node nodeNum (mailbox:Actor<_>)  =

    let mutable currNodeId = ""
    let mutable nodesDict = new Dictionary<string, IActorRef>()
    let mutable nodesArr = Array.empty
    let mutable routingTable = Array2D.create 8 4 "0"
    let mutable (leafNodes : string array) = Array.create 8 ""
    let mutable (neighbourNodes : string array) = Array.create 8 ""
    let mutable hops = 0
    let mutable v:IActorRef[] = [||]
    let mutable bossRef:IActorRef[] =[||]

    let rec loop() = actor {

        let! message = mailbox.Receive()
        match message with 

        | SetBoss boss ->
            bossRef<-boss          

        | SetNodeVals (nodeId,numberOfReq,nodesM,nodeIdL,routingT,leafN,neighbourN)->
            currNodeId<-nodeId
            nodesDict<-nodesM
            nodesArr<- nodeIdL
            routingTable<-routingT
            leafNodes<-leafN
            neighbourNodes<-neighbourN
            
        |Route (destNode,numberOfHopsPassed,pMsg) ->
                let mutable nodeF = currNodeId
                let mutable (combinedAddr : string array) = Array.create 48 ""
                let mutable indexOfcombinedAddr = 0
                let mutable nextNode = ""
                let mutable index=0
                let mutable row=0
                let mutable col=0
                let mutable iter=0
                let mutable minDist=0
                let mutable dest = int(destNode)
                let mutable numberOfHopsPassed = numberOfHopsPassed
                let mutable firstMatchedDigits = 0
                let mutable (combinedAddrArray: string array) = Array.empty 
                let mutable (dests: string array) = Array.empty 
                hops<-numberOfHopsPassed

                if currNodeId=destNode  then

                   bossRef.[0]<!CompleteReq( float (hops))

                else
                    //checking leaf nodes to forward msg
                    let l1 = int leafNodes.[0]
                    let l2 = int leafNodes.[7]

                    if dest >= l1 && dest <= l2 then
                        minDist <- abs(dest - int(leafNodes.[0]))
                        nodeF <- leafNodes.[0]
                        iter<-1

                        while(iter<8) do                       
                            if(abs(dest-int(leafNodes.[iter])) < minDist) then
                                minDist <- abs(dest-int(leafNodes.[iter]))
                                nodeF <- leafNodes.[iter]
                            iter<-iter+1

                    //checking routing table to forward msg
                    else

                        iter<-1
                        while(iter<7) do
                                if(currNodeId.[0..iter] = destNode.[0..iter]) then
                                        firstMatchedDigits<-iter                                        
                                else    
                                        iter<-6
                                iter<-iter+1

                        row<-firstMatchedDigits
                        col<-int(string(destNode.[firstMatchedDigits]))

                        if(( routingTable.[row,col] <> "0") && (firstMatchedDigits>=0)) then
                                nodeF <- routingTable.[row,col]

                        //forming a combinedAddrArray and checking it to forward msgs      
                        else

                                combinedAddrArray<-Array.append combinedAddrArray neighbourNodes
                                combinedAddrArray<-Array.append combinedAddrArray leafNodes
                                index<-0
                                iter<-0
                                indexOfcombinedAddr<-16

                                while(index < 8) do
                                        while(iter<4) do
                                                if(routingTable.[index,iter] <> "0") then

                                                        combinedAddrArray<-Array.append combinedAddrArray [|routingTable.[index,iter]|]  

                                                iter<-iter+1
                                        iter<-0        
                                        index<-index+1

                                let mutable secondMatchedDigits = -1

                                index<-0
                                let mutable flag = true
                                let mutable i = 0
                                while(index < combinedAddrArray.Length-1) do

                                                nextNode<-combinedAddrArray.[index]
                                                i<-0
                                                flag <- true
                                                while((i<8)&&(flag)) do
                                                        if(nextNode.[0..i]=destNode.[0..i]) then
                                                                secondMatchedDigits <-i
                                                        else
                                                                flag<-false
                                                        i<-i+1

                                                if(secondMatchedDigits>firstMatchedDigits) then
                                                        firstMatchedDigits<-secondMatchedDigits
                                                        nodeF<-nextNode

                                                else
                                                        if(secondMatchedDigits=firstMatchedDigits) then
                                                                if(abs(int(nextNode)-dest)<abs(int(currNodeId)-dest)) then
                                                                        firstMatchedDigits<-secondMatchedDigits
                                                                        nodeF<-nextNode

                                                index<-index+1

                    let v = nodesDict.Item(nodeF)
                      
                    if nodeF=currNodeId then
                        bossRef.[0] <! CompleteReq(float(hops))
                    else
                        v<!Route(destNode,hops+1,pMsg)        
        | _-> ()

        return! loop()
    }
    loop()

let Boss (mailbox:Actor<_>) = 

    let mutable numNodes = 0
    let mutable numRequests = 0
    let nodesDict = new Dictionary<string, IActorRef>()
    let mutable ttlhops = 0.0
    let mutable timesCompleted = 0.0
    let mutable bossRef:IActorRef[] = [||]
    
    let rec loop() = actor {

            let! message = mailbox.Receive()

            match message with 

            | SetAct boss ->
                bossRef<-boss
                
            | BuildNetwork (numNds,numReq) ->
                numNodes <-numNds
                numRequests <-numReq
                
                let mutable nodesArr = Array.create numNodes ""
                let mutable sortedArr = Array.create numNodes ""
                let leafNodes = Array.zeroCreate (8)
                let nbrNodes = Array.zeroCreate (8)

                let mutable unique = 0   
                let mutable nodeId = ""
                let mutable digitIndex = 0 

                //Spawning all acotrs
                for i in [0..numNodes-1] do
    
                    let nodeRef = Node (i+1)|> spawn system ("node"+string(i))

                    unique <-0
                    while unique=0 do
                        nodeId<-""
                        for i in [0..7] do
                            let rand= string (r.Next(0,4))
                            nodeId <- nodeId+rand
                                                                           
                        if not (nodesDict.ContainsKey(nodeId)) then
                            nodesDict.Add(nodeId,nodeRef)                           
                            nodesArr.[i] <- nodeId                           
                            unique<-1

                //sorting the list    
                sortedArr<-Array.sort nodesArr

                let count=0
                nodeId <- ""

                for i in [0..numNodes-1] do
                    
                    nodeId <- nodesArr.[i]
                    
                    //setting leaves
                    let findIndex arr elem = arr |> Array.findIndex ((=) elem)
                    let nodeIndex = findIndex sortedArr nodeId                     
                    
                    let mutable startNode = 0

                    if (nodeIndex-4)<0 then
                        startNode<-0

                    elif (nodeIndex+4)>numNodes-1 then                       
                        startNode <- numNodes-9

                    else
                        startNode <- nodeIndex-4

                    for i in [0..7] do
                        if startNode=nodeIndex then
                            startNode <- startNode+1
                        leafNodes.[i]<-sortedArr.[startNode]
                        startNode<-startNode+1

                    //setting neighbours
                    for n in [0..7] do
                        nbrNodes.[n] <- nodesArr.[(n+1) % numNodes] 

                    //setting the routing table                                                                                  
                    let mutable flag = false
                    let mutable column = 0
                    let mutable digit=0                    
                    let routingTable = Array2D.create 8 4 "0"

                    for entry in nodesDict do 
                    
                        digit<-0
                        flag<-false
                        column<-0

                        if entry.Key<>nodeId then
                                while not flag do
                                        if entry.Key.[digit]=nodeId.[digit] then
                                                digit<-digit+1
                                        else
                                                column<- entry.Key.[digit]|>string|>int
                                                if routingTable.[digit, column]="0"  then
                                                        routingTable.[digit, column]<- entry.Key                              
                                                        flag<-true
                                                else
                                                        flag<-true
                    

                        let v = nodesDict.Item(nodeId)

                        v <! SetBoss bossRef
                        v <! SetNodeVals(nodeId,numRequests,nodesDict,nodesArr,routingTable,leafNodes,nbrNodes)

                bossRef.[0] <! BeginRouting nodesArr
            
            | CompleteReq hops ->

                timesCompleted <- timesCompleted + 1.0
                ttlhops <- ttlhops + hops                
                let tot= numNodes * numRequests

                if timesCompleted = float (numNodes * numRequests) then
                    let avg= ttlhops/timesCompleted
                    printfn "Average Hops : %A" avg
                    Environment.Exit 0

            | BeginRouting nodesArr->

                let mutable nxt=0
                let mutable reqs=0
                
                for i in [0..numNodes-1] do 
                    nxt <- i

                    while reqs<numRequests do
                        nxt <- (nxt+1) % numNodes
                        let ke,v=nodesDict.TryGetValue nodesArr.[i]
                        v <! Route(nodesArr.[nxt],0,"Pastry")
                        reqs <- reqs+1   
                    reqs <- 0
                        
                                
            | _->()

            return! loop()
        }
    loop()


            

             
module mainModule=

        let args : string array = fsi.CommandLineArgs |> Array.tail
        let numNodes=args.[0] |> int
        let numRequests=args.[1] |> int
        
        let boss= 
                Boss
                    |> spawn system "boss"

        let mutable b:IActorRef[] = [|boss|]        

        boss<!SetAct(b)
        boss<!BuildNetwork(numNodes,numRequests)
   
        System.Console.ReadLine() |> ignore