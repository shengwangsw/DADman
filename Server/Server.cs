﻿using ComLibrary;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace Server
{
    public delegate void delImageVisible(int playerNumber);
    //client in server
    class Client
    {
        public string nick;
        public int playernumber;
        public string url;
        public IClient clientProxy;
        public bool dead;
        public bool connected;
        public int score;
       
    }

    public class Server
    {
        private int MSECROUND = Program.MSSEC; //game speed [communication refresh time]

        static TcpChannel channel = new TcpChannel(Program.PORT);

        public static string PATH = @".."+ Path.DirectorySeparatorChar+".."+ Path.DirectorySeparatorChar+
            ".."+ Path.DirectorySeparatorChar+"Server"+ Path.DirectorySeparatorChar+
            "bin"+ Path.DirectorySeparatorChar+"Log.txt";

       

        public Server()
        {
            createConnection();
        }
        
        private void createConnection()
        {
            ChannelServices.RegisterChannel(channel, false);
            RemotingConfiguration.RegisterWellKnownServiceType(
                typeof(RemoteServer),
                "Server",
                WellKnownObjectMode.Singleton
            );
        }
        
    }

    public class RemoteServer : MarshalByRefObject, IServer, IServerReplication, IGeneralControlServices
    {
        internal List<Client> clientList = new List<Client>();
        private Dictionary<string, int> player_image_hashmap = new Dictionary<string, int>();
        public int numberPlayersConnected = 0;
        public delegate void delProcess(int playerNumber, string move);

        public ServerForm serverForm;
        public Dictionary<IServer, bool> serversConnected = new Dictionary<IServer, bool>();
        public bool firstRound = true;

        public RemoteServer()
        {
            serverForm = new ServerForm(this);
            Thread thread = new Thread(() => createServerForm());
            thread.Start();
          
        }

        private void createServerForm()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(serverForm);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void connect(string nick, string url)
        {
            Client c = new Client();
            
            Console.WriteLine("SERVER: cliente tenta ligar com url "+url+".");
           

            IClient clientProxy = (IClient)Activator.GetObject(
                typeof(IClient),
                url
            );

            try { Console.WriteLine("SERVER: " + clientProxy.ToString()); }
            catch (Exception e){ Console.WriteLine(e); }

            numberPlayersConnected++;

            //Create a connected client with below parameters:
            c.nick = nick;
            c.url = url;
            c.clientProxy = clientProxy;
            c.playernumber = numberPlayersConnected;
            c.dead = false;
            c.connected = true;
            c.score = 0;

            clientList.Add(c);

            assignPlayer(c);
            if (numberPlayersConnected == Program.PLAYERNUMBER)
            {
                Thread thread = new Thread(() => sendStartGame());
                thread.Start();
            }

        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void sendMove(string nick, string move)
        {
            //TODO, RECONNECT THE CLIENT, IF NEEDED
            /*
            foreach (Client c in clientList)
            {
                if (c.nick.Equals(nick) && c.connected == false)
                {
                    c.connected = true;
                    //TODO Sheng
                }
            }
            */
            int pl_number = player_image_hashmap[nick];
            try
            {
                this.serverForm.listMove.Add(pl_number, move);
               // Console.WriteLine("*** Recebi " + pl_number + " " + move);
               // Console.WriteLine("*** " + this.serverForm.listMove.ToString());

                foreach (KeyValuePair<int, string> entry in this.serverForm.listMove)
                {
                    Console.WriteLine("***"+ entry.Key + " " + entry.Value);
                }
            }
            catch
            {
                Console.WriteLine("Client sent a move exception (Server sendMove(...))");
            }
            //this.serverForm.Invoke(new delProcess(serverForm.processMove), new object[] { pl_number, move });
        }


        private void assignPlayer(Client c)
        {
            player_image_hashmap.Add(c.nick, c.playernumber);

            foreach (KeyValuePair<string, int> entry in player_image_hashmap)
            {
                Console.WriteLine("INFO: " + entry.Key + " is " + entry.Value);
            }

        }

        public void sendPlayerDead(int playerNumber)
        {
            foreach (Client c in clientList)
            {
                if (c.playernumber == playerNumber)
                {
                    c.dead = true;
                }
                try
                {
                    c.clientProxy.playerDead(playerNumber);
                }
                catch
                {
                    c.connected = false;
                    Console.WriteLine("Exception on server sendPlayerDead()");
                }
            }
            string nick = player_image_hashmap.FirstOrDefault(x => x.Value == playerNumber).Key;
        }

        public void sendRoundUpdate(int roundID, string players_arg, string dead_arg, string monster_arg, string coins_arg) 
        {
            
            foreach (Client c in clientList)
            {
                //TODO, WHETHER NEED A THREAD TO RECEIVE ROUND UPDATE
                //new Thread(() => 
                //if(c.connected)
                try
                {
                    c.clientProxy.receiveRoundUpdate(roundID, players_arg, dead_arg, monster_arg, coins_arg);
                }
                catch
                {
                    c.connected = false;
                }
                //).Start();
            }
        }


        public void sendCoinEaten(int playerNumber, string coinName)
        {
            foreach (Client c in clientList)
            {
                try
                {
                    c.clientProxy.coinEaten(playerNumber, coinName);
                }
                catch
                {
                    c.connected = false;
                    Console.WriteLine("Exception on server sendCoinEaten()");
                }

            }
            string nick = player_image_hashmap.FirstOrDefault(x => x.Value == playerNumber).Key;
            
        }

        
        public void sendStartGame()
        {
            
                string arg = " ";
                foreach (Client c in clientList)
                {
                    arg += "-" +c.nick+"_"+ c.playernumber + "_" + c.url;
                }
                foreach (Client c in clientList)
                {
                    Console.WriteLine("SERVER: startGame:"+ arg);
                    try
                    {
                        c.clientProxy.startGame(numberPlayersConnected, arg);
                    }
                  
                    catch(Exception e)
                    {
                        Console.WriteLine("Exception on sendStartGame: " + e);
                    }
                }

                this.serverForm.Invoke(new delImageVisible(serverForm.startGame), new object[] { numberPlayersConnected });
                Console.WriteLine("Game started!");
            
        }

        public void connect(string url)
        {
            IServer serverProxy = (IServer)Activator.GetObject(
                typeof(IServer),
                url
            );


            //try catch missed
            while (serverProxy != null)
            {
                try
                {
                    serverProxy.connect("SERVER", "tcp://" + Util.GetLocalIPAddress() + ":" + port + "/Server");
                    serversConnected.Add(serverProxy,true);
                    break;
                }
                catch
                {       
                    Thread.Sleep(1000);
                }
            }
            serverProxy.SendFirstRound();
        }

        public void requestRound(int id)
        {
            throw new NotImplementedException();
        }

        public void Freeze()
        {
            //TODO
            throw new NotImplementedException();
        }

        public void Unfreeze()
        {
            //TODO
            throw new NotImplementedException();
        }

        public void InjectDelay(string pid1, string pid2)
        {
            //TODO
            throw new NotImplementedException();
        }

        public void newServerCreated(string serverURL)
        {
            connect(serverURL);
        }

        public void SendFirstRound(int roundID)
        {

            foreach (KeyValuePair<IServer, bool> entry in serversConnected)
            {
                if(entry.Value == true)
                {
                    entry.Key.UpdateBoard(roundID);
                }
            }


        }

        public void UpdateBoard(int roundID)
        {



        }
    }
}
