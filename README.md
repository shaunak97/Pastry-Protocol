# Pastry-Protocol
  
  The project is an implementation of Pastry, a scalable, distributed object location and routing substrate for wide-area peer-to-peer applications. Pastry is capable of performing application-level routing and object location in large overlay network of nodes. The project is implemented in F# using the Akka actor model. 
  
Implemented as per the specification of the Pastry protocol that can be found in the paper Pastry: Scalable, decentralized object location and routing for large-scale peer-to-peer systems by A. Rowstron and P. Druschel.

You can find the paper at http://rowstron.azurewebsites.net/PAST/pastry.pdf

The paper above, in Section 2.3 contains a specification of the Pastry API and of the API implemented by the application.
  
TEAM MEMBERS:


    BHARATH SHANKAR, UFID: 9841-4098
    SHAUNAK SOMPURA, UFID: 9911-2362


HOW TO RUN:

1) Navigate to the folder with the file project3.fsx
2) Run the following command on the terminal:

       dotnet fsi --langversion:preview project3.fsx <numNodes> <numRequests> 

WHAT IS RUNNING: 
  
  The program forms an overlay network of nodes with numNode number of Pastry nodes. A unique nodeId is created for each node along with a table that stores the leafset, neighbourhood set and the routing table. The program is able to form a connection between the nodes in the shortest number route possible. The program checks the leafset, neighborhood set and the routing table to find the next node to forward the message to. The message being sent between the nodes is "Pastry". The program keeps track of hops for each connection. The final output is the average number of hops for all connections.  
    

PROGRAM INPUTS:
      
      1.Number of nodes in the peer to peer system
      2.Number of requests each node has to make
        
  
LARGEST NETWORK DEALT WITH:

        The largest network the program run for was numNodes = 25000 and the average number of hops is 5.8
        The average hop count varies according to the value of b, number of nodes and number of requests. The average hop count increases with the number of nodes in the network.
  

